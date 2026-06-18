using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Interfaces
{
    public interface IHashIntegrityRepository
    {
        Task<bool> VerifyHashChain(string hash, int depth);
    }

}
