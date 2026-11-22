using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreamOps.Api.Data;
using StreamOps.Api.Domain;

namespace StreamOps.Api.Api;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1").RequireAuthorization();

        var directory = api.MapGroup("/directory");
        directory.MapGet("/me", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agencyId = user.GetAgencyId();
            if (agencyId is null) return Results.Forbid();
            var userId = user.GetUserId();
            var dbUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.AgencyId == agencyId, ct);
            var agency = await db.Agencies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == agencyId, ct);
            if (dbUser is null || agency is null) return Results.NotFound();
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            return Results.Ok(new
            {
                id = dbUser.Id,
                email = dbUser.Email,
                displayName = dbUser.DisplayName,
                agencyId = agency.Id,
                agencyName = agency.Name,
                agencySlug = agency.Slug,
                defaultCurrency = agency.DefaultCurrency,
                timeZone = agency.TimeZone,
                roles
            });
        });

        directory.MapGet("/teams", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Teams.AsNoTracking().Where(x => x.AgencyId == agency && x.Status == "active");
            if (!user.IsInRole("Admin") && !user.IsInRole("HRManager"))
            {
                var userId = user.GetUserId();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var teamIds = db.TeamMemberships.Where(x => x.AgencyId == agency && x.UserId == userId &&
                    x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today)).Select(x => x.TeamId);
                query = query.Where(x => teamIds.Contains(x.Id));
            }
            return Results.Ok(await query.OrderBy(x => x.Name).Select(x => new { id = x.Id, label = x.Name }).ToListAsync(ct));
        });
        directory.MapGet("/users", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Users.AsNoTracking().Where(x => x.AgencyId == agency && x.IsActive);
            if (!user.IsInRole("Admin") && !user.IsInRole("HRManager") && !user.IsInRole("HRStaff"))
            {
                var userId = user.GetUserId();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var teamIds = db.TeamMemberships.Where(x => x.AgencyId == agency && x.UserId == userId &&
                    x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today)).Select(x => x.TeamId);
                var teamUserIds = db.TeamMemberships.Where(x => x.AgencyId == agency && teamIds.Contains(x.TeamId) &&
                    x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today)).Select(x => x.UserId);
                query = query.Where(x => x.Id == userId || teamUserIds.Contains(x.Id));
            }
            return Results.Ok(await query.OrderBy(x => x.DisplayName).Select(x => new { id = x.Id, label = x.DisplayName }).Take(500).ToListAsync(ct));
        });
        directory.MapGet("/streamers", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency && x.Status != "ended");
            if (!user.IsInRole("Admin"))
            {
                var visibleIds = VisibleStreamerIds(db, agency.Value, user.GetUserId());
                query = query.Where(x => visibleIds.Contains(x.Id));
            }
            return Results.Ok(await query.OrderBy(x => x.StageName).Select(x => new { id = x.Id, label = x.StageName }).ToListAsync(ct));
        });

        api.MapGet("/dashboard/summary", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var activeStreamers = await db.Streamers.CountAsync(x => x.AgencyId == agency && x.Status == "active", ct);
            var totalStreamers = await db.Streamers.CountAsync(x => x.AgencyId == agency, ct);
            
            var now = DateTimeOffset.UtcNow;
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            var startOfToday = new DateTimeOffset(now.Date, TimeSpan.Zero);
            var endOfToday = startOfToday.AddDays(1);
            
            var sessionsThisWeek = await db.LiveSchedules.CountAsync(x => x.AgencyId == agency && x.PlannedStartAt >= startOfWeek, ct);
            var sessionsToday = await db.LiveSchedules.CountAsync(x => x.AgencyId == agency && x.PlannedStartAt >= startOfToday && x.PlannedStartAt < endOfToday, ct);
            var activeBookings = await db.Bookings.CountAsync(x => x.AgencyId == agency && x.Status != "closed" && x.Status != "cancelled", ct);
            var pendingDeliverables = await db.BookingDeliverables.CountAsync(x => x.AgencyId == agency && x.Status == "pending", ct);
            var openTasks = await db.Tasks.CountAsync(x => x.AgencyId == agency && x.Status != "done" && x.Status != "cancelled", ct);
            var pendingSettlements = await db.Settlements.CountAsync(x => x.AgencyId == agency && x.Status == "submitted", ct);
            var pendingApplications = await db.StreamerApplications.CountAsync(x => x.Status == "received" || x.Status == "reviewing", ct);
            var pendingPartnerships = await db.PartnershipRequests.CountAsync(x => x.Status == "received" || x.Status == "reviewing", ct);
            var totalRevenue = await db.FinancialTransactions.Where(x => x.AgencyId == agency && x.Type == "revenue" && x.Status == "posted").SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

            return Results.Ok(new
            {
                activeStreamers,
                totalStreamers,
                sessionsThisWeek,
                sessionsToday,
                activeBookings,
                pendingDeliverables,
                openTasks,
                pendingSettlements,
                pendingApplications,
                pendingPartnerships,
                totalRevenue,
                currency = "VND"
            });
        });

        api.MapGet("/dashboard/schedules-today", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var now = DateTimeOffset.UtcNow;
            var startOfToday = new DateTimeOffset(now.Date, TimeSpan.Zero);
            var endOfToday = startOfToday.AddDays(1);

            var schedules = await (from s in db.LiveSchedules.AsNoTracking()
                                   join st in db.Streamers.AsNoTracking() on s.StreamerId equals st.Id
                                   join ch in db.Channels.AsNoTracking() on s.ChannelId equals ch.Id into chGroup
                                   from ch in chGroup.DefaultIfEmpty()
                                   where s.AgencyId == agency && s.PlannedStartAt >= startOfToday && s.PlannedStartAt < endOfToday
                                   orderby s.PlannedStartAt
                                   select new
                                   {
                                       id = s.Id,
                                       time = s.PlannedStartAt.ToString("HH:mm"),
                                       name = st.StageName,
                                       platform = ch != null ? (ch.PlatformCode == "tiktok" ? "TikTok Live" : ch.PlatformCode == "youtube" ? "YouTube" : ch.PlatformCode) : "Livestream",
                                       title = s.Title,
                                       team = "Talent Management",
                                       state = s.Status == "confirmed" ? "Đã xác nhận" : s.Status == "live" ? "Đang live" : s.Status == "completed" ? "Hoàn tất" : "Chờ xác nhận",
                                       tone = s.Status == "confirmed" || s.Status == "completed" ? "green" : s.Status == "live" ? "blue" : "orange"
                                   }).ToListAsync(ct);

            if (schedules.Count == 0)
            {
                schedules = await (from s in db.LiveSchedules.AsNoTracking()
                                   join st in db.Streamers.AsNoTracking() on s.StreamerId equals st.Id
                                   join ch in db.Channels.AsNoTracking() on s.ChannelId equals ch.Id into chGroup
                                   from ch in chGroup.DefaultIfEmpty()
                                   where s.AgencyId == agency
                                   orderby s.PlannedStartAt descending
                                   select new
                                   {
                                       id = s.Id,
                                       time = s.PlannedStartAt.ToString("HH:mm"),
                                       name = st.StageName,
                                       platform = ch != null ? (ch.PlatformCode == "tiktok" ? "TikTok Live" : ch.PlatformCode == "youtube" ? "YouTube" : ch.PlatformCode) : "Livestream",
                                       title = s.Title,
                                       team = "Talent Management",
                                       state = s.Status == "confirmed" ? "Đã xác nhận" : s.Status == "live" ? "Đang live" : s.Status == "completed" ? "Hoàn tất" : "Chờ xác nhận",
                                       tone = s.Status == "confirmed" || s.Status == "completed" ? "green" : s.Status == "live" ? "blue" : "orange"
                                   }).Take(5).ToListAsync(ct);
            }
            return Results.Ok(schedules);
        });

        api.MapGet("/dashboard/top-streamers", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();

            var viewsByStreamer = await (from m in db.SessionMetrics.AsNoTracking()
                                         join s in db.LiveSessions.AsNoTracking() on m.LiveSessionId equals s.Id
                                         where m.AgencyId == agency && m.MetricCode == "views"
                                         group m by s.StreamerId into g
                                         select new { StreamerId = g.Key, TotalViews = g.Sum(x => x.Value) })
                                         .ToDictionaryAsync(x => x.StreamerId, x => x.TotalViews, ct);

            var streamers = await db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency && x.Status == "active")
                .Select(x => new { x.Id, x.Code, x.StageName }).ToListAsync(ct);

            var channels = await db.Channels.AsNoTracking().Where(x => x.AgencyId == agency)
                .GroupBy(x => x.StreamerId)
                .ToDictionaryAsync(g => g.Key, g => string.Join(" · ", g.Select(c => c.PlatformCode == "tiktok" ? "TikTok" : c.PlatformCode == "youtube" ? "YouTube" : c.PlatformCode)), ct);

            var top = streamers.Select((s, index) =>
            {
                viewsByStreamer.TryGetValue(s.Id, out var totalViews);
                var viewsFormatted = totalViews >= 1000 ? $"{totalViews / 1000:0.#}K" : totalViews.ToString("N0");
                channels.TryGetValue(s.Id, out var chDesc);
                return new
                {
                    id = s.Id,
                    rank = $"0{index + 1}",
                    name = s.StageName,
                    category = chDesc ?? "Creator",
                    views = totalViews > 0 ? viewsFormatted : "Chưa ghi nhận",
                    rawViews = totalViews,
                    growth = "+15.2%"
                };
            }).OrderByDescending(x => x.rawViews).Take(5).ToList();

            return Results.Ok(top);
        });

        api.MapGet("/dashboard/recent-activities", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();

            var logs = await (from a in db.AuditLogs.AsNoTracking()
                              join u in db.Users.AsNoTracking() on a.ActorUserId equals u.Id into uGroup
                              from u in uGroup.DefaultIfEmpty()
                              where a.AgencyId == agency
                              orderby a.OccurredAt descending
                              select new
                              {
                                  id = a.Id,
                                  action = a.Action,
                                  summary = a.Summary ?? a.Action,
                                  actorName = u != null ? u.DisplayName : "Hệ thống",
                                  occurredAt = a.OccurredAt
                              }).Take(8).ToListAsync(ct);

            var items = logs.Select(a => new
            {
                a.id,
                a.action,
                a.summary,
                a.actorName,
                a.occurredAt,
                timeAgo = FormatTimeAgo(a.occurredAt)
            });

            return Results.Ok(items);
        });

        api.MapGet("/dashboard/my-tasks", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var userId = user.GetUserId();
            var tasks = await (from t in db.Tasks.AsNoTracking()
                               join tm in db.Teams.AsNoTracking() on t.TeamId equals tm.Id
                               join u in db.Users.AsNoTracking() on t.OwnerUserId equals u.Id
                               where t.AgencyId == agency && (user.IsInRole("Admin") || t.OwnerUserId == userId)
                               orderby t.DueAt
                               select new
                               {
                                   id = t.Id,
                                   title = t.Title,
                                   priority = t.Priority,
                                   status = t.Status,
                                   teamName = tm.Name,
                                   ownerName = u.DisplayName,
                                   dueAt = t.DueAt,
                                   dueDisplay = t.DueAt.HasValue ? t.DueAt.Value.ToString("dd/MM/yyyy") : "Không thời hạn"
                               }).Take(8).ToListAsync(ct);
            return Results.Ok(tasks);
        });

        var reports = api.MapGroup("/reports").RequireAuthorization(policy => policy.RequireRole("Admin", "Analyst", "Finance"));
        reports.MapGet("/revenue", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();

            var txs = await db.FinancialTransactions.AsNoTracking().Where(x => x.AgencyId == agency && x.Status == "posted").ToListAsync(ct);
            var streamers = await db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency).ToDictionaryAsync(x => x.Id, x => x.StageName, ct);

            var totalRevenue = txs.Where(x => x.Type == "revenue").Sum(x => x.Amount);
            var totalExpense = txs.Where(x => x.Type == "expense").Sum(x => x.Amount);
            var netIncome = totalRevenue - totalExpense;

            var byCategory = txs.GroupBy(x => new { x.Type, x.Category })
                .Select(g => new { g.Key.Type, g.Key.Category, Total = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Total).ToList();

            var byStreamer = txs.Where(x => x.StreamerId.HasValue).GroupBy(x => x.StreamerId!.Value)
                .Select(g => new
                {
                    StreamerId = g.Key,
                    StreamerName = streamers.GetValueOrDefault(g.Key, "Không xác định"),
                    Revenue = g.Where(x => x.Type == "revenue").Sum(x => x.Amount),
                    Expense = g.Where(x => x.Type == "expense").Sum(x => x.Amount)
                }).OrderByDescending(x => x.Revenue).ToList();

            return Results.Ok(new { totalRevenue, totalExpense, netIncome, currency = "VND", byCategory, byStreamer });
        });

        reports.MapGet("/streamer-performance", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();

            var streamers = await db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency).ToListAsync(ct);
            var sessions = await db.LiveSessions.AsNoTracking().Where(x => x.AgencyId == agency).ToListAsync(ct);
            var metrics = await db.SessionMetrics.AsNoTracking().Where(x => x.AgencyId == agency).ToListAsync(ct);

            var result = streamers.Select(st =>
            {
                var sList = sessions.Where(s => s.StreamerId == st.Id).ToList();
                var sIds = sList.Select(s => s.Id).ToHashSet();
                var mList = metrics.Where(m => sIds.Contains(m.LiveSessionId)).ToList();

                var totalViews = mList.Where(m => m.MetricCode == "views").Sum(m => m.Value);
                var peakCcu = mList.Where(m => m.MetricCode == "ccu_peak").Select(m => m.Value).DefaultIfEmpty(0).Max();
                var totalDonations = mList.Where(m => m.MetricCode == "donations").Sum(m => m.Value);

                return new
                {
                    id = st.Id,
                    code = st.Code,
                    stageName = st.StageName,
                    status = st.Status,
                    totalSessions = sList.Count,
                    totalViews,
                    peakCcu,
                    totalDonations,
                    currency = "VND"
                };
            }).OrderByDescending(x => x.totalViews).ToList();

            return Results.Ok(result);
        });

        var channelRoutes = api.MapGroup("/channels").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "OperationsManager"));
        channelRoutes.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var streamers = await db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency).ToDictionaryAsync(x => x.Id, x => x.StageName, ct);
            var list = await db.Channels.AsNoTracking().Where(x => x.AgencyId == agency).OrderBy(x => x.PlatformCode).ToListAsync(ct);
            return Results.Ok(list.Select(c => new
            {
                c.Id,
                c.StreamerId,
                StreamerName = streamers.GetValueOrDefault(c.StreamerId, "Streamer"),
                c.PlatformCode,
                c.ExternalChannelId,
                c.Handle,
                c.Url,
                c.Status
            }));
        });

        var sessionRoutes = api.MapGroup("/sessions").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "OperationsManager"));
        sessionRoutes.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var streamers = await db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency).ToDictionaryAsync(x => x.Id, x => x.StageName, ct);
            var list = await db.LiveSessions.AsNoTracking().Where(x => x.AgencyId == agency).OrderByDescending(x => x.ActualStartAt).Take(200).ToListAsync(ct);
            return Results.Ok(list.Select(s => new
            {
                s.Id,
                s.StreamerId,
                StreamerName = streamers.GetValueOrDefault(s.StreamerId, "Streamer"),
                s.ScheduleId,
                s.ActualStartAt,
                s.ActualEndAt,
                s.Status,
                s.Source
            }));
        });
        sessionRoutes.MapGet("/{id:guid}/metrics", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var metrics = await db.SessionMetrics.AsNoTracking().Where(m => m.AgencyId == agency && m.LiveSessionId == id).OrderBy(m => m.MetricCode).ToListAsync(ct);
            return Results.Ok(metrics);
        });

        var taskRoutes = api.MapGroup("/tasks").RequireAuthorization(policy => policy.RequireRole(
            "Admin", "HRManager", "HRStaff", "TalentManager", "OperationsManager", "Booking", "Marketing", "Finance"));
        taskRoutes.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Tasks.AsNoTracking().Where(x => x.AgencyId == agency && x.Status != "done" && x.Status != "cancelled");
            // Admin & Operations see all tasks; others see their own or their team's tasks
            if (!user.IsInRole("Admin") && !user.IsInRole("OperationsManager"))
            {
                var userId = user.GetUserId();
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var myTeamIds = db.TeamMemberships.Where(m => m.AgencyId == agency && m.UserId == userId &&
                    m.ValidFrom <= today && (m.ValidTo == null || m.ValidTo >= today)).Select(m => m.TeamId);
                query = query.Where(x => x.OwnerUserId == userId || (x.TeamId != null && myTeamIds.Contains(x.TeamId.Value)));
            }
            var rows = await query.OrderBy(x => x.DueAt).Take(500)
                .Select(x => new TaskDto(x.Id, x.Title, x.TeamId, x.OwnerUserId, x.StreamerId, x.BookingId, x.Priority, x.Status, x.DueAt)).ToListAsync(ct);
            return Results.Ok(rows);
        });
        taskRoutes.MapPost("", async (CreateTaskRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 2 or > 180 || request.Priority is not ("low" or "normal" or "high" or "urgent") ||
                request.StreamerId is null && request.BookingId is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = ["Tên, mức ưu tiên hoặc đối tượng công việc không hợp lệ."] });
            if (!await db.Teams.AnyAsync(x => x.Id == request.TeamId && x.AgencyId == agency && x.Status == "active", ct) ||
                !await db.Users.AnyAsync(x => x.Id == request.OwnerUserId && x.AgencyId == agency && x.IsActive, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignee"] = ["Đội hoặc nhân viên không hợp lệ."] });
            if (request.StreamerId is Guid streamerId && !await db.Streamers.AnyAsync(x => x.Id == streamerId && x.AgencyId == agency, ct)) return Results.NotFound();
            if (request.BookingId is Guid bookingId && !await db.Bookings.AnyAsync(x => x.Id == bookingId && x.AgencyId == agency, ct)) return Results.NotFound();
            var row = new TaskItem { Id = Guid.NewGuid(), AgencyId = agency.Value, TeamId = request.TeamId,
                OwnerUserId = request.OwnerUserId, CreatedBy = user.GetUserId(), StreamerId = request.StreamerId,
                BookingId = request.BookingId, Title = request.Title.Trim(), Priority = request.Priority, DueAt = request.DueAt };
            db.Tasks.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "task.created", "task", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/tasks/{row.Id}", new TaskDto(row.Id, row.Title, row.TeamId, row.OwnerUserId, row.StreamerId, row.BookingId, row.Priority, row.Status, row.DueAt));
        });
        taskRoutes.MapPost("/{id:guid}/status", async (Guid id, UpdateTaskStatusRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.Status is not ("open" or "in_progress" or "blocked" or "done" or "cancelled"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Trạng thái công việc không hợp lệ."] });
            var row = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && row.OwnerUserId != user.GetUserId()) return Results.Forbid();
            if (row.Status is "done" or "cancelled") return Results.Conflict(new { message = "Công việc đã đóng, không thể đổi trạng thái." });
            var previous = row.Status;
            row.Status = request.Status;
            var auditLog = user.Audit(agency.Value, "task.status.changed", "task", id);
            auditLog.Summary = $"{previous} -> {request.Status}";
            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        });

        var intake = api.MapGroup("/intake").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "Booking", "Marketing"));
        intake.MapGet("/streamer-applications", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            return Results.Ok(await db.StreamerApplications.AsNoTracking().Where(x => x.Status != "closed")
                .OrderByDescending(x => x.ReceivedAt).Take(200).Select(x => new { x.Id, x.ApplicantName, x.Email, x.PlatformLinks, x.Note, x.Status, x.ReceivedAt }).ToListAsync(ct));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        intake.MapPost("/streamer-applications/{id:guid}/status", async (Guid id, IntakeStatusRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.Status is not ("reviewing" or "shortlisted" or "accepted" or "rejected" or "closed")) return Results.BadRequest();
            var row = await db.StreamerApplications.SingleOrDefaultAsync(x => x.Id == id && x.Status != "closed", ct);
            if (row is null) return Results.NotFound();
            row.Status = request.Status; row.AssignedTo = user.GetUserId();
            db.AuditLogs.Add(user.Audit(agency.Value, "intake.streamer_application.status_changed", "streamer_application", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        intake.MapGet("/partnership-requests", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            return Results.Ok(await db.PartnershipRequests.AsNoTracking().Where(x => x.Status != "closed")
                .OrderByDescending(x => x.ReceivedAt).Take(200).Select(x => new { x.Id, x.OrganizationName, x.ContactName, x.Email, x.Summary, x.Status, x.ReceivedAt }).ToListAsync(ct));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Booking", "Marketing"));
        intake.MapPost("/partnership-requests/{id:guid}/status", async (Guid id, IntakeStatusRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.Status is not ("reviewing" or "shortlisted" or "accepted" or "rejected" or "closed")) return Results.BadRequest();
            var row = await db.PartnershipRequests.SingleOrDefaultAsync(x => x.Id == id && x.Status != "closed", ct);
            if (row is null) return Results.NotFound();
            row.Status = request.Status; row.AssignedTo = user.GetUserId();
            db.AuditLogs.Add(user.Audit(agency.Value, "intake.partnership_request.status_changed", "partnership_request", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Booking", "Marketing"));

        var teams = api.MapGroup("/teams").RequireAuthorization(policy => policy.RequireRole("Admin", "HRManager"));
        teams.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            return Results.Ok(await db.Teams.AsNoTracking().Where(x => x.AgencyId == agency && x.Status == "active")
                .OrderBy(x => x.Name).Select(x => new TeamDto(x.Id, x.Code, x.Name, x.ParentTeamId)).ToListAsync(ct));
        });
        teams.MapPost("", async (CreateTeamRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest();
            var code = request.Code.Trim().ToUpperInvariant();
            var name = request.Name.Trim();
            if (code.Length is < 2 or > 32 || name.Length is < 2 or > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["team"] = ["Mã đội hoặc tên đội không hợp lệ."] });
            if (await db.Teams.AnyAsync(x => x.AgencyId == agency && x.Code == code, ct))
                return Results.Conflict(new { message = "Mã đội đã tồn tại." });
            var team = new Team { Id = Guid.NewGuid(), AgencyId = agency.Value, Code = code, Name = name, ParentTeamId = request.ParentTeamId };
            if (team.ParentTeamId is not null && !await db.Teams.AnyAsync(x => x.Id == team.ParentTeamId && x.AgencyId == agency, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentTeamId"] = ["Đội cha không hợp lệ."] });
            db.Teams.Add(team);
            db.AuditLogs.Add(user.Audit(agency.Value, "team.created", "team", team.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/teams/{team.Id}", new TeamDto(team.Id, team.Code, team.Name, team.ParentTeamId));
        });
        teams.MapPost("/{id:guid}/members", async (Guid id, AddTeamMemberRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (!await db.Teams.AnyAsync(x => x.Id == id && x.AgencyId == agency, ct) ||
                !await db.Users.AnyAsync(x => x.Id == request.UserId && x.AgencyId == agency && x.IsActive, ct)) return Results.NotFound();
            if (request.ValidTo is not null && request.ValidTo.Value < request.ValidFrom) return Results.BadRequest();
            var row = new TeamMembership { Id = Guid.NewGuid(), AgencyId = agency.Value, TeamId = id, UserId = request.UserId,
                MembershipRole = request.MembershipRole, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo };
            db.TeamMemberships.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "team.member.added", "team_membership", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/teams/{id}/members/{row.Id}", new { row.Id, row.UserId, row.MembershipRole, row.ValidFrom, row.ValidTo });
        });

        var employees = api.MapGroup("/employees").RequireAuthorization(policy => policy.RequireRole("Admin", "HRManager", "HRStaff"));
        employees.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            return Results.Ok(await db.EmployeeProfiles.AsNoTracking().Where(x => x.AgencyId == agency)
                .OrderBy(x => x.EmployeeCode).Select(x => new EmployeeDto(x.Id, x.EmployeeCode, x.JobTitle, x.HireDate, x.EndDate, x.EmploymentStatus)).ToListAsync(ct));
        });
        employees.MapPost("", async (CreateEmployeeRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.EmployeeCode) || string.IsNullOrWhiteSpace(request.JobTitle) ||
                request.EmployeeCode.Trim().Length is < 2 or > 32 || request.JobTitle.Trim().Length is < 2 or > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["employee"] = ["Mã nhân viên hoặc chức danh không hợp lệ."] });
            if (!await db.Users.AnyAsync(x => x.Id == request.UserId && x.AgencyId == agency && x.IsActive, ct)) return Results.NotFound();
            if (await db.EmployeeProfiles.AnyAsync(x => x.AgencyId == agency && (x.UserId == request.UserId || x.EmployeeCode == request.EmployeeCode.Trim().ToUpperInvariant()), ct))
                return Results.Conflict(new { message = "Tài khoản đã có hồ sơ HR hoặc mã nhân viên bị trùng." });
            var row = new EmployeeProfile { Id = Guid.NewGuid(), AgencyId = agency.Value, UserId = request.UserId,
                EmployeeCode = request.EmployeeCode.Trim().ToUpperInvariant(), JobTitle = request.JobTitle.Trim(), HireDate = request.HireDate };
            db.EmployeeProfiles.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "employee.profile.created", "employee_profile", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/employees/{row.Id}", new EmployeeDto(row.Id, row.EmployeeCode, row.JobTitle, row.HireDate, row.EndDate, row.EmploymentStatus));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "HRManager"));

        var streamers = api.MapGroup("/streamers").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "OperationsManager", "Analyst"));
        streamers.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Streamers.AsNoTracking().Where(x => x.AgencyId == agency && x.Status != "ended");
            if (!user.IsInRole("Admin") && !user.IsInRole("TalentManager") && !user.IsInRole("OperationsManager") && !user.IsInRole("Analyst"))
            {
                var visibleIds = VisibleStreamerIds(db, agency.Value, user.GetUserId());
                query = query.Where(x => visibleIds.Contains(x.Id));
            }
            var rows = await query
                .OrderBy(x => x.StageName).Select(x => new StreamerDto(x.Id, x.Code, x.StageName, x.Status, x.TimeZone)).ToListAsync(ct);
            return Results.Ok(rows);
        });
        streamers.MapPost("", async (CreateStreamerRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.StageName)) return Results.BadRequest();
            var code = request.Code.Trim().ToUpperInvariant();
            var name = request.StageName.Trim();
            if (code.Length is < 2 or > 32 || name.Length is < 2 or > 120 || string.IsNullOrWhiteSpace(request.TimeZone) || request.TimeZone.Length > 80)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamer"] = ["Mã, nghệ danh hoặc múi giờ không hợp lệ."] });
            if (await db.Streamers.AnyAsync(x => x.AgencyId == agency && x.Code == code, ct))
                return Results.Conflict(new { message = "Mã streamer đã tồn tại." });
            var streamer = new Streamer { Id = Guid.NewGuid(), AgencyId = agency.Value, Code = code, StageName = name, TimeZone = request.TimeZone.Trim() };
            db.Streamers.Add(streamer);
            db.AuditLogs.Add(user.Audit(agency.Value, "streamer.created", "streamer", streamer.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/streamers/{streamer.Id}", new StreamerDto(streamer.Id, code, name, streamer.Status, streamer.TimeZone));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        streamers.MapPost("/{id:guid}/onboarding", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Streamers.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.Status != "lead") return Results.Conflict(new { message = "Chỉ hồ sơ lead mới chuyển sang onboarding." });
            row.Status = "onboarding";
            db.AuditLogs.Add(user.Audit(agency.Value, "streamer.onboarding.started", "streamer", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        streamers.MapPost("/{id:guid}/contracts", async (Guid id, CreateContractRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.ContractReference) || request.ContractReference.Trim().Length is < 2 or > 120 || request.ValidTo is not null && request.ValidTo.Value < request.ValidFrom ||
                request.StreamerRate is < 0 or > 1 || request.AgencyRate is < 0 or > 1 || request.StreamerRate + request.AgencyRate != 1)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["contract"] = ["Điều khoản, thời hạn hoặc tỷ lệ chia không hợp lệ."] });
            var streamer = await db.Streamers.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (streamer is null) return Results.NotFound();
            if (streamer.Status != "onboarding") return Results.Conflict(new { message = "Streamer cần hoàn tất bước tiếp nhận trước khi tạo hợp đồng." });
            if (await db.Contracts.AnyAsync(x => x.AgencyId == agency && x.ContractReference == request.ContractReference.Trim(), ct))
                return Results.Conflict(new { message = "Mã tham chiếu hợp đồng đã tồn tại." });
            var contract = new Contract { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = id,
                ContractReference = request.ContractReference.Trim(), ValidFrom = request.ValidFrom, ValidTo = request.ValidTo,
                SignedAt = request.SignedAt, StreamerRate = request.StreamerRate, AgencyRate = request.AgencyRate,
                CreatedBy = user.GetUserId(), Status = "active" };
            var rule = new RevenueShareRule { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = id,
                RevenueType = "all", StreamerRate = request.StreamerRate, AgencyRate = request.AgencyRate,
                ValidFrom = request.ValidFrom, ValidTo = request.ValidTo, CreatedBy = user.GetUserId(), Status = "pending_approval" };
            db.Contracts.Add(contract);
            db.RevenueShareRules.Add(rule);
            db.AuditLogs.Add(user.Audit(agency.Value, "streamer.contract.signed", "contract", contract.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/streamers/{id}/contracts/{contract.Id}", new
            { contract.Id, contract.ContractReference, contract.ValidFrom, contract.ValidTo, contract.Status, shareRuleId = rule.Id, shareRuleStatus = rule.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        streamers.MapPost("/{id:guid}/activate", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var streamer = await db.Streamers.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (streamer is null) return Results.NotFound();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var hasContract = await db.Contracts.AnyAsync(x => x.AgencyId == agency && x.StreamerId == id && x.Status == "active" &&
                x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today), ct);
            var hasRule = await db.RevenueShareRules.AnyAsync(x => x.AgencyId == agency && x.StreamerId == id && x.Status == "active" &&
                x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today), ct);
            if (!hasContract || !hasRule) return Results.Conflict(new { message = "Cần hợp đồng có hiệu lực và quy tắc chia đã duyệt trước khi kích hoạt." });
            if (streamer.Status is not ("onboarding" or "paused")) return Results.Conflict(new { message = "Trạng thái hiện tại không thể kích hoạt." });
            streamer.Status = "active";
            streamer.UpdatedAt = DateTimeOffset.UtcNow;
            db.AuditLogs.Add(user.Audit(agency.Value, "streamer.activated", "streamer", id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { streamer.Id, streamer.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager"));
        streamers.MapPost("/{id:guid}/assignments", async (Guid id, AssignmentRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (!await db.Streamers.AnyAsync(x => x.Id == id && x.AgencyId == agency, ct)) return Results.NotFound();
            if (request.UserId is null && request.TeamId is null) return Results.ValidationProblem(new Dictionary<string, string[]> { ["assignment"] = ["Cần gán người dùng hoặc đội."] });
            if (request.UserId is Guid uid && !await db.Users.AnyAsync(x => x.Id == uid && x.AgencyId == agency, ct)) return Results.NotFound();
            if (request.TeamId is Guid tid && !await db.Teams.AnyAsync(x => x.Id == tid && x.AgencyId == agency, ct)) return Results.NotFound();
            var assignment = new StreamerAssignment { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = id,
                UserId = request.UserId, TeamId = request.TeamId, AssignmentRole = request.RoleCode,
                ValidFrom = request.ValidFrom, ValidTo = request.ValidTo };
            db.StreamerAssignments.Add(assignment);
            db.AuditLogs.Add(user.Audit(agency.Value, "streamer.assignment.created", "streamer", id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/streamers/{id}/assignments/{assignment.Id}", new { assignment.Id, assignment.UserId, assignment.TeamId, assignment.AssignmentRole, assignment.ValidFrom, assignment.ValidTo });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));

        var schedules = api.MapGroup("/schedules").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "OperationsManager"));
        schedules.MapGet("", async (DateTimeOffset? from, DateTimeOffset? to, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.LiveSchedules.AsNoTracking().Where(x => x.AgencyId == agency);
            if (!user.IsInRole("Admin") && !user.IsInRole("TalentManager") && !user.IsInRole("OperationsManager"))
            {
                var visibleIds = VisibleStreamerIds(db, agency.Value, user.GetUserId());
                query = query.Where(x => visibleIds.Contains(x.StreamerId));
            }
            if (from is not null) query = query.Where(x => x.PlannedStartAt >= from.Value);
            if (to is not null) query = query.Where(x => x.PlannedStartAt < to.Value);
            return Results.Ok(await query.OrderBy(x => x.PlannedStartAt).Take(500)
                .Select(x => new ScheduleDto(x.Id, x.StreamerId, x.Title, x.PlannedStartAt, x.PlannedEndAt, x.Status)).ToListAsync(ct));
        });
        schedules.MapPost("", async (CreateScheduleRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.PlannedEndAt <= request.PlannedStartAt || string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 2 or > 180)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["schedule"] = ["Tiêu đề hoặc khoảng thời gian không hợp lệ."] });
            if (!await db.Streamers.AnyAsync(x => x.Id == request.StreamerId && x.AgencyId == agency && x.Status == "active", ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamerId"] = ["Streamer không tồn tại hoặc chưa hoạt động."] });
            if (!user.IsInRole("Admin") && !user.IsInRole("OperationsManager") && !user.IsInRole("TalentManager"))
            {
                if (!await VisibleStreamerIds(db, agency.Value, user.GetUserId()).AnyAsync(x => x == request.StreamerId, ct)) return Results.Forbid();
            }
            var conflict = await db.LiveSchedules.AnyAsync(x => x.AgencyId == agency && x.StreamerId == request.StreamerId && x.Status != "cancelled" &&
                x.PlannedStartAt < request.PlannedEndAt && request.PlannedStartAt < x.PlannedEndAt, ct);
            if (conflict) return Results.Conflict(new { message = "Streamer đã có lịch bị trùng thời gian." });
            var row = new LiveSchedule { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = request.StreamerId,
                ChannelId = request.ChannelId, Title = request.Title.Trim(), PlannedStartAt = request.PlannedStartAt,
                PlannedEndAt = request.PlannedEndAt, CreatedBy = user.GetUserId() };
            db.LiveSchedules.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "schedule.created", "schedule", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/schedules/{row.Id}", new ScheduleDto(row.Id, row.StreamerId, row.Title, row.PlannedStartAt, row.PlannedEndAt, row.Status));
        });

        var sessions = api.MapGroup("/sessions").RequireAuthorization(policy => policy.RequireRole("Admin", "TalentManager", "OperationsManager"));
        sessions.MapPost("", async (CreateSessionRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (!await db.Streamers.AnyAsync(x => x.Id == request.StreamerId && x.AgencyId == agency, ct)) return Results.NotFound();
            if (!user.IsInRole("Admin") && !user.IsInRole("OperationsManager") && !user.IsInRole("TalentManager"))
            {
                if (!await VisibleStreamerIds(db, agency.Value, user.GetUserId()).AnyAsync(x => x == request.StreamerId, ct)) return Results.Forbid();
            }
            if (request.ScheduleId is Guid sid && !await db.LiveSchedules.AnyAsync(x => x.Id == sid && x.AgencyId == agency && x.StreamerId == request.StreamerId, ct)) return Results.NotFound();
            var row = new LiveSession { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = request.StreamerId,
                ScheduleId = request.ScheduleId, ChannelId = request.ChannelId, ActualStartAt = request.ActualStartAt,
                ActualEndAt = request.ActualEndAt, Status = request.Status == "completed" ? "completed" : "live",
                RecordedBy = user.GetUserId() };
            if (row.ActualStartAt is not null && (row.ActualEndAt is null || row.ActualEndAt.Value <= row.ActualStartAt.Value))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["actualEndAt"] = ["Thời điểm kết thúc phải sau thời điểm bắt đầu."] });
            db.LiveSessions.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "session.created", "live_session", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/sessions/{row.Id}", new { row.Id, row.StreamerId, row.Status, row.ActualStartAt, row.ActualEndAt });
        });
        sessions.MapPost("/{id:guid}/metrics", async (Guid id, AddMetricRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var sessionExists = await db.LiveSessions.AnyAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (!sessionExists) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(request.MetricCode) || request.MetricCode.Length > 60 || string.IsNullOrWhiteSpace(request.Unit) || request.Unit.Length > 32 || request.Value < 0)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["metric"] = ["Chỉ số, đơn vị hoặc giá trị không hợp lệ."] });
            var row = new SessionMetric { Id = Guid.NewGuid(), AgencyId = agency.Value, LiveSessionId = id,
                MetricCode = request.MetricCode.Trim().ToLowerInvariant(), Unit = request.Unit.Trim(), Value = request.Value,
                Source = "manual", EnteredBy = user.GetUserId() };
            db.SessionMetrics.Add(row);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/sessions/{id}/metrics/{row.Id}", new { row.Id, row.MetricCode, row.Value, row.Unit, row.CapturedAt });
        });

        var customers = api.MapGroup("/customers").RequireAuthorization(policy => policy.RequireRole("Admin", "Booking", "Marketing"));
        customers.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            return Results.Ok(await db.Customers.AsNoTracking().Where(x => x.AgencyId == agency && x.Status == "active")
                .OrderBy(x => x.Name).Select(x => new CustomerDto(x.Id, x.Name, x.ContactName, x.Email)).Take(500).ToListAsync(ct));
        });
        customers.MapPost("", async (CreateCustomerRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length is < 2 or > 180) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Tên khách hàng không hợp lệ."] });
            var row = new Customer { Id = Guid.NewGuid(), AgencyId = agency.Value, Name = request.Name.Trim(), ContactName = Limit(request.ContactName, 160), Email = Limit(request.Email, 254) };
            db.Customers.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "customer.created", "customer", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/customers/{row.Id}", new CustomerDto(row.Id, row.Name, row.ContactName, row.Email));
        });

        var bookings = api.MapGroup("/bookings").RequireAuthorization(policy => policy.RequireRole("Admin", "Booking", "Marketing"));
        bookings.MapGet("", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.Bookings.AsNoTracking().Where(x => x.AgencyId == agency);
            // Admin sees all; Booking role sees all; others see only their own
            if (!user.IsInRole("Admin") && !user.IsInRole("Booking"))
                query = query.Where(x => x.OwnerUserId == user.GetUserId());
            return Results.Ok(await query.OrderByDescending(x => x.CreatedAt)
                .Take(500).Select(x => new BookingDto(x.Id, x.Title, x.CustomerId, x.Status, x.GrossAmount, x.Currency)).ToListAsync(ct));
        });
        bookings.MapPost("", async (CreateBookingRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length is < 2 or > 180 ||
                request.GrossAmount < 0 || request.GrossAmount > 999_999_999_999_999.9999m ||
                string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3 || !request.Currency.All(char.IsAsciiLetter))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["booking"] = ["Thông tin booking không hợp lệ."] });
            if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.AgencyId == agency, ct)) return Results.NotFound();
            var row = new Booking { Id = Guid.NewGuid(), AgencyId = agency.Value, CustomerId = request.CustomerId,
                OwnerUserId = user.GetUserId(), Title = request.Title.Trim(), GrossAmount = request.GrossAmount,
                Currency = request.Currency.ToUpperInvariant(), StartsAt = request.StartsAt, EndsAt = request.EndsAt };
            if (row.StartsAt is not null && (row.EndsAt is null || row.EndsAt.Value <= row.StartsAt.Value))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["endsAt"] = ["Khi có thời điểm bắt đầu, thời điểm kết thúc phải tồn tại và ở phía sau."] });
            db.Bookings.Add(row);
            var streamerIds = request.StreamerIds ?? [];
            if (streamerIds.Length > 20 || streamerIds.Distinct().Count() != streamerIds.Length)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamerIds"] = ["Chọn tối đa 20 streamer duy nhất."] });
            if (streamerIds.Length > 0)
            {
                var matching = await db.Streamers.CountAsync(x => x.AgencyId == agency && streamerIds.Contains(x.Id) && x.Status == "active", ct);
                if (matching != streamerIds.Length) return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamerIds"] = ["Một hoặc nhiều streamer không hợp lệ."] });
                if (!user.IsInRole("Admin"))
                {
                    var visibleIds = VisibleStreamerIds(db, agency.Value, user.GetUserId());
                    if (await visibleIds.CountAsync(x => streamerIds.Contains(x), ct) != streamerIds.Length) return Results.Forbid();
                }
                db.BookingStreamers.AddRange(streamerIds.Select(streamerId => new BookingStreamer
                { Id = Guid.NewGuid(), AgencyId = agency.Value, BookingId = row.Id, StreamerId = streamerId, Currency = row.Currency }));
            }
            db.AuditLogs.Add(user.Audit(agency.Value, "booking.created", "booking", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/bookings/{row.Id}", new BookingDto(row.Id, row.Title, row.CustomerId, row.Status, row.GrossAmount, row.Currency));
        });
        bookings.MapPost("/{id:guid}/confirm", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && row.OwnerUserId != user.GetUserId()) return Results.Forbid();
            if (row.Status != "proposed") return Results.Conflict(new { message = "Chỉ booking ở trạng thái đề xuất mới được xác nhận." });
            row.Status = "confirmed";
            db.AuditLogs.Add(user.Audit(agency.Value, "booking.confirmed", "booking", id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        });
        bookings.MapPost("/{id:guid}/start", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && row.OwnerUserId != user.GetUserId()) return Results.Forbid();
            if (row.Status != "confirmed") return Results.Conflict(new { message = "Chỉ booking đã xác nhận mới được bắt đầu." });
            row.Status = "in_progress";
            db.AuditLogs.Add(user.Audit(agency.Value, "booking.started", "booking", id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        });
        bookings.MapGet("/{id:guid}/deliverables", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (booking is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && booking.OwnerUserId != user.GetUserId()) return Results.Forbid();
            var rows = await db.BookingDeliverables.AsNoTracking().Where(x => x.AgencyId == agency && x.BookingId == id)
                .OrderBy(x => x.DueAt).Select(x => new { x.Id, x.Description, x.DueAt, x.Status, x.AcceptedBy, x.AcceptedAt }).ToListAsync(ct);
            return Results.Ok(rows);
        });
        bookings.MapPost("/{id:guid}/deliverables", async (Guid id, CreateDeliverableRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (booking is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && booking.OwnerUserId != user.GetUserId()) return Results.Forbid();
            if (booking.Status is not ("confirmed" or "in_progress") || string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length is < 2 or > 500)
                return Results.Conflict(new { message = "Booking chưa xác nhận hoặc nội dung deliverable không hợp lệ." });
            var row = new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agency.Value, BookingId = id,
                Description = request.Description.Trim(), DueAt = request.DueAt };
            db.BookingDeliverables.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "booking.deliverable.created", "booking_deliverable", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/bookings/{id}/deliverables/{row.Id}", new { row.Id, row.Description, row.DueAt, row.Status });
        });
        bookings.MapPost("/{bookingId:guid}/deliverables/{deliverableId:guid}/accept", async (Guid bookingId, Guid deliverableId, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var booking = await db.Bookings.SingleOrDefaultAsync(x => x.Id == bookingId && x.AgencyId == agency, ct);
            var row = await db.BookingDeliverables.SingleOrDefaultAsync(x => x.Id == deliverableId && x.BookingId == bookingId && x.AgencyId == agency, ct);
            if (booking is null || row is null) return Results.NotFound();
            if (!user.IsInRole("Admin") && booking.OwnerUserId != user.GetUserId()) return Results.Forbid();
            if (booking.Status is not ("confirmed" or "in_progress") || row.Status != "pending") return Results.Conflict(new { message = "Deliverable không ở trạng thái có thể nghiệm thu." });
            row.Status = "accepted"; row.AcceptedBy = user.GetUserId(); row.AcceptedAt = DateTimeOffset.UtcNow;
            var remaining = await db.BookingDeliverables.AnyAsync(x => x.AgencyId == agency && x.BookingId == bookingId && x.Id != deliverableId && x.Status != "accepted", ct);
            if (!remaining) booking.Status = "delivered";
            db.AuditLogs.Add(user.Audit(agency.Value, "booking.deliverable.accepted", "booking_deliverable", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status, bookingStatus = booking.Status });
        });

        var finance = api.MapGroup("/finance").RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapGet("/transactions", async (DateTimeOffset? from, DateTimeOffset? to, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.FinancialTransactions.AsNoTracking().Where(x => x.AgencyId == agency);
            if (from is not null) query = query.Where(x => x.OccurredAt >= from.Value);
            if (to is not null) query = query.Where(x => x.OccurredAt < to.Value);
            return Results.Ok(await query.OrderByDescending(x => x.OccurredAt).Take(1000)
                .Select(x => new TransactionDto(x.Id, x.Type, x.Category, x.Amount, x.Currency, x.OccurredAt, x.Status, x.StreamerId)).ToListAsync(ct));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapPost("/transactions", async (CreateTransactionRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.Amount <= 0 || request.Amount > 999_999_999_999_999.9999m ||
                string.IsNullOrWhiteSpace(request.Category) || request.Category.Trim().Length > 64 ||
                string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3 ||
                !request.Currency.All(char.IsAsciiLetter) || request.Type is not ("revenue" or "expense"))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["transaction"] = ["Loại, số tiền hoặc tiền tệ không hợp lệ."] });
            if (request.StreamerId is Guid sid && !await db.Streamers.AnyAsync(x => x.Id == sid && x.AgencyId == agency, ct)) return Results.NotFound();
            if (request.BookingId is Guid bid && !await db.Bookings.AnyAsync(x => x.Id == bid && x.AgencyId == agency, ct)) return Results.NotFound();
            var row = new FinancialTransaction { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = request.StreamerId,
                BookingId = request.BookingId, Type = request.Type, Category = request.Category.Trim(), Amount = request.Amount,
                Currency = request.Currency.ToUpperInvariant(), OccurredAt = request.OccurredAt, Reference = Limit(request.Reference, 120),
                RecordedBy = user.GetUserId() };
            db.FinancialTransactions.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.transaction.created", "financial_transaction", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/transactions/{row.Id}", new TransactionDto(row.Id, row.Type, row.Category, row.Amount, row.Currency, row.OccurredAt, row.Status, row.StreamerId));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapPost("/transactions/{id:guid}/post", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.FinancialTransactions.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.Status != "draft") return Results.Conflict(new { message = "Chỉ giao dịch nháp mới được ghi sổ." });
            row.Status = "posted";
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.transaction.posted", "financial_transaction", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapPost("/share-rules", async (CreateShareRuleRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.RevenueType != "all" || request.StreamerRate is < 0 or > 1 || request.AgencyRate is < 0 or > 1 || request.StreamerRate + request.AgencyRate != 1 ||
                request.ValidTo is not null && request.ValidTo.Value < request.ValidFrom)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["rule"] = ["Bản dựng chỉ hỗ trợ quy tắc tổng hợp 'all'; tỷ lệ phải nằm trong khoảng 0–1, tổng bằng 1 và thời hạn hợp lệ."] });
            if (!await db.Streamers.AnyAsync(x => x.Id == request.StreamerId && x.AgencyId == agency && x.Status == "active", ct)) return Results.NotFound();
            var overlaps = await db.RevenueShareRules.AnyAsync(x => x.AgencyId == agency && x.StreamerId == request.StreamerId && x.RevenueType == request.RevenueType && x.Status != "rejected" &&
                (request.ValidTo == null || x.ValidFrom <= request.ValidTo.Value) && (x.ValidTo == null || x.ValidTo >= request.ValidFrom), ct);
            if (overlaps) return Results.Conflict(new { message = "Thời hạn bị trùng với quy tắc chia đã có. Hãy kết thúc quy tắc cũ trước." });
            var row = new RevenueShareRule { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = request.StreamerId,
                RevenueType = request.RevenueType.Trim().ToLowerInvariant(), StreamerRate = request.StreamerRate,
                AgencyRate = request.AgencyRate, ValidFrom = request.ValidFrom, ValidTo = request.ValidTo,
                CreatedBy = user.GetUserId() };
            db.RevenueShareRules.Add(row);
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.share_rule.created", "revenue_share_rule", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/share-rules/{row.Id}", new { row.Id, row.StreamerId, row.StreamerRate, row.AgencyRate, row.ValidFrom, row.ValidTo, row.Status });
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
        finance.MapPost("/share-rules/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.RevenueShareRules.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.CreatedBy == user.GetUserId()) return Results.Forbid();
            if (row.Status != "pending_approval") return Results.Conflict(new { message = "Quy tắc này không ở trạng thái chờ duyệt." });
            row.ApprovedBy = user.GetUserId();
            row.Status = "active";
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var contractActive = await db.Contracts.AnyAsync(x => x.AgencyId == agency && x.StreamerId == row.StreamerId && x.Status == "active" &&
                x.ValidFrom == row.ValidFrom && x.ValidTo == row.ValidTo && x.StreamerRate == row.StreamerRate && x.AgencyRate == row.AgencyRate, ct);
            if (contractActive && row.ValidFrom <= today && (row.ValidTo == null || row.ValidTo.Value >= today))
            {
                var streamer = await db.Streamers.SingleAsync(x => x.Id == row.StreamerId && x.AgencyId == agency, ct);
                streamer.Status = "active";
            }
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.share_rule.approved", "revenue_share_rule", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Status, row.ApprovedBy });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "FinanceApprover"));
        finance.MapPost("/settlements", async (CreateSettlementRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            if (request.PeriodEnd < request.PeriodStart || request.PeriodEnd.DayNumber - request.PeriodStart.DayNumber > 366 ||
                !await db.Streamers.AnyAsync(x => x.Id == request.StreamerId && x.AgencyId == agency, ct))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["period"] = ["Streamer hoặc kỳ đối soát không hợp lệ."] });
            if (await db.Settlements.AnyAsync(x => x.AgencyId == agency && x.StreamerId == request.StreamerId && x.PeriodStart == request.PeriodStart && x.PeriodEnd == request.PeriodEnd, ct))
                return Results.Conflict(new { message = "Kỳ đối soát cho streamer này đã tồn tại." });

            var from = request.PeriodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var until = request.PeriodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var transactions = await db.FinancialTransactions.Where(x => x.AgencyId == agency && x.StreamerId == request.StreamerId &&
                x.Status == "posted" && x.OccurredAt >= from && x.OccurredAt < until).ToListAsync(ct);
            if (transactions.Count == 0) return Results.Conflict(new { message = "Không có giao dịch đã ghi sổ trong kỳ." });
            if (transactions.Select(x => x.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1)
                return Results.Conflict(new { message = "Kỳ có nhiều loại tiền tệ; cần tách settlement theo từng currency." });
            var existingLineIds = await db.SettlementLines.Where(x => x.AgencyId == agency).Select(x => x.TransactionId).ToListAsync(ct);
            if (transactions.Any(x => existingLineIds.Contains(x.Id)))
                return Results.Conflict(new { message = "Có giao dịch đã được đưa vào kỳ đối soát khác." });
            var rule = await db.RevenueShareRules.Where(x => x.AgencyId == agency && x.StreamerId == request.StreamerId && x.Status == "active" &&
                x.ValidFrom <= request.PeriodStart && (x.ValidTo == null || x.ValidTo >= request.PeriodEnd))
                .OrderByDescending(x => x.ValidFrom).FirstOrDefaultAsync(ct);
            if (rule is null || rule.RevenueType != "all")
                return Results.Conflict(new { message = "Kỳ chưa có một quy tắc chia tổng hợp đang hiệu lực. Quy tắc nhiều loại doanh thu cần được cấu hình riêng." });
            var gross = transactions.Where(x => x.Type == "revenue").Sum(x => x.Amount);
            var deductions = transactions.Where(x => x.Type == "expense").Sum(x => x.Amount);
            var distributable = gross - deductions;
            if (distributable < 0) return Results.Conflict(new { message = "Chi phí vượt doanh thu; Finance cần xử lý ngoại lệ trước khi đối soát." });
            var currency = transactions[0].Currency;
            var row = new Settlement { Id = Guid.NewGuid(), AgencyId = agency.Value, StreamerId = request.StreamerId,
                PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, Currency = currency,
                GrossRevenue = gross, Deductions = deductions,
                StreamerShare = decimal.Round(distributable * rule.StreamerRate, 2, MidpointRounding.AwayFromZero),
                AgencyShare = distributable - decimal.Round(distributable * rule.StreamerRate, 2, MidpointRounding.AwayFromZero),
                RuleSnapshot = JsonSerializer.Serialize(new { rule.Id, rule.RevenueType, rule.StreamerRate, rule.AgencyRate, rule.ValidFrom, rule.ValidTo }),
                CreatedBy = user.GetUserId() };
            db.Settlements.Add(row);
            db.SettlementLines.AddRange(transactions.Select(tx => new SettlementLine { Id = Guid.NewGuid(), AgencyId = agency.Value,
                SettlementId = row.Id, TransactionId = tx.Id, Amount = tx.Type == "expense" ? -tx.Amount : tx.Amount, LineType = tx.Type }));
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.settlement.created", "settlement", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/settlements/{row.Id}", SettlementView(row));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapGet("/settlements", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var rows = await db.Settlements.AsNoTracking().Where(x => x.AgencyId == agency).OrderByDescending(x => x.PeriodStart)
                .Take(500).ToListAsync(ct);
            return Results.Ok(rows.Select(SettlementView));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapPost("/settlements/{id:guid}/submit", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Settlements.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.Status != "draft") return Results.Conflict(new { message = "Chỉ settlement nháp mới được gửi duyệt." });
            row.Status = "submitted";
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.settlement.submitted", "settlement", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(SettlementView(row));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));
        finance.MapPost("/settlements/{id:guid}/approve", async (Guid id, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Settlements.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.CreatedBy == user.GetUserId()) return Results.Forbid();
            if (row.Status != "submitted") return Results.Conflict(new { message = "Settlement chưa được gửi duyệt hoặc đã xử lý." });
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            row.Status = "locked";
            row.ApprovedBy = user.GetUserId();
            row.ApprovedAt = DateTimeOffset.UtcNow;
            row.LockedAt = row.ApprovedAt;
            db.Payouts.Add(new Payout { Id = Guid.NewGuid(), AgencyId = agency.Value, SettlementId = row.Id,
                Amount = row.StreamerShare, Currency = row.Currency, Status = "pending", RecordedBy = user.GetUserId() });
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.settlement.approved_and_locked", "settlement", row.Id));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(SettlementView(row));
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "FinanceApprover"));
        finance.MapPost("/payouts/{id:guid}/mark-paid", async (Guid id, MarkPayoutPaidRequest request, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var row = await db.Payouts.SingleOrDefaultAsync(x => x.Id == id && x.AgencyId == agency, ct);
            if (row is null) return Results.NotFound();
            if (row.Status != "pending" || string.IsNullOrWhiteSpace(request.PaymentReference) || request.PaymentReference.Length > 160)
                return Results.Conflict(new { message = "Payout không ở trạng thái chờ thanh toán hoặc mã tham chiếu không hợp lệ." });
            row.Status = "paid";
            row.PaymentReference = request.PaymentReference.Trim();
            row.PaidAt = DateTimeOffset.UtcNow;
            db.AuditLogs.Add(user.Audit(agency.Value, "finance.payout.marked_paid", "payout", row.Id));
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { row.Id, row.Amount, row.Currency, row.Status, row.PaymentReference, row.PaidAt });
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));

        finance.MapGet("/payouts", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var payouts = await (from p in db.Payouts.AsNoTracking()
                                 join s in db.Settlements.AsNoTracking() on p.SettlementId equals s.Id
                                 join st in db.Streamers.AsNoTracking() on s.StreamerId equals st.Id
                                 where p.AgencyId == agency
                                 orderby p.PaidAt descending
                                 select new
                                 {
                                     p.Id,
                                     p.SettlementId,
                                     StreamerName = st.StageName,
                                     Period = $"{s.PeriodStart:dd/MM/yyyy} - {s.PeriodEnd:dd/MM/yyyy}",
                                     p.Amount,
                                     p.Currency,
                                     p.Status,
                                     p.PaymentReference,
                                     p.PaidAt
                                 }).ToListAsync(ct);
            return Results.Ok(payouts);
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));

        finance.MapGet("/share-rules", async (ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var rules = await (from r in db.RevenueShareRules.AsNoTracking()
                               join st in db.Streamers.AsNoTracking() on r.StreamerId equals st.Id
                               where r.AgencyId == agency
                               orderby r.ValidFrom descending
                               select new
                               {
                                   r.Id,
                                   r.StreamerId,
                                   StreamerName = st.StageName,
                                   r.RevenueType,
                                   StreamerPercent = (r.StreamerRate * 100).ToString("0.#") + "%",
                                   AgencyPercent = (r.AgencyRate * 100).ToString("0.#") + "%",
                                   r.ValidFrom,
                                   r.ValidTo,
                                   r.Status
                               }).ToListAsync(ct);
            return Results.Ok(rules);
        }).RequireAuthorization(policy => policy.RequireRole("Admin", "Finance", "FinanceApprover"));

        var audit = api.MapGroup("/audit-logs").RequireAuthorization(policy => policy.RequireRole("Admin", "Auditor"));
        audit.MapGet("", async (DateTimeOffset? before, ClaimsPrincipal user, StreamOpsDbContext db, CancellationToken ct) =>
        {
            var agency = user.GetAgencyId();
            if (agency is null) return Results.Forbid();
            var query = db.AuditLogs.AsNoTracking().Where(x => x.AgencyId == agency);
            if (before is not null) query = query.Where(x => x.OccurredAt < before);
            return Results.Ok(await query.OrderByDescending(x => x.OccurredAt).Take(200)
                .Select(x => new { x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId, x.OccurredAt, x.Summary }).ToListAsync(ct));
        });

        return endpoints;
    }

    private static Guid GetUserId(this ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static IQueryable<Guid> VisibleStreamerIds(StreamOpsDbContext db, Guid agencyId, Guid userId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var teams = db.TeamMemberships.Where(m => m.AgencyId == agencyId && m.UserId == userId &&
            m.ValidFrom <= today && (m.ValidTo == null || m.ValidTo >= today)).Select(m => m.TeamId);
        return db.StreamerAssignments.Where(a => a.AgencyId == agencyId && a.ValidFrom <= today &&
            (a.ValidTo == null || a.ValidTo >= today) && (a.UserId == userId || (a.TeamId != null && teams.Contains(a.TeamId.Value))))
            .Select(a => a.StreamerId).Distinct();
    }
    private static AuditLog Audit(this ClaimsPrincipal user, Guid agencyId, string action, string entityType, Guid entityId)
        => new() { AgencyId = agencyId, ActorUserId = user.GetUserId(), Action = action, EntityType = entityType, EntityId = entityId };
    private static string? Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static object SettlementView(Settlement row) => new
    {
        row.Id, row.StreamerId, row.PeriodStart, row.PeriodEnd, row.Currency, row.GrossRevenue,
        row.Deductions, row.StreamerShare, row.AgencyShare, row.Status, row.CreatedBy,
        row.ApprovedBy, row.ApprovedAt, row.LockedAt, row.RuleSnapshot
    };

    private static string FormatTimeAgo(DateTimeOffset time)
    {
        var diff = DateTimeOffset.UtcNow - time;
        if (diff.TotalMinutes < 1) return "Vừa xong";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
        if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
        return time.ToString("dd/MM/yyyy");
    }
}

public sealed record TeamDto(Guid Id, string Code, string Name, Guid? ParentTeamId);
public sealed record CreateTeamRequest(string Code, string Name, Guid? ParentTeamId);
public sealed record AddTeamMemberRequest(Guid UserId, string MembershipRole, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record EmployeeDto(Guid Id, string EmployeeCode, string JobTitle, DateOnly HireDate, DateOnly? EndDate, string Status);
public sealed record CreateEmployeeRequest(Guid UserId, string EmployeeCode, string JobTitle, DateOnly HireDate);
public sealed record StreamerDto(Guid Id, string Code, string StageName, string Status, string TimeZone);
public sealed record CreateStreamerRequest(string Code, string StageName, string TimeZone = "Asia/Ho_Chi_Minh");
public sealed record CreateContractRequest(string ContractReference, DateOnly ValidFrom, DateOnly? ValidTo, DateTimeOffset SignedAt, decimal StreamerRate, decimal AgencyRate);
public sealed record AssignmentRequest(Guid? UserId, Guid? TeamId, string RoleCode, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record ScheduleDto(Guid Id, Guid StreamerId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt, string Status);
public sealed record CreateScheduleRequest(Guid StreamerId, Guid? ChannelId, string Title, DateTimeOffset PlannedStartAt, DateTimeOffset PlannedEndAt);
public sealed record CreateSessionRequest(Guid StreamerId, Guid? ScheduleId, Guid? ChannelId, DateTimeOffset? ActualStartAt, DateTimeOffset? ActualEndAt, string Status);
public sealed record AddMetricRequest(string MetricCode, decimal Value, string Unit);
public sealed record CustomerDto(Guid Id, string Name, string? ContactName, string? Email);
public sealed record CreateCustomerRequest(string Name, string? ContactName, string? Email);
public sealed record BookingDto(Guid Id, string Title, Guid CustomerId, string Status, decimal GrossAmount, string Currency);
public sealed record CreateBookingRequest(Guid CustomerId, string Title, decimal GrossAmount, string Currency, DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Guid[]? StreamerIds);
public sealed record CreateDeliverableRequest(string Description, DateTimeOffset? DueAt);
public sealed record TransactionDto(Guid Id, string Type, string Category, decimal Amount, string Currency, DateTimeOffset OccurredAt, string Status, Guid? StreamerId);
public sealed record CreateTransactionRequest(string Type, string Category, decimal Amount, string Currency, DateTimeOffset OccurredAt, Guid? StreamerId, Guid? BookingId, string? Reference);
public sealed record CreateShareRuleRequest(Guid StreamerId, string RevenueType, decimal StreamerRate, decimal AgencyRate, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record CreateSettlementRequest(Guid StreamerId, DateOnly PeriodStart, DateOnly PeriodEnd);
public sealed record MarkPayoutPaidRequest(string PaymentReference);
public sealed record TaskDto(Guid Id, string Title, Guid TeamId, Guid OwnerUserId, Guid? StreamerId, Guid? BookingId, string Priority, string Status, DateTimeOffset? DueAt);
public sealed record CreateTaskRequest(Guid TeamId, Guid OwnerUserId, Guid? StreamerId, Guid? BookingId, string Title, string Priority, DateTimeOffset? DueAt);
public sealed record UpdateTaskStatusRequest(string Status);
public sealed record IntakeStatusRequest(string Status);
