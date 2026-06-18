//using UserManagement.DAL.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using System.Security.Claims;
using UserManagement.Model.Claims;
using UserManagement.Models.SSO;
using UserManagement.Models.Claims;
using UserManagement.Enum;
using Microsoft.AspNetCore.Http.Extensions;


namespace UserManagement.Filters
{

    public class AppAuthFilterAttribute : IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AppAuthFilterAttribute(string roles)
        {
            _roles = roles.Split(',');
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (!context.HttpContext.Items.TryGetValue("userclaimmodel", out object? userClaims))
            {
                context.Result = new JsonResult(new { message = "UnAuthenticated", apiResponseStatus = 3, result = false }) { StatusCode = StatusCodes.Status200OK };
                Console.WriteLine("Unable to authorize for: " + context.HttpContext.Request.GetDisplayUrl());
                Console.WriteLine("Using Authorization: " + context.HttpContext.Request.Headers.Authorization);
                return;
            }

            if(userClaims != null)
            {
                AuthClaimModel userclaimmodel = (AuthClaimModel)userClaims;
                List<Claim> userclaim = userclaimmodel.claims;
                bool acs = true;
                var application = userclaim.Where(c => c.Type == "application").Select(application => application).ToArray();

                var expClaim = userclaim.FirstOrDefault(c => c.Type == "exp");
                if (expClaim == null || !long.TryParse(expClaim.Value, out long expSeconds))
                {
                    context.Result = new JsonResult(new { message = "Invalid JWT expiration claim", apiResponseStatus = 3, result = false })
                    { StatusCode = StatusCodes.Status200OK };
                    return;
                }

                // Convert expiration from seconds since Unix epoch to DateTime
                var expDate = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                if (expDate <= DateTime.UtcNow)
                {
                    context.Result = new JsonResult(new { message = "JWT Expired", apiResponseStatus = 3, result = false })
                    { StatusCode = StatusCodes.Status401Unauthorized };
                    return;
                }
                var app = JsonSerializer.Deserialize<ClaimModel.Application>(application[0].Value);
                acs &= _roles.Contains(app.Role.Name);
                if (!acs)
                {
                    context.Result = new JsonResult(new { message = "Unauthorized Acess", apiResponseStatus = 3, result = false }) { StatusCode = StatusCodes.Status200OK };
                    return;
                }
            }

        }
    }

    public class AuthorizeAttribute : TypeFilterAttribute
    {
        public AuthorizeAttribute(string roles) : base(typeof(AppAuthFilterAttribute))
        {
            Arguments = [roles];
        }
    }

}
