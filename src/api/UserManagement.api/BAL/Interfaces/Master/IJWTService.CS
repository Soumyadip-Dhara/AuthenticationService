using System.Security.Claims;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IJWTService
    {
        Task<AuthTokenForModules> IssueJwtToken(
            int appId, 
            Claim[] jwtClaims, 
            double accessTokenValidityInMinutes = 5,
            double refreshTokenValidityInMinutes = 10,
            long userId = 0,
            Guid? sessionId = null
        );
        Task<AuthTokenForModules> JWTTokenCreationForUMAndMMForMultiple(GetJWTPayload applicationData);
        Task<AuthTokenForModules> JWTTokenCreationForUMAndMM(DataCollectionJWTDTO dataCollectionDTO);
        Task<AuthTokenForModules> JWTTokenCreationForOtherForMultiple(GetJWTPayload applicationData);
        Task<AuthTokenForModules> JWTTokenCreationForOther(DataCollectionJWTDTO dataCollectionDTO);

    }
}
