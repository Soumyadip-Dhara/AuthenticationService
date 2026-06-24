using System.Collections.Generic;
using System.Security.Claims;

namespace UserManagement.BAL.Interfaces
{
    public interface IClaimService
    {
        long GetUserId();
        string[] GetRoles();
        IEnumerable<Claim> GetAllClaims();
    }
}
