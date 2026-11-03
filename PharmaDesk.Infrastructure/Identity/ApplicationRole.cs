using Microsoft.AspNetCore.Identity;

namespace PharmaDesk.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<long>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }

    public string? Description { get; set; }
}
