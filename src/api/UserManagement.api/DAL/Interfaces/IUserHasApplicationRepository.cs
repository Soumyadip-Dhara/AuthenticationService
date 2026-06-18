using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
namespace UserManagement.DAL.Interfaces
{
    public interface IUserHasApplicationRepository : IRepository<UserHasApplication>
    {
        Task<List<ApplicationFetchDTO>> GetApplicationsByUserId(long userId);

        Task<List<ApplicationGetDTO>> GetApplicationsByUserIdForUM(long userId);
    }
}