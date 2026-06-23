using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserManagement.Models.DTO
{
    [Keyless]
    public class UserProfileQueryModel
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Designation { get; set; }
        public string MobileNumber { get; set; }
        public string Email { get; set; }
        public string Level { get; set; }
        public string? SignerID { get; set; }
    }

}




