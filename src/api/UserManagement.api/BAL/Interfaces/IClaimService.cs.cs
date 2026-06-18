using System.Security.Claims;
using UserManagement.Models.Claims;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface IClaimService
    {
        public string[] GetUserApplications();
        public string[] GetRoles();
        public string GetScopeByApplicationName(string applicationName);
        public string GetRoleByApplicationName(string applicationName);
        public int GetRoleIdByApplicationId(int applicationId);
        public List<int> GetRoleIdsByApplicationIds(List<int> applicationIds);
        public List<int> GetLevelIdsByApplicationIds(List<int> applicationIds);
        public List<string> GetScopesByApplicationName(string applicationName);
        public long GetTokenId();
        public Claim[] GetRawJwtClaims();
        public long GetPrevTokenId();
        public int GetUserId();
        public string GetUserName();
        public string GetDesignation();
        public int GetApplicationIdByApplicationName(string applicationName);
        int GetApplicationId();
        int GetRoleId();
        public ClaimModel.Application GetApplication();
        OtherModuleClaimsDTO GetClaimsForJWTForOtherApplication();
        public string GetSessionId();
    }
}
