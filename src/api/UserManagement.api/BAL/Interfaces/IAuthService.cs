using UserManagement.Models;

namespace UserManagement.BAL.Interfaces
{
    public interface IAuthService
    {
        public Task<UserModel> GetUserDetails(string userId);

        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
        Task<bool> ValidateToken(string token);
        //public Task<List<UserPrivilege>> GetUserAccess(int userId);
        //public Task<List<Claim>> GenerateModuleClaims(int userId,int SubSystemId);
    }
}
