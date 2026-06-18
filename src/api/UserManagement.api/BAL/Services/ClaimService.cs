using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Newtonsoft.Json;
using UserManagement.BAL.Interfaces;
using UserManagement.Model.Claims;
using UserManagement.Models.Claims;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private List<ClaimModel.Application> _applications = new List<ClaimModel.Application>();
        private AuthClaimModel logedinUserClaims = new AuthClaimModel();

        public ClaimService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
            if (_contextAccessor.HttpContext != null)
            {
                logedinUserClaims = (AuthClaimModel)_contextAccessor.HttpContext.Items["userclaimmodel"];
                if (logedinUserClaims != null && logedinUserClaims.claims != null)
                {
                    _applications = logedinUserClaims.claims.Where(claims => claims.Type == "application").Select(claim => JsonConvert.DeserializeObject<ClaimModel.Application>(claim.Value)).ToList();
                }
            }
            else
            {
                logedinUserClaims = null;
            }
        }

        //private AuthClaimModel? GetLoggedInUser()
        //{
        //    return _contextAccessor.HttpContext?
        //        .Items["userclaimmodel"] as AuthClaimModel;
        //}

        public Claim[] GetRawJwtClaims()
        {
            return [..logedinUserClaims.claims];
        }

        public string[] GetUserApplications()
        {
            string[] usersApplication = _applications.Select(application => application.Name).ToArray();
            return usersApplication;
        }

        public string[] GetRoles()
        {
            string[] userRole = _applications.Select(application => application.Role.Name).ToArray();
            return userRole;
        }

        public int GetRoleIdByApplicationId(int applicationId)
        {
            int roleId = _applications.Where(application => application.Id == applicationId).Select(application => application.Role).Select(role => role.Id).FirstOrDefault();
            return roleId;
        }

        public List<int> GetRoleIdsByApplicationIds(List<int> applicationIds)
        {
            List<int> roleIds = _applications
                .Where(application => applicationIds.Contains(application.Id))
                .Select(application => application.Role.Id)
                .ToList();

            return roleIds;
        }

        public List<int> GetLevelIdsByApplicationIds(List<int> applicationIds)
        {
            List<int> levelIds = _applications
                .Where(application => applicationIds.Contains(application.Id))
                .Select(application => application.Role.Level.Id)
                //.Select(level => level.Id)
                .ToList();

            return levelIds;
        }

        public List<string> GetScopesByApplicationName(string applicationName)
        {
            List<string> userScopes = _applications
            .Where(application => application.Name == applicationName)
            .Select(application => application.Role.Level.Scope)
            //.Select(level => level.Levels.Scope)
            //.Select(scope=>scope)
            .Distinct()
            .ToList();
            return userScopes;
        }

        public string GetScopeByApplicationName(string applicationName)
        {
            //string  userScope = _applications.Where(application => application.Name == applicationName).SelectMany(appliaction => appliaction.Roles).SelectMany(role => role.Scope).FirstOrDefault();
            return "";
        }

        public string GetRoleByApplicationName(string applicationName)
        {
            string roleName = _applications.Where(application => application.Name == applicationName).Select(role => role.Name).FirstOrDefault();
            return roleName;
        }

        public long GetTokenId()
        {
            if (logedinUserClaims != null)
            {
                return logedinUserClaims.claims.Where(
                    claims => claims.Type == JwtRegisteredClaimNames.Jti
                ).Select(
                    claim => long.Parse(claim.Value)
                ).FirstOrDefault();
            }
            return 0L;
        }

        public long GetPrevTokenId()
        {
            if (logedinUserClaims != null)
            {
                return logedinUserClaims.claims.Where(
                    claims => claims.Type == "pti"
                ).Select(
                    claim => long.Parse(claim.Value)
                ).FirstOrDefault();
            }
            return 0L;
        }
        public string GetSessionId()
        {
            if (logedinUserClaims != null)
            {
                return logedinUserClaims.claims.Where(
                    claims => claims.Type == "sid"
                ).Select(
                    claim => (claim.Value)
                ).FirstOrDefault();
            }
            return "";
        }

        public int GetUserId()
        {
            var userId = logedinUserClaims.claims.Where(claims => claims.Type == "nameid").Select(claim => int.Parse(claim.Value)).FirstOrDefault();
            Console.WriteLine("claim-GetUserId: " + userId);
            return userId;
        }

        public string GetUserName()
        {
            var userName = logedinUserClaims.claims.Where(claims => claims.Type == "name").Select(claim => claim.Value).FirstOrDefault();
            Console.WriteLine("claim-GetUserName: " + userName);
            return userName;
        }

        public string GetDesignation()
        {
            return logedinUserClaims.claims.Where(claims => claims.Type == "designation").Select(claim => claim.Value).FirstOrDefault();
        }

        public int GetApplicationIdByApplicationName(string applicationName)
        {
            int id = _applications.Where(applications => applications.Name == applicationName).Select(application => application.Id).FirstOrDefault();
            return id;
        }
        public int GetApplicationId()
        {
            int id = _applications.Select(application => application.Id).FirstOrDefault();
            return id;
        }
        public ClaimModel.Application GetApplication()
        {
            var application = _applications.Select(application => application).FirstOrDefault();
            return application;
        }
        public int GetRoleId()
        {
            int id = _applications.Select(application => application.Role.Id).FirstOrDefault();
            return id;
        }
        public OtherModuleClaimsDTO GetClaimsForJWTForOtherApplication()
        {
            return new OtherModuleClaimsDTO
            {
                AppId = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "aid")?.Value, out int appId) ? appId : 0,
                NameId = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value,
                Name = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value,
                Role = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "role")?.Value,
                RoleId = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "roleId")?.Value, out int roleId) ? roleId : 0,
                Permissions = JsonConvert.DeserializeObject<List<string>>(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "permissions")?.Value ?? "[]"),
                Level = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "level")?.Value,
                LevelId = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "levelId")?.Value, out int levelId) ? levelId : 0,
                Scope = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "scope")?.Value,
                ScopeId = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "scopeId")?.Value, out int scopeId) ? scopeId : 0,
                UserId = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "nameid")?.Value, out int userId) ? userId : 0,
                Email = logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "email")?.Value,
                CreatedBy = int.TryParse(logedinUserClaims.claims.FirstOrDefault(claim => claim.Type == "created_by")?.Value, out int createdBy) ? createdBy : 0
            };
        }

    }
}
