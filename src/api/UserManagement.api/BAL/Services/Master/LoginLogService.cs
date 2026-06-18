using AutoMapper;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;

namespace UserManagement.BAL.Services.Master
{
    public class LoginLogService : ILoginLogService
    {
        private readonly IMapper _mapper;
        private readonly ILoginLogRepository _loginLogRepository;
        private readonly IConfiguration _configuration;

        public LoginLogService(ILoginLogRepository LoginLogRepository, IMapper mapper, IConfiguration configuration)
        {
            _loginLogRepository = LoginLogRepository;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<(bool,string)> IsMultipleLoggedIn(long userId, int appId)
        {
            //APIResponseClass<AuthenticatedUserRoleSelectedResponse> response = new();

            var userLog = await _loginLogRepository.GetSingleAysnc(
                        e => e.UserId == userId && e.ApplicationId == appId
                   );


            if (userLog != null)
            {
                if (userLog.LoginTime.AddMinutes(Double.Parse(_configuration["TimeInMinutes:SSO"].ToString())) > DateTime.UtcNow)
                {
                    return (true, "Your User Id is already logged in to this application in different session.");
                }
                else
                {
                    var resLogin = _loginLogRepository.Delete(userLog);
                    if (!resLogin)
                    {
                        return (true, "Login failed, please try again..");
                    }
                    _loginLogRepository.SaveChangesManaged();
                    return (false, "Token expired");
                }
            }
            else
            {
                return (false, "Not Logged In");
            }
        }
    }
}