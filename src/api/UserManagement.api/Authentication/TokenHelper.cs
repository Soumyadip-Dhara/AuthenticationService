//using UserManagement.DAL.Enums;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.Model.Claims;

namespace UserManagement.api.Authentication
{
    public class TokenHelper : ITokenHelper
    {
        private readonly IConfiguration _Configuration;
        private readonly ILogger<TokenHelper> _logger;
        private readonly ITokencache _tokencache;
        private readonly string ActiveLifeTimeWindowinMint;
        private readonly string authSecretKey;

        public TokenHelper(ILogger<TokenHelper> logger, IConfiguration Configuration, ITokencache tokencache)
        {
            _logger = logger;
            _Configuration = Configuration;
            authSecretKey = _Configuration.GetValue<string>("Auth:SecretKey");
            ActiveLifeTimeWindowinMint = _Configuration.GetValue<string>("Auth:ActiveLifeTimeWindowinMint");
            _tokencache = tokencache;
        }

        /*
        public string GenerateToken(UserClaimModel user, TokenType _tokenType, out DateTime ValidTo)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSecretKey));
                double expireInterval;
                List<Claim> authClaims = null;

                if (_tokenType == TokenType.AccessToken)
                {
                    authClaims = new List<Claim>
                    {
                            new Claim(ClaimType.TokenType.ToString(), "AccessToken"),
                            new Claim(ClaimType.UserId.ToString(), user.UserId.ToString()),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                            new Claim(ClaimType.Role.ToString(), user.RoleName)

                    };

                    expireInterval = _Configuration.GetValue<double>("Auth:AccessTokenLifeTime", 30);
                }
                else
                {
                    //authClaims = new List<Claim>
                    //{
                    //        new Claim("TokenType", "RefreshToken"),
                    //        new Claim("UserId", user.UserId.ToString()),
                    //        new Claim("CreationDate", DateTime.UtcNow.ToString()),
                    //        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

                    //};
                    //expireInterval = Convert.ToDouble(_Configuration.GetValue<string>("Auth:RefreshTokenLifeTime"));

                    throw new Exception("Invalid token type execute");
                }

                var Createdtoken = new JwtSecurityToken(
                    expires: DateTime.UtcNow.AddMinutes(expireInterval),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );

                ValidTo = Createdtoken.ValidTo;
                var token = tokenHandler.WriteToken(Createdtoken);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
        }
        */
        /// <summary>
        /// out int LifetimeExpirtedFlag = 0 when the token is valid
        /// out int LifetimeExpirtedFlag = 1 when the token is invalid due any other reason
        /// out int LifetimeExpirtedFlag = 2 when the token is invalid due to its life time expiried only
        /// </summary>
        /// <param name="token"></param>
        /// <param name="LifetimeExpirtedFlag"></param>
        /// <returns></returns>
        public SecurityToken ValidateToken(string token, out int LifetimeExpirtedFlag)
        {
            SecurityToken validatedToken = null;

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();
                tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
                LifetimeExpirtedFlag = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("IDX10223: Lifetime validation failed."))
                    LifetimeExpirtedFlag = 2;
                else
                    LifetimeExpirtedFlag = 1;
                //_logger.LogError(ex);
                //IDX10223: Lifetime validation failed.
                validatedToken = null;
            }

            return validatedToken;
        }

        public AuthClaimModel ValidateAndGetTokenClaims(string token, out bool RefreshedAccessTokenRecieved)
        {
            int LifetimeExpirtedFlag = -1;
            var validToken = ValidateToken(token, out LifetimeExpirtedFlag);
            RefreshedAccessTokenRecieved = false;
            if (validToken != null)
            {
                var cachedItem = _tokencache.GetItem(token);/* just to increase time to leave time in cache*/
                var tokenHandler = new JwtSecurityTokenHandler();
                var authToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                return new AuthClaimModel() { RefreshedAccessToken = string.Empty, claims = authToken.Claims.ToList<Claim>() };
            }
            else if (LifetimeExpirtedFlag == 2)
            {
                var cachedItem = _tokencache.GetItem(token);

                if (cachedItem == null)
                {
                    return null;
                }
                else
                {
                    /*
                    _tokencache.RemoveItem(token);
                    TimeSpan span = DateTime.UtcNow.Subtract(cachedItem.tokenValidTo);
                    if (span.TotalMinutes > 0 && span.TotalMinutes < int.Parse(ActiveLifeTimeWindowinMint))
                    {
                        DateTime newValidTo;
                        var newAccessToken = GenerateToken(cachedItem.userClaimModel, TokenType.AccessToken, out newValidTo);
                        var newtokenHandler = new JwtSecurityTokenHandler();
                        var newauthToken = newtokenHandler.ReadToken(newAccessToken) as JwtSecurityToken;
                        var result = new AuthClaimModel() { RefreshedAccessToken = newAccessToken, claims = newauthToken.Claims.ToList<Claim>() };
                        _tokencache.PutItem(newAccessToken, new TokenChachedItems() { tokenValidTo = newValidTo, userClaimModel = cachedItem.userClaimModel });
                        RefreshedAccessTokenRecieved = true;
                        return result;
                    }
                    else
                    {
                        return null;
                    }
                    */
                }
                return null;

            }
            else
                return null;

        }


        public void InvalidateUserLogin(List<Guid> UserIds)
        {
            try
            {
                foreach (var userid in UserIds)
                    _tokencache.InvalidateUserToken(userid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw ex;
            }
        }

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidateLifetime = false, // Because there is no expiration in the generated token
                ValidateAudience = false, // Because there is no audiance in the generated token // need to add later
                ValidateIssuer = false,   // Because there is no issuer in the generated token
                //ValidIssuer = "Sample", //// need to add later
                //ValidAudience = "Sample", // // need to add later
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authSecretKey)) // The same key as the one that generate the token
            };
        }
    }
}
