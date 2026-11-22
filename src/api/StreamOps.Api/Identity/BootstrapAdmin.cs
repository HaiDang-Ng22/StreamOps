using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StreamOps.Api.Data;
using StreamOps.Api.Domain;

namespace StreamOps.Api.Identity;

public static class BootstrapAdmin
{
    private static readonly string[] RoleNames =
    [
        "Admin", "HRManager", "HRStaff", "TalentManager", "OperationsManager",
        "Booking", "Marketing", "Finance", "FinanceApprover", "Analyst", "Streamer", "Auditor"
    ];

    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BOOTSTRAP_ADMIN_EMAIL"]?.Trim();
        var password = configuration["BOOTSTRAP_ADMIN_PASSWORD"];
        var agencyName = configuration["BOOTSTRAP_AGENCY_NAME"]?.Trim();
        var agencySlug = configuration["BOOTSTRAP_AGENCY_SLUG"]?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(agencyName) || string.IsNullOrWhiteSpace(agencySlug))
            throw new InvalidOperationException("Set bootstrap values through environment variables or a secret store before running this one-time command.");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StreamOpsDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<StreamOpsUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (await db.Users.AnyAsync())
            throw new InvalidOperationException("Bootstrap refused: the identity store already contains a user.");

        await using var transaction = await db.Database.BeginTransactionAsync();
        var agency = new Agency { Id = Guid.NewGuid(), Name = agencyName, Slug = agencySlug };
        db.Agencies.Add(agency);
        await db.SaveChangesAsync();

        foreach (var roleName in RoleNames)
        {
            if (!await roles.RoleExistsAsync(roleName))
            {
                var result = await roles.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
                if (!result.Succeeded) throw new InvalidOperationException("Could not initialize the role catalog.");
            }
        }

        var admin = new StreamOpsUser
        {
            Id = Guid.NewGuid(), AgencyId = agency.Id, DisplayName = "Agency Administrator",
            Email = email, UserName = email, EmailConfirmed = true, IsActive = true
        };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded) throw new InvalidOperationException("Bootstrap admin was rejected by password policy.");
        var assigned = await users.AddToRoleAsync(admin, "Admin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Could not grant the initial administrator role.");
        db.AuditLogs.Add(new AuditLog { AgencyId = agency.Id, ActorUserId = admin.Id,
            Action = "system.initial_admin_created", EntityType = "user", EntityId = admin.Id,
            Summary = "One-time bootstrap command" });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        Console.WriteLine("Initial agency and administrator created. Remove bootstrap secrets from the environment now.");
    }
}
