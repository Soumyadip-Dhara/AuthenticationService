using System.Security.Claims;

namespace UserManagement.BAL.Interfaces
{
    public interface IClaimService
    {
        // Core Identity Claims
        int GetUserId();
        string GetUserName();
        string GetEmail();
        string GetSessionId();

        // Role & Authorization Claims
        string[] GetRoles();
        List<string> GetPermissions();
        
        // Contextual/Profile Claims
        string GetLevel();
        string GetScope();
        string GetParentScope();
        string GetDesignation();
        string GetFinYear();

        // Specific codes
        string GetDistrictCode();
        string GetDdoCode();
        string GetTreasuryCode();
        string GetSlsCode();

        // Generic Claim Fetching
        string GetClaim(string claimType);
        IEnumerable<string> GetClaims(string claimType);
        
        // Legacy Application Mappings (Placeholders)
        int GetRoleIdByApplicationId(int applicationId);
        List<int> GetRoleIdsByApplicationIds(List<int> applicationIds);
        List<int> GetLevelIdsByApplicationIds(List<int> applicationIds);

        // Full Claims Principal access
        ClaimsPrincipal GetClaimsPrincipal();
    }
}
