using Microsoft.AspNetCore.Authorization;

namespace UserManagement.Jwt.Handler
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(params string[] permissions)
        {
            Permissions = permissions;
        }
        public string[] Permissions { get; }
    }
}
