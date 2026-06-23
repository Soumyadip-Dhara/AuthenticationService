namespace UserManagement.Models.DTO
{
    public class ApplicationDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RoleDTO Role { get; set; }
    }

    public class RoleDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Permissions { get; set; }
        public LevelDTO Level { get; set; }
    }

    public class LevelDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long ScopeId { get; set; }
        public string Scope { get; set; }
    }

    public class GetJWTPayload
    {
        public List<ApplicationDTO> Application { get; set; }
        public string? Username { get; set; }
        public int UserId { get; set; }
    }
    public class JWTDTO
    {
        public List<ApplicationDTO> Application { get; set; }
        public string Username { get; set; }
        public int UserId { get; set; }
        public long Nbf { get; set; }
        public long Exp { get; set; }
        public long Iat { get; set; }
    }
    public class DataCollectionJWTDTO
    {
        public int AppId { get; set; }
        public int UserId { get; set; }
    }
    //public class LogOutDTO
    //{
    //    public int AppId { get; set; }
    //    public int UserId { get; set; }
    //}

    public class NewJWTToken
    {
        public string Role { get; set; }
        public List<string> Permissions { get; set; }
        public string Level { get; set; }
        public string Scope { get; set; }
        public int NameId { get; set; }
        public string Name { get; set; }
        public long Iat { get; set; }
        public long Nbf { get; set; }
        public long Exp { get; set; }
    }
    public class AuthToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
    public class AuthTokenForModules
    {
        public string Url { get; set; }
        public string ApplicationName { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public double RefreshTokenValidityInMinutes { get; set; }
        public string? GuidToken { get; set; }
    }

    public class OtherModuleClaimsDTO
    {
        public int AppId { get; set; }
        public string Role { get; set; } = null!;
        public int RoleId { get; set; }
        public List<string> Permissions { get; set; } = null!;
        public string Level { get; set; } = null!;
        public int LevelId { get; set; }
        public string Scope { get; set; } = null!;
        public int ScopeId { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string NameId { get; set; } = null!;
        public int CreatedBy { get; set; }
    }
}
