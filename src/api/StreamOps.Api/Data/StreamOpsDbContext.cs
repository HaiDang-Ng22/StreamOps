using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StreamOps.Api.Domain;
using StreamOps.Api.Identity;
using System.Text.RegularExpressions;

namespace StreamOps.Api.Data;

public sealed class StreamOpsDbContext(
    DbContextOptions<StreamOpsDbContext> options)
    : IdentityDbContext<StreamOpsUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Agency> Agencies => Set<Agency>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<Streamer> Streamers => Set<Streamer>();
    public DbSet<StreamerAssignment> StreamerAssignments => Set<StreamerAssignment>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<LiveSchedule> LiveSchedules => Set<LiveSchedule>();
    public DbSet<LiveSession> LiveSessions => Set<LiveSession>();
    public DbSet<SessionMetric> SessionMetrics => Set<SessionMetric>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStreamer> BookingStreamers => Set<BookingStreamer>();
    public DbSet<BookingDeliverable> BookingDeliverables => Set<BookingDeliverable>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<RevenueShareRule> RevenueShareRules => Set<RevenueShareRule>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<SettlementLine> SettlementLines => Set<SettlementLine>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<StreamerApplication> StreamerApplications => Set<StreamerApplication>();
    public DbSet<PartnershipRequest> PartnershipRequests => Set<PartnershipRequest>();
    public DbSet<AccountInvitation> AccountInvitations => Set<AccountInvitation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("streamops");

        builder.Entity<StreamOpsUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(160).IsRequired();
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        builder.Entity<Agency>(entity =>
        {
            entity.ToTable("agencies", "streamops");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });

        builder.Entity<StreamerApplication>().ToTable("streamer_applications", "streamops");
        builder.Entity<PartnershipRequest>().ToTable("partnership_requests", "streamops");
        builder.Entity<AccountInvitation>(entity =>
        {
            entity.ToTable("account_invitations", "streamops");
            entity.HasIndex(x => x.TokenHash).IsUnique();
        });
        builder.Entity<AuditLog>().ToTable("audit_logs", "streamops");

        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(type => typeof(IAgencyRecord).IsAssignableFrom(type.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property(nameof(IAgencyRecord.AgencyId)).IsRequired();
            builder.Entity(entityType.ClrType).HasIndex(nameof(IAgencyRecord.AgencyId));
        }

        builder.Entity<Booking>().Property(x => x.GrossAmount).HasPrecision(19, 4);
        builder.Entity<BookingStreamer>().Property(x => x.FeeAmount).HasPrecision(19, 4);
        builder.Entity<TaskItem>().ToTable("tasks", "streamops");
        builder.Entity<FinancialTransaction>().Property(x => x.Amount).HasPrecision(19, 4);
        builder.Entity<RevenueShareRule>().Property(x => x.StreamerRate).HasPrecision(7, 4);
        builder.Entity<RevenueShareRule>().Property(x => x.AgencyRate).HasPrecision(7, 4);
        builder.Entity<Contract>().Property(x => x.StreamerRate).HasPrecision(7, 4);
        builder.Entity<Contract>().Property(x => x.AgencyRate).HasPrecision(7, 4);
        builder.Entity<Settlement>().Property(x => x.GrossRevenue).HasPrecision(19, 4);
        builder.Entity<Settlement>().Property(x => x.Deductions).HasPrecision(19, 4);
        builder.Entity<Settlement>().Property(x => x.StreamerShare).HasPrecision(19, 4);
        builder.Entity<Settlement>().Property(x => x.AgencyShare).HasPrecision(19, 4);
        builder.Entity<Settlement>().Property(x => x.RuleSnapshot).HasColumnType("jsonb");
        builder.Entity<Payout>().Property(x => x.Amount).HasPrecision(19, 4);
        builder.Entity<SessionMetric>().Property(x => x.Value).HasPrecision(20, 4);

        builder.Entity<Team>().HasIndex(x => new { x.AgencyId, x.Code }).IsUnique();
        builder.Entity<Streamer>().HasIndex(x => new { x.AgencyId, x.Code }).IsUnique();
        builder.Entity<Contract>().HasIndex(x => new { x.AgencyId, x.ContractReference }).IsUnique();
        builder.Entity<TeamMembership>().HasIndex(x => new { x.TeamId, x.UserId, x.ValidFrom }).IsUnique();
        builder.Entity<Channel>().HasIndex(x => new { x.AgencyId, x.PlatformCode, x.ExternalChannelId }).IsUnique();
        builder.Entity<Settlement>().HasIndex(x => new { x.AgencyId, x.StreamerId, x.PeriodStart, x.PeriodEnd }).IsUnique();
        builder.Entity<SettlementLine>().HasIndex(x => new { x.AgencyId, x.TransactionId }).IsUnique();
        builder.Entity<Booking>().HasIndex(x => new { x.AgencyId, x.Status, x.CreatedAt });
        builder.Entity<LiveSchedule>().HasIndex(x => new { x.AgencyId, x.PlannedStartAt });
        builder.Entity<FinancialTransaction>().HasIndex(x => new { x.AgencyId, x.OccurredAt, x.Status });
        builder.Entity<TaskItem>().HasIndex(x => new { x.AgencyId, x.OwnerUserId, x.Status, x.DueAt });
        builder.Entity<Streamer>().HasIndex(x => new { x.AgencyId, x.Id }).IsUnique();
        builder.Entity<Team>().HasIndex(x => new { x.AgencyId, x.Id }).IsUnique();
        builder.Entity<Booking>().HasIndex(x => new { x.AgencyId, x.Id }).IsUnique();
        builder.Entity<BookingStreamer>().HasIndex(x => new { x.AgencyId, x.BookingId, x.StreamerId }).IsUnique();
        builder.Entity<LiveSession>().HasIndex(x => new { x.AgencyId, x.Id }).IsUnique();

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var table = entityType.GetTableName();
            if (!string.IsNullOrWhiteSpace(table))
            {
                entityType.SetTableName(ToSnakeCase(table));
                entityType.SetSchema("streamops");
            }
            foreach (var property in entityType.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string value)
        => Regex.Replace(value, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
}
