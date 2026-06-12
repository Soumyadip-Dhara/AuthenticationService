using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;

namespace AuthService.IdP.Pages;

public class DashboardModel : PageModel
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public DashboardModel(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public string UserName { get; set; } = "User";
    public string UserEmail { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public List<ApplicationViewModel> Applications { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? returnUrl)
    {
        // 1. Authenticate using the IdP session cookie
        var result = await HttpContext.AuthenticateAsync("idp-session");
        if (!result.Succeeded || result.Principal == null)
        {
            // If not logged in, redirect back to login
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }

        ReturnUrl = returnUrl;

        // 2. Fetch user information
        UserName = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "User";
        UserEmail = result.Principal.FindFirstValue(ClaimTypes.Email) ?? "";

        // 3. Retrieve all registered applications
        await foreach (var app in _applicationManager.ListAsync())
        {
            var clientId = await _applicationManager.GetClientIdAsync(app);
            var displayName = await _applicationManager.GetDisplayNameAsync(app);
            
            // Check permissions to see if this is an interactive client app
            var permissions = await _applicationManager.GetPermissionsAsync(app);
            var isInteractive = permissions.Contains(OpenIddictConstants.Permissions.Endpoints.Authorization);
            
            if (!isInteractive)
            {
                continue; // Skip API resource servers and non-interactive clients
            }

            var redirectUris = await _applicationManager.GetRedirectUrisAsync(app);
            string targetUrl = "";
            bool isPending = false;

            // If we have a pending OIDC returnUrl that matches this client ID,
            // the user can select it to resume the auth flow.
            if (!string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.Contains($"client_id={clientId}"))
            {
                targetUrl = ReturnUrl;
                isPending = true;
            }
            else
            {
                // Otherwise, construct a default launch URL from the first redirect URI
                // e.g. https://localhost:5004/signin-oidc becomes https://localhost:5004/bff/login
                var redirectUri = redirectUris.FirstOrDefault();
                if (redirectUri != null && Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
                {
                    targetUrl = $"{uri.Scheme}://{uri.Authority}/bff/login";
                }
            }

            Applications.Add(new ApplicationViewModel
            {
                ClientId = clientId ?? "",
                DisplayName = displayName ?? clientId ?? "Unnamed App",
                TargetUrl = targetUrl,
                IsPending = isPending
            });
        }

        return Page();
    }
}

public class ApplicationViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public bool IsPending { get; set; }
}
