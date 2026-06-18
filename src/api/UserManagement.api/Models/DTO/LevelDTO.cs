using SixLabors.ImageSharp.Memory;
using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class LevelGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public int? Rank { get; set; }
        public string ApplicationName { get; set; } = null!;
        public int ApplicationId { get; set; }
        public string? ScopeName { get; set; }
        public List<string>? AccisibleLevels { get; set; }
        public List<string> AllowedRoles { get; set; } = null!;
        public List<string> AdminRole { get; set; } = null!;
        public bool? SameLeveladminAllowed { get; set; }
        public string CreatedBy { get; set; } = null!;
        public bool? IsGlobal { get; set; }
        
    }
    public class LevelByApplicationsDTO
    {
        public List<int> applicationIds { get; set; } = null!;
    }
    public class LevelSetDTO
    {
        [RegularExpression(
            @"[a-zA-Z]*",
            ErrorMessage = "{0} must be alphabetic characters only"
        )]
        public string name { get; set; } = null!;
        public int applicationId { get; set; }

        [RegularExpression(
            @"^(?:[1-9][0-9]*|0\.[0-9]*[1-9][0-9]*)$",
            ErrorMessage = "{0} must be numeric characters only and must be greater than 0"
        )]
        public int rank { get; set; }

        public List<VisibleToLevelDTO> VisibleToLevels { get; set; } = null!;
    }

    public class LevelPayloadDTO
    {
        public string Name { get; set; } = null!;
        public int ApplicationId { get; set; }
        public int Rank { get; set; }
        public List<VisibleToLevelDTO> VisibleToLevels { get; set; } = null!;
        public List<AllowedRoleDTO> AllowedRoles { get; set; } = null!;
        public int AdminRole { get; set; }
        public int GlobalLevel { get; set; }
        public bool IsGlobalLevel { get; set; }
        public bool SameLevelOtherOfficeAdminAllowed { get; set; }
    }

    public class VisibleToLevelDTO
    {
        public int Id { get; set; }
    }
    public class AllowedRoleDTO
    {
        public int Id { get; set; }
    }
    public class LevelFetchDTO
    {
        public long LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public long OriginalId { get; set; }

    }

    public class LevelAndScopeCombo
    {
        public long LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public List<ScopeFetchDTO> Scopes { get; set; } = null!;
    }

    public class AcessLevelAndAllLevel
    {
        public List<AcesseLevel> AcesseLevel { get; set; } = null!;
        public List<AllLevel> AllLevel { get; set; } = null!; 
    }

    public class AcesseLevel
    {
        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;

    }
    public class AllLevel
    {
        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;
    }

    public class LevelUpdateDTO
    {
        [RegularExpression(
            @"[a-zA-Z]*",
            ErrorMessage = "{0} must be alphabetic characters only"
        )]
        public string name { get; set; } = null!;
        public int applicationId { get; set; }
        public int levelId { get; set; }

        [RegularExpression(
            @"^(?:[1-9][0-9]*|0\.[0-9]*[1-9][0-9]*)$",
            ErrorMessage = "{0} must be numeric characters only, and must be greater than 0"
        )]
        public int rank { get; set; }
        public List<AcesseLevel> acesseLevel { get; set; } = null!;
        public List<AllowedRole> allowedRoles { get; set; } = null!;
        public AllowedRole adminRole { get; set; } = null!;
        public bool SameLevelAdminAllowed { get; set; }
    }
    public class AllowedRole
    {
        public int id { get; set; }
        public string title { get; set; } = null!;
    }
    public class AllRole
    {
        public int id { get; set; }
        public string title { get; set; } = null!;
    }
    public class FetchRoleDataByLevel
    {
        public List<AllowedRole> allowdRoles { get; set; } = null!;
        public List<AllRole> allRole { get; set; } = null!;

    }
}
