using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Model.Claims;
using UserManagement.Models;

namespace UserManagement.BAL.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepositories;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(IAuthRepository authRepositorie, IMapper mapper, IConfiguration configuration)
        {
            _authRepositories = authRepositorie;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<UserModel> GetUserDetails(string userName)
        {
            var result = await _authRepositories.GetSingleAysnc(login => login.UserName == userName);
            return _mapper.Map<UserModel>(result);
        }

        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }

        public async Task<bool> ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Auth:SecretKey"])),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true, // Ensures the token has not expired
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            bool isAuthenticated = principal.Identity.IsAuthenticated;

            return isAuthenticated;
        } 
    }
}
