using System.Security.Claims;
using Newtonsoft.Json;
using UserManagement.BAL.Interfaces;

namespace UserManagement.BAL.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public ClaimService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public ClaimsPrincipal GetClaimsPrincipal()
        {
            return _contextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        }

        public string GetClaim(string claimType)
        {
            return GetClaimsPrincipal().FindFirstValue(claimType) ?? string.Empty;
        }

        public IEnumerable<string> GetClaims(string claimType)
        {
            return GetClaimsPrincipal().FindAll(claimType).Select(c => c.Value);
        }

        public int GetUserId()
        {
            var userIdStr = GetClaim("nameid");
            if (int.TryParse(userIdStr, out int userId))
            {
                return userId;
            }
            return 0;
        }

        public string GetUserName()
        {
            return GetClaim(ClaimTypes.Name) ?? GetClaim("name");
        }

        public string GetEmail()
        {
            return GetClaim(ClaimTypes.Email) ?? GetClaim("email");
        }

        public string GetSessionId()
        {
            return GetClaim("sid");
        }

        public string[] GetRoles()
        {
            var standardRoles = GetClaimsPrincipal().FindAll(ClaimTypes.Role).Select(c => c.Value);
            var customRoles = GetClaims("role");
            return standardRoles.Union(customRoles).Distinct().ToArray();
        }

        public List<string> GetPermissions()
        {
            var permissionsJson = GetClaim("permissions");
            if (string.IsNullOrEmpty(permissionsJson))
            {
                return new List<string>();
            }

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(permissionsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public string GetLevel()
        {
            return GetClaim("level");
        }

        public string GetScope()
        {
            return GetClaim("scope");
        }

        public string GetParentScope()
        {
            return GetClaim("parent_scope");
        }

        public string GetDesignation()
        {
            return GetClaim("designation");
        }

        public string GetFinYear()
        {
            return GetClaim("finyear");
        }

        public string GetDistrictCode()
        {
            return GetClaim("districtcode");
        }

        public string GetDdoCode()
        {
            return GetClaim("ddo_code");
        }

        public string GetTreasuryCode()
        {
            return GetClaim("treas_code");
        }

        public string GetSlsCode()
        {
            return GetClaim("sls_code");
        }

        // Legacy Mappings (Placeholders for compilation)
        public int GetRoleIdByApplicationId(int applicationId)
        {
            return 0;
        }

        public List<int> GetRoleIdsByApplicationIds(List<int> applicationIds)
        {
            return new List<int>();
        }

        public List<int> GetLevelIdsByApplicationIds(List<int> applicationIds)
        {
            return new List<int>();
        }
    }
}
