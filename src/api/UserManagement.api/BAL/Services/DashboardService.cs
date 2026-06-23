using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Repositories;
using UserManagement.Middlewares;
using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserActivityLogRepository _userActivityLogRepository;
        private readonly IUserMasterRepository _userMasterRepository;
        private readonly IHashIntegrityRepository _hashIntegrityRepository;
        private readonly ILogger<DashboardService> _logger;
        private readonly UserManagementDBContext _context;

        public DashboardService(UserManagementDBContext context,IUserActivityLogRepository userActivityLogRepository,IHashIntegrityRepository hashIntegrityRepository, ILogger<DashboardService> logger, IUserMasterRepository userMasterRepository)
        {
            _userActivityLogRepository = userActivityLogRepository;
            _userMasterRepository = userMasterRepository;
            _hashIntegrityRepository = hashIntegrityRepository;
            _logger = logger;
            _context = context;

        }
        public async Task<List<DailyUserLoginDTO>> GetMonthlyLoggedInUser(string? period)
        {
            try
            {
                DateTime endDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
                DateTime startDate;
                if (period != null)
                {
                    switch (period.ToLower())
                    {
                        case "one year":
                        case "1y":
                            startDate = endDate.AddYears(-1);
                            break;
                        case "six month":
                        case "6m":
                            startDate = endDate.AddMonths(-6);
                            break;
                        case "one month":
                        case "1m":
                            startDate = endDate.AddMonths(-1);
                            break;
                        default:
                            throw new ArgumentException("Invalid period. Allowed values: 'one year', 'six month', 'one month'.");
                    }
                }
                else
                {
                    int year = endDate.Month >= 4 ? endDate.Year : endDate.Year - 1;
                    DateTime fyStart = new DateTime(year, 4, 1);
                    startDate = fyStart;
                }

                    var dailyUserLogins = await _userActivityLogRepository
                        .GetFiltered(log =>
                            log.IsLogin &&
                            log.ActivityTime >= startDate &&
                            log.ActivityTime < endDate)
                        .GroupBy(log => log.ActivityTime.Date)
                        .Select(group => new DailyUserLoginDTO
                        {
                            LoginDay = group.Key,
                            UserLoginCount = group.Select(log => log.UserId).Distinct().Count()
                        })
                        .OrderBy(result => result.LoginDay)
                        .ToListAsync();

                return dailyUserLogins;
            }
            catch (ArgumentException ex)
            {
                throw new ApplicationException($"Invalid period specified.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logged-in user stats.");
                throw new ApplicationException("An error occurred while fetching user login data.", ex);
            }
        }
        public async Task<DashboardSummaryDTO> GetDashboardSummaryAsync()
        {
            return await _userMasterRepository.GetDashboardSummaryAsync();
        }

        public async Task<List<MostLoggedInUsersDTO>> MostLoggedInUsers()
        {
            try
            {
                var mostLoggedInUsers = await _userActivityLogRepository
                    .GetFiltered(log => log.IsLogin)
                    .GroupBy(log => log.UserId)
                    .Select(group => new
                    {
                        UserId = group.Key,
                        LoginCount = group.Count()
                    })
                    .OrderByDescending(x => x.LoginCount)
                    .Take(10)
                    .ToListAsync();

                var userIds = mostLoggedInUsers.Select(x => x.UserId).ToList();
                var userDetails = await _userMasterRepository
                    .GetFiltered(um => userIds.Contains(um.Id))
                    .ToListAsync();

                var result = mostLoggedInUsers
                    .Join(userDetails,
                        login => login.UserId,
                        user => user.Id,
                        (login, user) => new MostLoggedInUsersDTO
                        {
                             id = user.Id,
                            user_name = user.UserName,
                            name = user.Name,
                            designation = user.Designation,
                            login_count = login.LoginCount
                        })
                    .OrderByDescending(x => x.login_count)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving most logged-in users.");
                throw new ApplicationException("An error occurred while fetching most logged-in users.", ex);
            }
        }

        //public async Task<List<RecentlyUsersCreatedDTO>> RecentlyUsersCreated()
        //{
        //    try
        //    {
        //        var recentUsers = await (
        //    from um in _context.UserMasters
        //    join uha in _context.UserHasApplications on um.Id equals uha.UserId
        //    join app in _context.Applications on uha.AppId equals app.Id
        //    where um.IsActive == true
        //    orderby um.Id descending
        //    select new RecentlyUsersCreatedDTO
        //    {
        //        id = um.Id,
        //        user_name = um.UserName,
        //        name = um.Name,
        //        designation = um.Designation,
        //        application = app.Title
        //    }
        //).Take(10)
        //            .ToListAsync();

        //        return recentUsers;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving recently created users.");
        //        throw new ApplicationException("An error occurred while fetching recently created users.", ex);
        //    }
        //}

        public async Task<List<RecentlyUsersCreatedDTO>> RecentlyUsersCreated()
        {
            try
            {
                var recentUsers = await (
                    from um in _context.UserMasters
                    join uha in _context.UserHasApplications on um.Id equals uha.UserId
                    join app in _context.Applications on uha.AppId equals app.Id
                    where um.IsActive == true
                    group app by new
                    {
                        um.Id,
                        um.UserName,
                        um.Name,
                        um.Designation
                    }
                    into g
                    orderby g.Key.Id descending
                    select new RecentlyUsersCreatedDTO
                    {
                        id = g.Key.Id,
                        user_name = g.Key.UserName,
                        name = g.Key.Name,
                        designation = g.Key.Designation,
                        //application = string.Join(", ", g.Select(a => a.Title))
                        application = g.Select(a => a.Title).Distinct().ToList()

                    }
                )
                .Take(10)
                .ToListAsync();

                return recentUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recently created users.");
                throw new ApplicationException("An error occurred while fetching recently created users.", ex);
            }
        }


        public async Task<List<MonthlyUserLoginCountDTO>> GetMonthlyLoggedInUserCount()
        {
            try
            {
                var now = DateTime.UtcNow;
                int year = now.Month >= 4 ? now.Year : now.Year - 1;
                DateTime fyStart = new DateTime(year, 4, 1);
                DateTime fyEnd = fyStart.AddYears(1).AddTicks(-1);
                var months = Enumerable.Range(1, 12).ToList();
                var userCountsDict = await _userMasterRepository
                    .GetFiltered(um => um.CreatedAt >= fyStart && um.CreatedAt <= fyEnd)
                    .GroupBy(um =>
                        ((um.CreatedAt.Month + 12 - 4) % 12) + 1
                    )
                    .Select(g => new { FinancialMonth = g.Key, UsersCreated = g.Count() })
                    .ToDictionaryAsync(x => x.FinancialMonth, x => x.UsersCreated);

                var result = months.Select(m => new MonthlyUserLoginCountDTO
                {
                    month = m,
                    UserLoginCount = userCountsDict.ContainsKey(m) ? userCountsDict[m] : 0
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly user login counts.");
                throw new ApplicationException("An error occurred while fetching monthly user login counts.", ex);
            }
        }

        //public async Task<List<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter)
        //{
        //    return await _userActivityLogRepository.GetDailyUserActivity(filter);
        //}

        public async Task<PaginatedResult<UserActivityLogDTO>> GetDailyUserActivity(ActivityLogFilterDTO filter)
        {
            return await _userActivityLogRepository.GetDailyUserActivity(filter);
        }

        public async Task<List<UserLoginCountDTO>> GetCurrentLoggedInUserCount()
        {
            return await _userActivityLogRepository.GetCurrentLoggedInUserCount();
        }

        public async Task<List<DeviceLoginCount>> GetDeviceLoginsAsync()
        {
            return await _userActivityLogRepository.GetDeviceLoginsAsync();
        }

        public async Task<List<AgentLoginCount>> GetAgentLoginsAsync()
        {
            return await _userActivityLogRepository.GetAgentLoginsAsync();
        }

        public async Task<ActivityPageResponse> GetPagedActivities(ActivityLogRequestDto request)
        {
            return await _userActivityLogRepository.GetPagedActivities(request);
        }

        public async Task<bool> VerifyHashChain(string hash, int depth)
        {
            return await _hashIntegrityRepository.VerifyHashChain(hash, depth);
        }

    }







}

