using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using StreamOps.Api.Domain;
using StreamOps.Api.Data;
using StreamOps.Api.Identity;
using StreamOps.Api.Api;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var databaseUrl = builder.Configuration["DATABASE_URL"];
var databaseConnectionString = !string.IsNullOrWhiteSpace(databaseUrl)
    ? ConvertDatabaseUrl(databaseUrl)
    : builder.Configuration.GetConnectionString("StreamOps");
if (string.IsNullOrWhiteSpace(databaseConnectionString))
    throw new InvalidOperationException("Configure DATABASE_URL or ConnectionStrings:StreamOps using a secret store.");
builder.Services.AddDbContext<StreamOpsDbContext>(options => options.UseNpgsql(databaseConnectionString));

builder.Services
    .AddIdentityCore<StreamOpsUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<StreamOpsDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtSigningKey = builder.Configuration["JWT_SIGNING_KEY"];
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
    throw new InvalidOperationException("JWT_SIGNING_KEY must be configured with at least 32 bytes via a secret store.");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "streamops-api";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "streamops-web";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.AddPolicy("public-intake", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0, AutoReplenishment = true }));
});

builder.Services.AddCors(options => options.AddPolicy("web", policy =>
    policy.SetIsOriginAllowed(origin =>
    {
        if (string.IsNullOrWhiteSpace(origin)) return false;
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return uri.Host is "localhost" or "127.0.0.1";
        }
        return false;
    })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions());
app.MapGet("/api/v1", () => Results.Ok(new
{
    name = "StreamOps API",
    version = "v1",
    status = "running"
}));

var auth = app.MapGroup("/api/v1/auth");
auth.MapPost("/login", async (LoginRequest request, SignInManager<StreamOpsUser> signIn,
    UserManager<StreamOpsUser> users, IConfiguration configuration, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
        string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email hoặc mật khẩu không hợp lệ.");
    var normalizedEmail = request.Email.Trim();
    var user = await users.FindByEmailAsync(normalizedEmail);
    if (user is null || !user.IsActive || !user.EmailConfirmed)
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email hoặc mật khẩu không hợp lệ.");

    var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
    if (!result.Succeeded)
        return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Email hoặc mật khẩu không hợp lệ.");

    var roles = await users.GetRolesAsync(user);
    var now = DateTime.UtcNow;
    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new("agency_id", user.AgencyId.ToString()),
        new("display_name", user.DisplayName)
    };
    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(configuration["JWT_ISSUER"] ?? "streamops-api",
        configuration["JWT_AUDIENCE"] ?? "streamops-web", claims, now,
        now.AddMinutes(15), credentials);
    return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token),
        token.ValidTo, user.DisplayName, roles));
}).AllowAnonymous().RequireRateLimiting("login");

auth.MapPost("/invitations", async (CreateInvitationRequest request, ClaimsPrincipal actor,
    StreamOpsDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 || string.IsNullOrWhiteSpace(request.RoleCode))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["invitation"] = ["Email hoặc vai trò không hợp lệ."] });
    var agencyId = actor.GetAgencyId();
    if (agencyId is null) return Results.Forbid();
    var allowedRoles = new HashSet<string>(StringComparer.Ordinal)
        { "HRManager", "HRStaff", "TalentManager", "OperationsManager", "Booking", "Marketing", "Finance", "FinanceApprover", "Analyst", "Streamer" };
    if (!allowedRoles.Contains(request.RoleCode)) return Results.ValidationProblem(new Dictionary<string, string[]>
        { ["roleCode"] = ["Vai trò không được hỗ trợ hoặc cần quy trình cấp quyền riêng."] });
    if ((request.RoleCode == "Streamer") != (request.StreamerId is not null))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamerId"] = ["Chỉ lời mời Streamer mới được gắn hồ sơ streamer."] });

    var email = request.Email.Trim().ToLowerInvariant();
    if (await db.Users.AnyAsync(x => x.AgencyId == agencyId && x.NormalizedEmail == email.ToUpperInvariant(), ct))
        return Results.Conflict(new { message = "Không thể tạo lời mời cho địa chỉ này." });
    var existing = await db.AccountInvitations.Where(x => x.AgencyId == agencyId && x.Email == email && x.AcceptedAt == null && x.RevokedAt == null)
        .ToListAsync(ct);
    foreach (var old in existing) old.RevokedAt = DateTimeOffset.UtcNow;
    if (request.StreamerId is Guid streamerId && !await db.Streamers.AnyAsync(x => x.Id == streamerId && x.AgencyId == agencyId && x.AccountUserId == null && x.Status == "active", ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["streamerId"] = ["Hồ sơ không hợp lệ hoặc đã có tài khoản."] });

    var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    var invite = new AccountInvitation
    {
        Id = Guid.NewGuid(), AgencyId = agencyId.Value, Email = email,
        TokenHash = HashToken(rawToken), RoleCode = request.RoleCode,
        StreamerId = request.StreamerId,
        InvitedBy = Guid.Parse(actor.FindFirstValue(ClaimTypes.NameIdentifier)!),
        ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
    };
    db.AccountInvitations.Add(invite);
    db.AuditLogs.Add(new AuditLog { AgencyId = agencyId.Value, ActorUserId = invite.InvitedBy,
        Action = "account.invitation.created", EntityType = "account_invitation", EntityId = invite.Id,
        Summary = $"Role={request.RoleCode}; email masked" });
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/v1/auth/invitations/{invite.Id}", new
    {
        invitationId = invite.Id,
        activationToken = rawToken,
        expiresAt = invite.ExpiresAt,
        delivery = "Copy this one-time token into the approved invitation channel. It is shown once."
    });
}).RequireAuthorization(policy => policy.RequireRole("Admin"));

auth.MapPost("/invitations/accept", async (AcceptInvitationRequest request, StreamOpsDbContext db,
    UserManager<StreamOpsUser> users, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length != 64 ||
        string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
        string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 160 ||
        string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Lời mời không hợp lệ hoặc đã hết hạn.");
    var hash = HashToken(request.Token);
    var invite = await db.AccountInvitations.SingleOrDefaultAsync(x => x.TokenHash == hash, ct);
    if (invite is null || invite.AcceptedAt is not null || invite.RevokedAt is not null || invite.ExpiresAt <= DateTimeOffset.UtcNow)
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Lời mời không hợp lệ hoặc đã hết hạn.");
    if (!string.Equals(invite.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Lời mời không hợp lệ hoặc đã hết hạn.");

    var user = new StreamOpsUser
    {
        Id = Guid.NewGuid(), AgencyId = invite.AgencyId, Email = invite.Email,
        UserName = invite.Email, DisplayName = request.DisplayName.Trim(), IsActive = true,
        EmailConfirmed = true
    };
    await using var transaction = await db.Database.BeginTransactionAsync(ct);
    var created = await users.CreateAsync(user, request.Password);
    if (!created.Succeeded)
        return Results.ValidationProblem(created.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
    var roleResult = await users.AddToRoleAsync(user, invite.RoleCode);
    if (!roleResult.Succeeded)
    {
        await users.DeleteAsync(user);
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Không thể kích hoạt tài khoản.");
    }
    if (invite.StreamerId is Guid streamerId)
    {
        var streamer = await db.Streamers.SingleOrDefaultAsync(x => x.Id == streamerId && x.AgencyId == invite.AgencyId && x.AccountUserId == null, ct);
        if (streamer is null)
        {
            await users.DeleteAsync(user);
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Lời mời không hợp lệ hoặc đã hết hạn.");
        }
        streamer.AccountUserId = user.Id;
    }
    invite.AcceptedAt = DateTimeOffset.UtcNow;
    db.AuditLogs.Add(new AuditLog { AgencyId = invite.AgencyId, ActorUserId = user.Id,
        Action = "account.invitation.accepted", EntityType = "user", EntityId = user.Id,
        Summary = "Account activated from one-time invitation" });
    await db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return Results.Ok(new { status = "active", message = "Tài khoản đã được kích hoạt. Hãy đăng nhập." });
}).AllowAnonymous();

var publicForms = app.MapGroup("/api/v1/public");
publicForms.MapPost("/streamer-applications", async (PublicStreamerApplication request, StreamOpsDbContext db,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 160 ||
        string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
        !request.ConsentAccepted)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["form"] = ["Vui lòng kiểm tra thông tin bắt buộc."] });
    var entry = new StreamerApplication
    {
        ApplicantName = request.Name.Trim(), Email = request.Email.Trim().ToLowerInvariant(),
        PlatformLinks = Limit(request.PlatformLinks, 1000), Note = Limit(request.Note, 2000), ConsentAt = DateTimeOffset.UtcNow
    };
    db.StreamerApplications.Add(entry);
    await db.SaveChangesAsync(ct);
    return Results.Accepted(value: new { received = true, reference = entry.Id.ToString("N")[..8] });
}).AllowAnonymous().RequireRateLimiting("public-intake");

publicForms.MapPost("/partnership-requests", async (PublicPartnershipRequest request, StreamOpsDbContext db,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Organization) || request.Organization.Length > 180 ||
        string.IsNullOrWhiteSpace(request.ContactName) || request.ContactName.Length > 160 ||
        string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
        string.IsNullOrWhiteSpace(request.Summary) || request.Summary.Length > 2000 ||
        !request.ConsentAccepted)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["form"] = ["Vui lòng kiểm tra thông tin bắt buộc."] });
    var entry = new PartnershipRequest
    {
        OrganizationName = request.Organization.Trim(), ContactName = request.ContactName.Trim(),
        Email = request.Email.Trim().ToLowerInvariant(), Summary = request.Summary.Trim(), ConsentAt = DateTimeOffset.UtcNow
    };
    db.PartnershipRequests.Add(entry);
    await db.SaveChangesAsync(ct);
    return Results.Accepted(value: new { received = true, reference = entry.Id.ToString("N")[..8] });
}).AllowAnonymous().RequireRateLimiting("public-intake");

app.MapWorkflowEndpoints();

if (args.Contains("--bootstrap-admin", StringComparer.Ordinal))
{
    await BootstrapAdmin.RunAsync(app.Services, builder.Configuration);
    return;
}

if (args.Contains("--seed-data", StringComparer.Ordinal))
{
    await DataSeeder.SeedAsync(app.Services);
    return;
}

if (app.Environment.IsDevelopment())
{
    await DataSeeder.SeedAsync(app.Services);
}

app.Run();

static string ConvertDatabaseUrl(string value)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgres" or "postgresql"))
        throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL URL.");
    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2) throw new InvalidOperationException("DATABASE_URL is incomplete.");
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = Uri.UnescapeDataString(userInfo[1]),
        SslMode = SslMode.Require,
        ChannelBinding = ChannelBinding.Prefer,
        Timeout = 15,
        CommandTimeout = 30,
        ApplicationName = "StreamOps"
    };
    return builder.ConnectionString;
}

static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
static string? Limit(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];

public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string DisplayName, IList<string> Roles);
public sealed record CreateInvitationRequest(string Email, string RoleCode, Guid? StreamerId);
public sealed record AcceptInvitationRequest(string Email, string Token, string DisplayName, string Password);
public sealed record PublicStreamerApplication(string Name, string Email, string? PlatformLinks, string? Note, bool ConsentAccepted);
public sealed record PublicPartnershipRequest(string Organization, string ContactName, string Email, string Summary, bool ConsentAccepted);

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetAgencyId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue("agency_id"), out var agencyId) ? agencyId : null;
}
