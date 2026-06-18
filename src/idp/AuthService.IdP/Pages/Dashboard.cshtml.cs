using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenIddict.Abstractions;
using Microsoft.EntityFrameworkCore;
using AuthService.IdP.DAL;

using AuthService.IdP.DAL.Entities;

namespace AuthService.IdP.Pages;

public class DashboardModel : PageModel
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IDPDBContext1 _idpDbContext1;

    public DashboardModel(
        IOpenIddictApplicationManager applicationManager,
        IDPDBContext1 idpDbContext1)
    {
        _applicationManager = applicationManager;
        _idpDbContext1 = idpDbContext1;
    }

    public string UserName { get; set; } = "User";
    public string UserEmail { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public UserMaster? UserProfile { get; set; }
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

        // Get the logged-in user's database ID from claims
        var userIdStr = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdStr, out var userId))
        {
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
        }

        UserProfile = await _idpDbContext1.UserMasters.FirstOrDefaultAsync(u => u.Id == userId);

        // Retrieve the applications this user is authorized to access
        var allowedApps = await _idpDbContext1.UserHasApplications
            .Where(u => u.UserId == userId && u.App.IsActive)
            .Select(u => u.App)
            .ToListAsync();

        var allowedClientIds = allowedApps
            .Select(app => $"{GetAppCode(app.Title)}-bff")
            .ToHashSet();

        // Always allow the local demo application to maintain testing capabilities
        allowedClientIds.Add("demo-login-bff");

        // 3. Retrieve all registered applications
        await foreach (var app in _applicationManager.ListAsync())
        {
            var clientId = await _applicationManager.GetClientIdAsync(app);
            if (string.IsNullOrEmpty(clientId) || !allowedClientIds.Contains(clientId))
            {
                continue; // Only show applications the user is authorized to access
            }

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

    private static string GetAppCode(string title)
    {
        if (title.Equals("MasterDataManagement", StringComparison.OrdinalIgnoreCase))
            return "mdm";

        var code = title.ToLowerInvariant()
                        .Replace(" ", "")
                        .Replace("-", "")
                        .Replace("_", "");
        return code;
    }
}

public class ApplicationViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public bool IsPending { get; set; }
}
