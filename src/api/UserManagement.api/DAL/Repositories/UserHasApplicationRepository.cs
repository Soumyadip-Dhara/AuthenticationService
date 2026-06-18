using Microsoft.EntityFrameworkCore;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories
{
    public class UserHasApplicationRepository : Repository<UserHasApplication, UserManagementDBContext>, IUserHasApplicationRepository
    {
        private readonly UserManagementDBContext _context;
        private readonly IClaimService _claimService;
        private readonly IUserMasterRepository _userMasterRepository;

        public UserHasApplicationRepository(UserManagementDBContext context, IUserMasterRepository userMasterRepository, IClaimService claimService) : base(context)
        {
            _context = context;
            _claimService = claimService;
            _userMasterRepository = userMasterRepository;
        }

        public async Task<List<ApplicationFetchDTO>> GetApplicationsByUserId(long userId)
        {
            var onlyUM = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == _claimService.GetUserId(),
                e => e.IsOnlyUsermanagement);
            var applications = new List<ApplicationFetchDTO>();
            if (onlyUM)
            {
                applications = await _context.UserHasApplications.Where(e => e.UserId == userId && !e.App.Title.ToLower().Equals("user management")).Select(e => new ApplicationFetchDTO
                {
                    AppId = e.AppId,
                    ApplicationName = e.App.Title,
                    LogoUrl = e.App.LogoUrl
                }).Distinct().ToListAsync();
            }
            else
            {
                applications = await _context.UserHasApplications.Where(e => e.UserId == userId).Select(e => new ApplicationFetchDTO
                {
                    AppId = e.AppId,
                    ApplicationName = e.App.Title,
                    LogoUrl = e.App.LogoUrl
                }).Distinct().ToListAsync();
            }

            return applications;
        }
        public async Task<List<ApplicationGetDTO>> GetApplicationsByUserIdForUM(long userId)
        {
            var data = await _context.UserHasApplications.Where(e => e.UserId == userId && e.AppId != 1 && e.AppId != 5).Select(e => new ApplicationGetDTO
            {
                Id = e.AppId,
                Title = e.App.Title,
                url = e.App.Url,
                LogoUrl = e.App.LogoUrl
            }).Distinct().ToListAsync();

            return data;
        }
    }
}