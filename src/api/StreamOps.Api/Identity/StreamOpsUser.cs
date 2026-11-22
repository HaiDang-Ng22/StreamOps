using Microsoft.AspNetCore.Identity;

namespace StreamOps.Api.Identity;

public sealed class StreamOpsUser : IdentityUser<Guid>
{
    public Guid AgencyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
