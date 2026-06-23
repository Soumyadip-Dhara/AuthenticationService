using UserManagement.Jwt.Handler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace UserManagement.Jwt.Auth
{
    /// <summary>
    /// Handles OR-based evaluation of comma-separated policy names.
    /// When a policy name like "FullAccess,can-ddo-create" is requested,
    /// this provider builds a combined policy that succeeds if ANY of the
    /// individual named policies succeeds.
    /// </summary>
    public class AnyPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

        public AnyPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallbackProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallbackProvider.GetFallbackPolicyAsync();

        public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // If no comma, just return the named policy from the fallback provider
            if (!policyName.Contains(','))
                return await _fallbackProvider.GetPolicyAsync(policyName);

            // Split the comma-separated policy names
            var policyNames = policyName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Resolve each individual policy
            var policies = new List<AuthorizationPolicy>();
            foreach (var name in policyNames)
            {
                var policy = await _fallbackProvider.GetPolicyAsync(name);
                if (policy != null)
                    policies.Add(policy);
            }

            if (policies.Count == 0)
                return null;

            // Build an OR-combined policy using AnyPolicyRequirement
            var builder = new AuthorizationPolicyBuilder();
            builder.Requirements.Add(new AnyPolicyRequirement(policies));
            return builder.Build();
        }
    }

    /// <summary>
    /// A requirement that holds multiple policies and is satisfied if ANY of them succeeds.
    /// </summary>
    public class AnyPolicyRequirement : IAuthorizationRequirement
    {
        public IReadOnlyList<AuthorizationPolicy> Policies { get; }

        public AnyPolicyRequirement(IEnumerable<AuthorizationPolicy> policies)
        {
            Policies = policies.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Handler for AnyPolicyRequirement - succeeds if the user satisfies ANY of the contained policies.
    /// Uses IServiceProvider to lazily resolve IAuthorizationService and avoid circular dependency.
    /// </summary>
    public class AnyPolicyHandler : AuthorizationHandler<AnyPolicyRequirement>
    {
        private readonly IServiceProvider _serviceProvider;

        public AnyPolicyHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AnyPolicyRequirement requirement)
        {
            // Lazily resolve IAuthorizationService to avoid circular dependency at registration time
            var authorizationService = _serviceProvider.GetRequiredService<IAuthorizationService>();

            foreach (var policy in requirement.Policies)
            {
                var result = await authorizationService.AuthorizeAsync(context.User, context.Resource, policy);
                if (result.Succeeded)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
            // None of the policies matched — requirement not met
        }
    }
}
