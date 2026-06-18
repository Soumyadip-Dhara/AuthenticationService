using System.Security.Claims;

namespace UserManagement.Model.Claims
{
    public class AuthClaimModel
    {
        public List<Claim> claims { get; set; }
        public string RefreshedAccessToken { get; set; }
    }
}
