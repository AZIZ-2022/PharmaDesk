using Microsoft.AspNetCore.Authorization;

namespace PharmaDesk.Web.Security;

public class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public PermissionAuthorizeAttribute(string permissionCode)
    {
        Policy = $"Permission:{permissionCode}";
    }
}
