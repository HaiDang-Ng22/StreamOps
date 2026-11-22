using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StreamOps.Api.Domain;
using StreamOps.Api.Identity;
using System.Text.Json;

namespace StreamOps.Api.Data;

public static class DataSeeder
{
    private static readonly string[] Roles =
    [
        "Admin", "HRManager", "HRStaff", "TalentManager", "OperationsManager",
        "Booking", "Marketing", "Finance", "FinanceApprover", "Analyst", "Streamer", "Auditor"
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StreamOpsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<StreamOpsUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        // 1. Roles
        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });
            }
        }

        // 2. Agency
        var agency = await db.Agencies.FirstOrDefaultAsync();
        if (agency is null)
        {
            agency = new Agency
            {
                Id = Guid.NewGuid(),
                Name = "Nova Creator House",
                Slug = "nova-creator-house",
                TimeZone = "Asia/Ho_Chi_Minh",
                DefaultCurrency = "VND"
            };
            db.Agencies.Add(agency);
            await db.SaveChangesAsync();
        }
        else
        {
            agency.Name = "Nova Creator House";
            agency.Slug = "nova-creator-house";
            await db.SaveChangesAsync();
        }

        var agencyId = agency.Id;

        // 3. Key Staff Users
        var defaultPassword = "StreamOps@2026!";
        var staffAccounts = new (string Email, string Name, string Role)[]
        {
            ("admin@streamops.local", "Agency Administrator", "Admin"),
            ("hr.manager@streamops.local", "Trần Thị Mai", "HRManager"),
            ("hr@streamops.local", "Trần Thị Mai", "HRManager"),
            ("talent.manager@streamops.local", "Nguyễn Hoàng Nam", "TalentManager"),
            ("talent@streamops.local", "Nguyễn Hoàng Nam", "TalentManager"),
            ("ops.manager@streamops.local", "Lê Minh Quân", "OperationsManager"),
            ("operations@streamops.local", "Lê Minh Quân", "OperationsManager"),
            ("booking.lead@streamops.local", "Phạm Thu Hà", "Booking"),
            ("booking@streamops.local", "Phạm Thu Hà", "Booking"),
            ("finance.lead@streamops.local", "Đỗ Đức Huy", "Finance"),
            ("finance@streamops.local", "Đỗ Đức Huy", "Finance"),
            ("finance.approver@streamops.local", "Vũ Hoàng Yến", "FinanceApprover"),
            ("approver@streamops.local", "Vũ Hoàng Yến", "FinanceApprover"),
            ("analyst@streamops.local", "Đinh Văn Thành", "Analyst")
        };

        var usersByEmail = new Dictionary<string, StreamOpsUser>(StringComparer.OrdinalIgnoreCase);

        foreach (var (email, name, role) in staffAccounts)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new StreamOpsUser
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    Email = email,
                    UserName = email,
                    DisplayName = name,
                    EmailConfirmed = true,
                    IsActive = true
                };
                var createResult = await userManager.CreateAsync(user, defaultPassword);
                if (!createResult.Succeeded)
                {
                    // Fallback create directly if password error
                    throw new InvalidOperationException($"Could not create user {email}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                user.DisplayName = name;
                user.IsActive = true;
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);

                // Ensure password can be reset if needed
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                await userManager.ResetPasswordAsync(user, token, defaultPassword);
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }

            usersByEmail[email] = user;
        }

        var adminUser = usersByEmail["admin@streamops.local"];
        var hrUser = usersByEmail["hr.manager@streamops.local"];
        var talentUser = usersByEmail["talent.manager@streamops.local"];
        var opsUser = usersByEmail["ops.manager@streamops.local"];
        var bookingUser = usersByEmail["booking.lead@streamops.local"];
        var financeUser = usersByEmail["finance.lead@streamops.local"];
        var approverUser = usersByEmail["finance.approver@streamops.local"];
        var analystUser = usersByEmail["analyst@streamops.local"];

        // 4. Teams
        var teamsData = new (string Code, string Name)[]
        {
            ("TALENT", "Talent Management"),
            ("LIVE_OPS", "Live Operations"),
            ("BOOKING", "Booking & Partnerships"),
            ("FINANCE", "Finance & Accounting"),
            ("HR", "Human Resources"),
            ("CONTENT", "Content & Creative"),
            ("ANALYTICS", "Data & Analytics")
        };

        var teamsByCode = new Dictionary<string, Team>();
        foreach (var (code, name) in teamsData)
        {
            var team = await db.Teams.FirstOrDefaultAsync(t => t.AgencyId == agencyId && t.Code == code);
            if (team is null)
            {
                team = new Team
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    Code = code,
                    Name = name,
                    Status = "active"
                };
                db.Teams.Add(team);
            }
            teamsByCode[code] = team;
        }
        await db.SaveChangesAsync();

        // 5. Employee Profiles & Memberships
        var staffProfiles = new (StreamOpsUser User, string Code, string Title, string TeamCode, string MembershipRole)[]
        {
            (adminUser, "EMP-001", "Giám đốc điều hành", "TALENT", "lead"),
            (hrUser, "EMP-002", "Trưởng phòng Nhân sự", "HR", "lead"),
            (talentUser, "EMP-003", "Trưởng phòng Quản lý Creator", "TALENT", "lead"),
            (opsUser, "EMP-004", "Trưởng nhóm Vận hành Livestream", "LIVE_OPS", "lead"),
            (bookingUser, "EMP-005", "Trưởng nhóm Đối tác & Thương mại", "BOOKING", "lead"),
            (financeUser, "EMP-006", "Kế toán trưởng", "FINANCE", "member"),
            (approverUser, "EMP-007", "Giám đốc Tài chính", "FINANCE", "lead"),
            (analystUser, "EMP-008", "Chuyên viên Phân tích Dữ liệu", "ANALYTICS", "lead")
        };

        foreach (var (user, code, title, teamCode, mRole) in staffProfiles)
        {
            if (!await db.EmployeeProfiles.AnyAsync(e => e.AgencyId == agencyId && e.UserId == user.Id))
            {
                db.EmployeeProfiles.Add(new EmployeeProfile
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    UserId = user.Id,
                    EmployeeCode = code,
                    JobTitle = title,
                    HireDate = new DateOnly(2025, 1, 15),
                    EmploymentStatus = "active"
                });
            }

            var team = teamsByCode[teamCode];
            if (!await db.TeamMemberships.AnyAsync(m => m.AgencyId == agencyId && m.TeamId == team.Id && m.UserId == user.Id))
            {
                db.TeamMemberships.Add(new TeamMembership
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    TeamId = team.Id,
                    UserId = user.Id,
                    MembershipRole = mRole,
                    ValidFrom = new DateOnly(2025, 1, 15)
                });
            }
        }
        await db.SaveChangesAsync();

        // 6. Streamers
        var streamersData = new (string Code, string StageName, string LegalName, string Status)[]
        {
            ("STM01", "Mèo Mốc", "Nguyễn Thảo My", "active"),
            ("STM02", "Kaito Plays", "Phạm Quốc Huy", "active"),
            ("STM03", "An Nhiên", "Lê Ngọc An", "active"),
            ("STM04", "Bắp Rang TV", "Trần Tuấn Kiệt", "active"),
            ("STM05", "Linh Ruby", "Đoàn Mỹ Linh", "onboarding"),
            ("STM06", "Quang Gaming", "Vũ Đình Quang", "lead")
        };

        var streamersByCode = new Dictionary<string, Streamer>();
        foreach (var (code, stageName, legalName, status) in streamersData)
        {
            var streamer = await db.Streamers.FirstOrDefaultAsync(s => s.AgencyId == agencyId && s.Code == code);
            if (streamer is null)
            {
                streamer = new Streamer
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    Code = code,
                    StageName = stageName,
                    LegalName = legalName,
                    Status = status,
                    TimeZone = "Asia/Ho_Chi_Minh"
                };
                db.Streamers.Add(streamer);
            }
            else
            {
                streamer.StageName = stageName;
                streamer.LegalName = legalName;
                streamer.Status = status;
            }
            streamersByCode[code] = streamer;
        }
        await db.SaveChangesAsync();

        // 7. Channels
        var channelsData = new (string StreamerCode, string Platform, string ExtId, string Handle, string Url)[]
        {
            ("STM01", "tiktok", "meomoc.live", "@meomoc.live", "https://www.tiktok.com/@meomoc.live"),
            ("STM01", "youtube", "UCmeomoc_gaming", "@meomocchannel", "https://www.youtube.com/@meomocchannel"),
            ("STM02", "youtube", "UCkaitoplays_vn", "@kaitoplaysvn", "https://www.youtube.com/@kaitoplaysvn"),
            ("STM02", "twitch", "kaitoplays", "kaitoplays", "https://www.twitch.tv/kaitoplays"),
            ("STM03", "tiktok", "annhien.official", "@annhien.official", "https://www.tiktok.com/@annhien.official"),
            ("STM04", "youtube", "UCbaprangtv", "@baprangtv", "https://www.youtube.com/@baprangtv")
        };

        var channelsByExtId = new Dictionary<string, Channel>();
        foreach (var (sCode, platform, extId, handle, url) in channelsData)
        {
            var streamer = streamersByCode[sCode];
            var channel = await db.Channels.FirstOrDefaultAsync(c => c.AgencyId == agencyId && c.PlatformCode == platform && c.ExternalChannelId == extId);
            if (channel is null)
            {
                channel = new Channel
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    PlatformCode = platform,
                    ExternalChannelId = extId,
                    Handle = handle,
                    Url = url,
                    Status = "active"
                };
                db.Channels.Add(channel);
            }
            channelsByExtId[extId] = channel;
        }
        await db.SaveChangesAsync();

        // 8. Contracts & Revenue Share Rules (for active streamers)
        var contractsData = new (string SCode, string Ref, decimal SRate, decimal ARate)[]
        {
            ("STM01", "HD-2026-STM01", 0.7000m, 0.3000m),
            ("STM02", "HD-2026-STM02", 0.7000m, 0.3000m),
            ("STM03", "HD-2026-STM03", 0.6500m, 0.3500m),
            ("STM04", "HD-2026-STM04", 0.7500m, 0.2500m)
        };

        foreach (var (sCode, cRef, sRate, aRate) in contractsData)
        {
            var streamer = streamersByCode[sCode];
            if (!await db.Contracts.AnyAsync(c => c.AgencyId == agencyId && c.ContractReference == cRef))
            {
                db.Contracts.Add(new Contract
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    ContractReference = cRef,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidTo = new DateOnly(2027, 1, 1),
                    SignedAt = DateTimeOffset.UtcNow.AddMonths(-3),
                    StreamerRate = sRate,
                    AgencyRate = aRate,
                    Status = "active",
                    CreatedBy = talentUser.Id
                });
            }

            if (!await db.RevenueShareRules.AnyAsync(r => r.AgencyId == agencyId && r.StreamerId == streamer.Id && r.Status == "active"))
            {
                db.RevenueShareRules.Add(new RevenueShareRule
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    RevenueType = "all",
                    StreamerRate = sRate,
                    AgencyRate = aRate,
                    ValidFrom = new DateOnly(2026, 1, 1),
                    ValidTo = new DateOnly(2027, 1, 1),
                    CreatedBy = talentUser.Id,
                    ApprovedBy = approverUser.Id,
                    Status = "active"
                });
            }

            if (!await db.StreamerAssignments.AnyAsync(a => a.AgencyId == agencyId && a.StreamerId == streamer.Id && a.UserId == talentUser.Id))
            {
                db.StreamerAssignments.Add(new StreamerAssignment
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    UserId = talentUser.Id,
                    TeamId = teamsByCode["TALENT"].Id,
                    AssignmentRole = "primary_manager",
                    ValidFrom = new DateOnly(2026, 1, 1)
                });
            }
        }
        await db.SaveChangesAsync();

        // 9. Live Schedules & Sessions & Metrics
        var today = DateTime.UtcNow.Date;
        var todaySchedules = new (string SCode, string ChannelExtId, string Title, TimeSpan StartTime, TimeSpan EndTime)[]
        {
            ("STM01", "meomoc.live", "Buổi sáng cùng cộng đồng & Q&A", new TimeSpan(9, 30, 0), new TimeSpan(12, 0, 0)),
            ("STM02", "UCkaitoplays_vn", "Rank cùng hội viên & Trải nghiệm meta mới", new TimeSpan(13, 0, 0), new TimeSpan(16, 30, 0)),
            ("STM03", "annhien.official", "Góc trò chuyện cuối tuần & Acoustic Live", new TimeSpan(16, 30, 0), new TimeSpan(19, 0, 0)),
            ("STM04", "UCbaprangtv", "Ra mắt series mới & Setup góc làm việc", new TimeSpan(20, 0, 0), new TimeSpan(22, 30, 0))
        };

        foreach (var (sCode, chExt, title, startT, endT) in todaySchedules)
        {
            var streamer = streamersByCode[sCode];
            var channel = channelsByExtId[chExt];
            var pStart = new DateTimeOffset(today.Add(startT), TimeSpan.Zero);
            var pEnd = new DateTimeOffset(today.Add(endT), TimeSpan.Zero);

            if (!await db.LiveSchedules.AnyAsync(s => s.AgencyId == agencyId && s.StreamerId == streamer.Id && s.PlannedStartAt == pStart))
            {
                db.LiveSchedules.Add(new LiveSchedule
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    ChannelId = channel.Id,
                    Title = title,
                    PlannedStartAt = pStart,
                    PlannedEndAt = pEnd,
                    Status = "confirmed",
                    CreatedBy = opsUser.Id
                });
            }
        }

        // Past Completed Sessions with real metrics
        var pastSessions = new (string SCode, string ChannelExtId, string Title, int DaysAgo, decimal Views, decimal PeakCCU, decimal Gifts, decimal Comments)[]
        {
            ("STM01", "meomoc.live", "Mega Live TikTok - Giao lưu cùng fan", 2, 128400, 14500, 28500000, 8420),
            ("STM02", "UCkaitoplays_vn", "Chung kết leo rank mùa 14 - Kaito Plays", 3, 96200, 9800, 18200000, 6120),
            ("STM03", "annhien.official", "Acoustic Thứ Sáu cùng An Nhiên", 1, 82700, 8200, 15600000, 5310),
            ("STM04", "UCbaprangtv", "Review thiết bị livestream chuyên nghiệp 2026", 4, 110500, 12100, 35000000, 7890)
        };

        foreach (var (sCode, chExt, sTitle, daysAgo, views, ccu, gifts, comments) in pastSessions)
        {
            var streamer = streamersByCode[sCode];
            var channel = channelsByExtId[chExt];
            var sDate = today.AddDays(-daysAgo);
            var actualStart = new DateTimeOffset(sDate.AddHours(19), TimeSpan.Zero);
            var actualEnd = new DateTimeOffset(sDate.AddHours(22), TimeSpan.Zero);

            var existingSession = await db.LiveSessions.FirstOrDefaultAsync(ls => ls.AgencyId == agencyId && ls.StreamerId == streamer.Id && ls.ActualStartAt == actualStart);
            if (existingSession is null)
            {
                var sched = new LiveSchedule
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    ChannelId = channel.Id,
                    Title = sTitle,
                    PlannedStartAt = actualStart,
                    PlannedEndAt = actualEnd,
                    Status = "completed",
                    CreatedBy = opsUser.Id
                };
                db.LiveSchedules.Add(sched);

                var session = new LiveSession
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    StreamerId = streamer.Id,
                    ScheduleId = sched.Id,
                    ChannelId = channel.Id,
                    ActualStartAt = actualStart,
                    ActualEndAt = actualEnd,
                    Status = "completed",
                    Source = "manual",
                    RecordedBy = opsUser.Id
                };
                db.LiveSessions.Add(session);

                db.SessionMetrics.AddRange(
                    new SessionMetric { Id = Guid.NewGuid(), AgencyId = agencyId, LiveSessionId = session.Id, MetricCode = "views", Value = views, Unit = "count", CapturedAt = actualEnd, EnteredBy = opsUser.Id },
                    new SessionMetric { Id = Guid.NewGuid(), AgencyId = agencyId, LiveSessionId = session.Id, MetricCode = "ccu_peak", Value = ccu, Unit = "users", CapturedAt = actualEnd, EnteredBy = opsUser.Id },
                    new SessionMetric { Id = Guid.NewGuid(), AgencyId = agencyId, LiveSessionId = session.Id, MetricCode = "donations", Value = gifts, Unit = "VND", CapturedAt = actualEnd, EnteredBy = opsUser.Id },
                    new SessionMetric { Id = Guid.NewGuid(), AgencyId = agencyId, LiveSessionId = session.Id, MetricCode = "comments", Value = comments, Unit = "count", CapturedAt = actualEnd, EnteredBy = opsUser.Id }
                );
            }
        }
        await db.SaveChangesAsync();

        // 10. Customers & Bookings & Deliverables
        var customersData = new (string Name, string Contact, string Email)[]
        {
            ("Shopee Vietnam", "Hoàng Hải Yến", "partnership@shopee.vn"),
            ("Samsung Vina Electronics", "Trần Gia Bảo", "marketing@samsung.com.vn"),
            ("VNGGames Corporation", "Nguyễn Thùy Chi", "brand@vng.com.vn")
        };

        var customersByName = new Dictionary<string, Customer>();
        foreach (var (cName, contact, cEmail) in customersData)
        {
            var cust = await db.Customers.FirstOrDefaultAsync(c => c.AgencyId == agencyId && c.Name == cName);
            if (cust is null)
            {
                cust = new Customer
                {
                    Id = Guid.NewGuid(),
                    AgencyId = agencyId,
                    Name = cName,
                    ContactName = contact,
                    Email = cEmail,
                    Status = "active"
                };
                db.Customers.Add(cust);
            }
            customersByName[cName] = cust;
        }
        await db.SaveChangesAsync();

        // Booking 1: Shopee
        var shopee = customersByName["Shopee Vietnam"];
        var b1 = await db.Bookings.FirstOrDefaultAsync(b => b.AgencyId == agencyId && b.Title.Contains("Shopee 11.11"));
        if (b1 is null)
        {
            b1 = new Booking
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                CustomerId = shopee.Id,
                OwnerUserId = bookingUser.Id,
                Title = "Chiến dịch Siêu Sale Ngày Đôi Shopee 11.11",
                Status = "in_progress",
                GrossAmount = 120_000_000m,
                Currency = "VND",
                StartsAt = DateTimeOffset.UtcNow.AddDays(-5),
                EndsAt = DateTimeOffset.UtcNow.AddDays(5)
            };
            db.Bookings.Add(b1);
        }

        // Booking 2: Samsung
        var samsung = customersByName["Samsung Vina Electronics"];
        var b2 = await db.Bookings.FirstOrDefaultAsync(b => b.AgencyId == agencyId && b.Title.Contains("Galaxy"));
        if (b2 is null)
        {
            b2 = new Booking
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                CustomerId = samsung.Id,
                OwnerUserId = bookingUser.Id,
                Title = "Quảng bá công nghệ Galaxy AI cùng Bắp Rang TV",
                Status = "confirmed",
                GrossAmount = 85_000_000m,
                Currency = "VND",
                StartsAt = DateTimeOffset.UtcNow.AddDays(1),
                EndsAt = DateTimeOffset.UtcNow.AddDays(14)
            };
            db.Bookings.Add(b2);
        }

        // Booking 3: VNGGames
        var vng = customersByName["VNGGames Corporation"];
        var b3 = await db.Bookings.FirstOrDefaultAsync(b => b.AgencyId == agencyId && b.Title.Contains("PUBG Mobile"));
        if (b3 is null)
        {
            b3 = new Booking
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                CustomerId = vng.Id,
                OwnerUserId = bookingUser.Id,
                Title = "Giải đấu giao hữu PUBG Mobile All-Star Creator",
                Status = "delivered",
                GrossAmount = 60_000_000m,
                Currency = "VND",
                StartsAt = DateTimeOffset.UtcNow.AddDays(-10),
                EndsAt = DateTimeOffset.UtcNow.AddDays(-2)
            };
            db.Bookings.Add(b3);
        }
        await db.SaveChangesAsync();

        // Add BookingStreamers and Deliverables now that Bookings have persisted
        if (!await db.BookingStreamers.AnyAsync(bs => bs.AgencyId == agencyId && bs.BookingId == b1.Id))
        {
            db.BookingStreamers.AddRange(
                new BookingStreamer { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b1.Id, StreamerId = streamersByCode["STM01"].Id, FeeAmount = 70_000_000m, Currency = "VND", Status = "confirmed" },
                new BookingStreamer { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b1.Id, StreamerId = streamersByCode["STM03"].Id, FeeAmount = 50_000_000m, Currency = "VND", Status = "confirmed" }
            );
        }
        if (!await db.BookingDeliverables.AnyAsync(bd => bd.AgencyId == agencyId && bd.BookingId == b1.Id))
        {
            db.BookingDeliverables.AddRange(
                new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b1.Id, Description = "02 video ngắn quảng bá Flash Sale Shopee 11.11 trên TikTok", DueAt = DateTimeOffset.UtcNow.AddDays(-1), Status = "accepted", AcceptedBy = bookingUser.Id, AcceptedAt = DateTimeOffset.UtcNow.AddHours(-12) },
                new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b1.Id, Description = "01 phiên livestream chốt đơn 90 phút ngày 11.11", DueAt = DateTimeOffset.UtcNow.AddDays(2), Status = "pending" }
            );
        }

        if (!await db.BookingStreamers.AnyAsync(bs => bs.AgencyId == agencyId && bs.BookingId == b2.Id))
        {
            db.BookingStreamers.Add(
                new BookingStreamer { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b2.Id, StreamerId = streamersByCode["STM04"].Id, FeeAmount = 85_000_000m, Currency = "VND", Status = "confirmed" }
            );
        }
        if (!await db.BookingDeliverables.AnyAsync(bd => bd.AgencyId == agencyId && bd.BookingId == b2.Id))
        {
            db.BookingDeliverables.Add(
                new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b2.Id, Description = "01 video unboxing và đánh giá chi tiết tính năng AI", DueAt = DateTimeOffset.UtcNow.AddDays(7), Status = "pending" }
            );
        }

        if (!await db.BookingStreamers.AnyAsync(bs => bs.AgencyId == agencyId && bs.BookingId == b3.Id))
        {
            db.BookingStreamers.Add(
                new BookingStreamer { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b3.Id, StreamerId = streamersByCode["STM02"].Id, FeeAmount = 60_000_000m, Currency = "VND", Status = "confirmed" }
            );
        }
        if (!await db.BookingDeliverables.AnyAsync(bd => bd.AgencyId == agencyId && bd.BookingId == b3.Id))
        {
            db.BookingDeliverables.AddRange(
                new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b3.Id, Description = "Caster chính 02 trận chung kết bảng", DueAt = DateTimeOffset.UtcNow.AddDays(-3), Status = "accepted", AcceptedBy = bookingUser.Id, AcceptedAt = DateTimeOffset.UtcNow.AddDays(-3) },
                new BookingDeliverable { Id = Guid.NewGuid(), AgencyId = agencyId, BookingId = b3.Id, Description = "01 livestream luyện tập cùng fan trước giải", DueAt = DateTimeOffset.UtcNow.AddDays(-4), Status = "accepted", AcceptedBy = bookingUser.Id, AcceptedAt = DateTimeOffset.UtcNow.AddDays(-4) }
            );
        }
        await db.SaveChangesAsync();

        // 11. Financial Transactions
        if (!await db.FinancialTransactions.AnyAsync(f => f.AgencyId == agencyId))
        {
            var txs = new List<FinancialTransaction>
            {
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM01"].Id, Type = "revenue", Category = "donation", Amount = 28_500_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-2), Status = "posted", Source = "tiktok_payout", Reference = "TK-PAY-20261120-001", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM02"].Id, Type = "revenue", Category = "donation", Amount = 18_200_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-3), Status = "posted", Source = "youtube_superchat", Reference = "YT-SC-20261119-042", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM03"].Id, Type = "revenue", Category = "donation", Amount = 15_600_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-1), Status = "posted", Source = "tiktok_payout", Reference = "TK-PAY-20261121-008", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM01"].Id, BookingId = b1?.Id, Type = "revenue", Category = "booking", Amount = 60_000_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-4), Status = "posted", Source = "contract", Reference = "SHP-INV-1111-01", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM02"].Id, BookingId = b3?.Id, Type = "revenue", Category = "booking", Amount = 60_000_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-5), Status = "posted", Source = "contract", Reference = "VNG-INV-PUBG-09", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM01"].Id, Type = "expense", Category = "production", Amount = 3_500_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-2), Status = "posted", Source = "studio_rental", Reference = "STD-INV-7734", RecordedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, StreamerId = streamersByCode["STM02"].Id, Type = "expense", Category = "equipment", Amount = 2_000_000m, Currency = "VND", OccurredAt = DateTimeOffset.UtcNow.AddDays(-3), Status = "posted", Source = "gear_setup", Reference = "GEAR-INV-8891", RecordedBy = financeUser.Id }
            };
            db.FinancialTransactions.AddRange(txs);
            await db.SaveChangesAsync();
        }

        // 12. Settlements & Payouts (Historical and Current)
        if (!await db.Settlements.AnyAsync(s => s.AgencyId == agencyId))
        {
            var m1 = streamersByCode["STM01"];
            var s1 = new Settlement
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                StreamerId = m1.Id,
                PeriodStart = new DateOnly(2026, 10, 1),
                PeriodEnd = new DateOnly(2026, 10, 31),
                Currency = "VND",
                GrossRevenue = 85_000_000m,
                Deductions = 5_000_000m,
                StreamerShare = 56_000_000m,
                AgencyShare = 24_000_000m,
                Status = "locked",
                CreatedBy = financeUser.Id,
                ApprovedBy = approverUser.Id,
                ApprovedAt = DateTimeOffset.UtcNow.AddDays(-20),
                LockedAt = DateTimeOffset.UtcNow.AddDays(-20),
                RuleSnapshot = JsonSerializer.Serialize(new { streamerRate = 0.7000, agencyRate = 0.3000, revenueType = "all" })
            };
            db.Settlements.Add(s1);

            var kaito = streamersByCode["STM02"];
            var s2 = new Settlement
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                StreamerId = kaito.Id,
                PeriodStart = new DateOnly(2026, 11, 1),
                PeriodEnd = new DateOnly(2026, 11, 30),
                Currency = "VND",
                GrossRevenue = 78_200_000m,
                Deductions = 2_000_000m,
                StreamerShare = 53_340_000m,
                AgencyShare = 22_860_000m,
                Status = "submitted",
                CreatedBy = financeUser.Id,
                RuleSnapshot = JsonSerializer.Serialize(new { streamerRate = 0.7000, agencyRate = 0.3000, revenueType = "all" })
            };
            db.Settlements.Add(s2);
            await db.SaveChangesAsync();

            var p1 = new Payout
            {
                Id = Guid.NewGuid(),
                AgencyId = agencyId,
                SettlementId = s1.Id,
                Amount = 56_000_000m,
                Currency = "VND",
                Status = "paid",
                PaymentReference = "VCB-TRANS-20261105-098231",
                PaidAt = DateTimeOffset.UtcNow.AddDays(-17),
                RecordedBy = financeUser.Id
            };
            db.Payouts.Add(p1);
            await db.SaveChangesAsync();
        }

        // 13. Tasks
        if (!await db.Tasks.AnyAsync(t => t.AgencyId == agencyId))
        {
            var taskList = new TaskItem[]
            {
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, TeamId = teamsByCode["FINANCE"].Id, OwnerUserId = approverUser.Id, StreamerId = streamersByCode["STM02"].Id, Title = "Duyệt bảng đối soát tháng 11 cho Kaito Plays", Priority = "urgent", Status = "open", DueAt = DateTimeOffset.UtcNow.AddHours(8), CreatedBy = financeUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, TeamId = teamsByCode["BOOKING"].Id, OwnerUserId = bookingUser.Id, BookingId = b1?.Id, Title = "Nghiệm thu deliverable livestream Shopee 11.11", Priority = "high", Status = "open", DueAt = DateTimeOffset.UtcNow.AddDays(1), CreatedBy = bookingUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, TeamId = teamsByCode["LIVE_OPS"].Id, OwnerUserId = opsUser.Id, StreamerId = streamersByCode["STM04"].Id, Title = "Chuẩn bị kịch bản & thiết bị cho phiên live Samsung", Priority = "normal", Status = "in_progress", DueAt = DateTimeOffset.UtcNow.AddDays(2), CreatedBy = opsUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, TeamId = teamsByCode["TALENT"].Id, OwnerUserId = talentUser.Id, StreamerId = streamersByCode["STM04"].Id, Title = "Kiểm tra hợp đồng tài trợ mới của Bắp Rang TV", Priority = "high", Status = "open", DueAt = DateTimeOffset.UtcNow.AddDays(3), CreatedBy = talentUser.Id },
                new() { Id = Guid.NewGuid(), AgencyId = agencyId, TeamId = teamsByCode["TALENT"].Id, OwnerUserId = talentUser.Id, StreamerId = streamersByCode["STM05"].Id, Title = "Hoàn tất thủ tục tiếp nhận hồ sơ cho Linh Ruby", Priority = "normal", Status = "in_progress", DueAt = DateTimeOffset.UtcNow.AddDays(4), CreatedBy = talentUser.Id }
            };
            db.Tasks.AddRange(taskList);
            await db.SaveChangesAsync();
        }

        // 14. Public Applications & Partnership Requests
        if (!await db.StreamerApplications.AnyAsync())
        {
            db.StreamerApplications.AddRange(
                new StreamerApplication { Id = Guid.NewGuid(), ApplicantName = "Nguyễn Thanh Phong", Email = "thanhphong.gaming@gmail.com", PlatformLinks = "tiktok.com/@thanhphong_fps (150K followers)", Note = "Kinh nghiệm livestream Valorant/PUBG 2 năm, muốn hợp tác chuyên nghiệp", ConsentAt = DateTimeOffset.UtcNow.AddDays(-2), Status = "reviewing", AssignedTo = talentUser.Id, ReceivedAt = DateTimeOffset.UtcNow.AddDays(-2) },
                new StreamerApplication { Id = Guid.NewGuid(), ApplicantName = "Đỗ Khánh Linh", Email = "khanhlinh.beauty@gmail.com", PlatformLinks = "tiktok.com/@linhbeauty (85K followers)", Note = "Sáng tạo nội dung makeup & skincare, có định hướng livestream bán hàng", ConsentAt = DateTimeOffset.UtcNow.AddDays(-1), Status = "received", ReceivedAt = DateTimeOffset.UtcNow.AddDays(-1) }
            );
        }

        if (!await db.PartnershipRequests.AnyAsync())
        {
            db.PartnershipRequests.AddRange(
                new PartnershipRequest { Id = Guid.NewGuid(), OrganizationName = "Highlands Coffee", ContactName = "Lê Thùy Dung", Email = "dung.le@highlands.com.vn", Summary = "Cần booking 3 streamer Gen Z quảng bá dòng trà Freeze mới trong tháng 12", ConsentAt = DateTimeOffset.UtcNow.AddDays(-3), Status = "reviewing", AssignedTo = bookingUser.Id, ReceivedAt = DateTimeOffset.UtcNow.AddDays(-3) },
                new PartnershipRequest { Id = Guid.NewGuid(), OrganizationName = "Logitech Vietnam", ContactName = "Trần Quốc Anh", Email = "qanh@logitech.com", Summary = "Tài trợ bộ thiết bị gaming gear và mic cho streamer của agency", ConsentAt = DateTimeOffset.UtcNow.AddDays(-1), Status = "received", ReceivedAt = DateTimeOffset.UtcNow.AddDays(-1) }
            );
        }
        await db.SaveChangesAsync();

        // 15. Audit Logs
        if (!await db.AuditLogs.AnyAsync(a => a.AgencyId == agencyId && a.Action != "system.initial_admin_created"))
        {
            db.AuditLogs.AddRange(
                new AuditLog { AgencyId = agencyId, ActorUserId = adminUser.Id, Action = "agency.initialized", EntityType = "agency", EntityId = agencyId, Summary = "Cấu hình không gian agency Nova Creator House", OccurredAt = DateTimeOffset.UtcNow.AddDays(-10) },
                new AuditLog { AgencyId = agencyId, ActorUserId = talentUser.Id, Action = "contract.signed", EntityType = "contract", EntityId = streamersByCode["STM01"].Id, Summary = "Ký hợp đồng HD-2026-STM01 cùng Mèo Mốc tỷ lệ 70/30", OccurredAt = DateTimeOffset.UtcNow.AddDays(-8) },
                new AuditLog { AgencyId = agencyId, ActorUserId = bookingUser.Id, Action = "booking.deliverable.accepted", EntityType = "booking_deliverable", EntityId = b1?.Id, Summary = "Nghiệm thu video Flash Sale Shopee 11.11", OccurredAt = DateTimeOffset.UtcNow.AddHours(-12) },
                new AuditLog { AgencyId = agencyId, ActorUserId = approverUser.Id, Action = "settlement.approved", EntityType = "settlement", EntityId = streamersByCode["STM01"].Id, Summary = "Duyệt đối soát tháng 10 cho Mèo Mốc (56.000.000 VND)", OccurredAt = DateTimeOffset.UtcNow.AddDays(-17) },
                new AuditLog { AgencyId = agencyId, ActorUserId = opsUser.Id, Action = "session.completed", EntityType = "live_session", EntityId = streamersByCode["STM02"].Id, Summary = "Ghi nhận phiên live Kaito Plays đạt 96.2K views", OccurredAt = DateTimeOffset.UtcNow.AddDays(-3) }
            );
            await db.SaveChangesAsync();
        }

        Console.WriteLine("StreamOps Real Business Data seeded successfully.");
    }
}
