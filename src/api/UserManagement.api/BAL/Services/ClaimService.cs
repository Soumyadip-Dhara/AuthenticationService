using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Security.Claims;
using UserManagement.BAL.Interfaces;

namespace UserManagement.BAL.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClaimService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdString = user?.FindFirst("nameid")?.Value 
                ?? user?.FindFirst("sub")?.Value 
                ?? user?.FindFirst("userId")?.Value 
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? "0";
                return long.TryParse(userIdString, out var id) ? id : 0;
        }

        public string[] GetRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var roles = user?.FindAll("role").Select(c => c.Value)
                .Concat(user?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>())
                .Distinct()
                .ToArray() ?? Array.Empty<string>();
            return roles;
        }

        public System.Collections.Generic.IEnumerable<Claim> GetAllClaims()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Claims ?? System.Linq.Enumerable.Empty<Claim>();
        }
    }
}
