//using UserManagement.DAL.Enums;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Model.Claims;

namespace UserManagement.Authentication
{
    public interface ITokenHelper
    {
        //public string GenerateToken(UserClaimModel user, TokenType _tokenType, out DateTime ValidTo);
        public SecurityToken ValidateToken(string token, out int LifetimeExpirtedFlag);
        AuthClaimModel ValidateAndGetTokenClaims(string token, out bool RefreshedAccessTokenRecieved);
        public void InvalidateUserLogin(List<Guid> UserIds);
    }
}
