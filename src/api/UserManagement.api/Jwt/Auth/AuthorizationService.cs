using UserManagement.Jwt.Handler;
using Microsoft.AspNetCore.Authorization;

namespace UserManagement.Jwt.Auth
{
    public static class AuthorizationService
    {
        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            // Register custom authorization handler
            services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

            // Add authorization policies
            services.AddAuthorization(options =>
            {
                // Policy for non-admin users with read-only access
                options.AddPolicy("ReadOnly", policy =>
                {
                    policy.Requirements.Add(new PermissionRequirement("read"));
                });

                // Policy for full access (admins only)
                options.AddPolicy("FullAccess", policy =>
                {
                    policy.RequireRole("admin");
                    policy.Requirements.Add(new PermissionRequirement("create", "update", "delete", "read"));
                });              

                options.AddPolicy("can-ddo-create", policy =>
                {
                    policy.RequireRole("ddo_manager");
                    policy.Requirements.Add(new PermissionRequirement("can-ddo-create"));
                });

                options.AddPolicy("can-ddo-update", policy =>
                {
                    policy.RequireRole("ddo_manager");
                    policy.Requirements.Add(new PermissionRequirement("can-ddo-update"));
                });

                options.AddPolicy("can-ddo-delete", policy =>
                {
                    policy.RequireRole("ddo_manager");
                    policy.Requirements.Add(new PermissionRequirement("can-ddo-delete"));
                });

                options.AddPolicy("can-ddo-read", policy =>
                {
                    policy.RequireRole("ddo_manager");
                    policy.Requirements.Add(new PermissionRequirement("can-ddo-read"));
                });

                // Deny access policy
                options.AddPolicy("DoNotAllow", policy => policy.RequireAssertion(_ => false));
            });
        }
    }
}
