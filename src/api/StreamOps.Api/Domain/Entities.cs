namespace StreamOps.Api.Domain;

public interface IAgencyRecord
{
    Guid Id { get; set; }
    Guid AgencyId { get; set; }
}

public sealed class Agency
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    public string DefaultCurrency { get; set; } = "VND";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Team : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid? ParentTeamId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class TeamMembership : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public string MembershipRole { get; set; } = "member";
    public DateOnly ValidFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? ValidTo { get; set; }
}

public sealed class EmployeeProfile : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid UserId { get; set; }
    public string EmployeeCode { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public Guid? ManagerUserId { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string EmploymentStatus { get; set; } = "active";
}

public sealed class Streamer : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Code { get; set; } = "";
    public string StageName { get; set; } = "";
    public string? LegalName { get; set; }
    public string Status { get; set; } = "lead";
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
    public Guid? AccountUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class StreamerAssignment : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? UserId { get; set; }
    public string AssignmentRole { get; set; } = "manager";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public sealed class Contract : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public string ContractReference { get; set; } = "";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public DateTimeOffset SignedAt { get; set; }
    public decimal StreamerRate { get; set; }
    public decimal AgencyRate { get; set; }
    public string Status { get; set; } = "active";
    public Guid CreatedBy { get; set; }
}

public sealed class Channel : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public string PlatformCode { get; set; } = "";
    public string ExternalChannelId { get; set; } = "";
    public string Handle { get; set; } = "";
    public string? Url { get; set; }
    public string Status { get; set; } = "active";
}

public sealed class LiveSchedule : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public Guid? ChannelId { get; set; }
    public string Title { get; set; } = "";
    public DateTimeOffset PlannedStartAt { get; set; }
    public DateTimeOffset PlannedEndAt { get; set; }
    public string Status { get; set; } = "proposed";
    public Guid CreatedBy { get; set; }
}

public sealed class LiveSession : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? ChannelId { get; set; }
    public DateTimeOffset? ActualStartAt { get; set; }
    public DateTimeOffset? ActualEndAt { get; set; }
    public string Status { get; set; } = "live";
    public string Source { get; set; } = "manual";
    public Guid RecordedBy { get; set; }
}

public sealed class SessionMetric : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid LiveSessionId { get; set; }
    public string MetricCode { get; set; } = "";
    public decimal Value { get; set; }
    public string Unit { get; set; } = "count";
    public string Source { get; set; } = "manual";
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid EnteredBy { get; set; }
}

public sealed class Customer : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Name { get; set; } = "";
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "active";
}

public sealed class Booking : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "proposed";
    public decimal GrossAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class BookingStreamer : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid BookingId { get; set; }
    public Guid StreamerId { get; set; }
    public decimal FeeAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "proposed";
}

public sealed class BookingDeliverable : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid BookingId { get; set; }
    public string Description { get; set; } = "";
    public DateTimeOffset? DueAt { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? AcceptedBy { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}

public sealed class TaskItem : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid TeamId { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? StreamerId { get; set; }
    public Guid? BookingId { get; set; }
    public string Title { get; set; } = "";
    public string Priority { get; set; } = "normal";
    public string Status { get; set; } = "open";
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialTransaction : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid? StreamerId { get; set; }
    public Guid? BookingId { get; set; }
    public string Type { get; set; } = "revenue";
    public string Category { get; set; } = "other";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTimeOffset OccurredAt { get; set; }
    public string Status { get; set; } = "draft";
    public string Source { get; set; } = "manual";
    public string? Reference { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RevenueShareRule : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public string RevenueType { get; set; } = "all";
    public decimal StreamerRate { get; set; }
    public decimal AgencyRate { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string Status { get; set; } = "pending_approval";
}

public sealed class Settlement : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid StreamerId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal GrossRevenue { get; set; }
    public decimal Deductions { get; set; }
    public decimal StreamerShare { get; set; }
    public decimal AgencyShare { get; set; }
    public string RuleSnapshot { get; set; } = "{}";
    public string Status { get; set; } = "draft";
    public Guid CreatedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? LockedAt { get; set; }
}

public sealed class SettlementLine : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid SettlementId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string LineType { get; set; } = "revenue";
}

public sealed class Payout : IAgencyRecord
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid SettlementId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Status { get; set; } = "pending";
    public string? PaymentReference { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public Guid RecordedBy { get; set; }
}

public sealed class StreamerApplication
{
    public Guid Id { get; set; }
    public string ApplicantName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PlatformLinks { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset ConsentAt { get; set; }
    public string Status { get; set; } = "received";
    public Guid? AssignedTo { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PartnershipRequest
{
    public Guid Id { get; set; }
    public string OrganizationName { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Summary { get; set; } = "";
    public DateTimeOffset ConsentAt { get; set; }
    public string Status { get; set; } = "received";
    public Guid? AssignedTo { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AccountInvitation
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public string Email { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public string RoleCode { get; set; } = "";
    public Guid? StreamerId { get; set; }
    public Guid InvitedBy { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid AgencyId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? RequestId { get; set; }
    public string? Summary { get; set; }
}
