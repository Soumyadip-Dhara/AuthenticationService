using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace UserManagement.Jwt.Handler
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ILogger _logger;
        public PermissionHandler(ILogger<PermissionHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var permissionsClaim = context.User.FindFirst(c => c.Type == "permissions");

            if (permissionsClaim != null)
            {
                try
                {
                    var userPermissions = JsonSerializer.Deserialize<List<string>>(permissionsClaim.Value);

                    if (userPermissions != null && requirement.Permissions.All(p => userPermissions.Contains(p, StringComparer.OrdinalIgnoreCase)))
                    {
                        context.Succeed(requirement);
                    }
                    else
                    {
                        _logger.LogWarning("User lacks required permissions: {Permissions}", string.Join(", ", requirement.Permissions));
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to deserialize permissions claim.");
                }
            }
            else
            {
                _logger.LogWarning("No permissions claim found for user.");
            }

            return Task.CompletedTask;
        }
    }
}
