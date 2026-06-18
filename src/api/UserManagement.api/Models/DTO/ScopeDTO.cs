using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class ScopeStructureDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
        public int LevelId { get; set; }
    }

    public class ScopeDataInsertDTO
    {
        //[RegularExpression(
        //@"[a-zA-Z0-9(),\-\s]*",
        //ErrorMessage = "{0} must contain only alphanumeric characters, spaces, commas (,), hyphens (-), white space ( ), and parentheses ()."
        // )]
        //public string Name { get; set; } = null!;

        //[RegularExpression(
        //    @"[a-zA-Z0-9]*",
        //    ErrorMessage = "{0} must be alphanumeric characters only"
        //)]
        //public string Value { get; set; } = null!;

        [RegularExpression(
    @"[a-zA-Z0-9(),_\-\s\$]*",
    ErrorMessage = "{0} must contain only alphanumeric characters, spaces, commas (,), hyphens (-), whitespace, parentheses (),  $ and _."
)]
        public string Name { get; set; } = null!;

        [RegularExpression(
            @"[a-zA-Z0-9_\$]*",
            ErrorMessage = "{0} must be alphanumeric characters or $ or _ only."
        )]
        public string Value { get; set; } = null!;
        public int LevelId { get; set; }

        public int ApplicationId { get; set; }
        public List<VisibleToScopes>? VisibleToScopes { get; set; }
        public bool IsGlobalScope { get; set; }

    }
    public class VisibleToScopes
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
        public int LevelId { get; set; }
    }

    public class ScopeCreateDTO
    {
        [RegularExpression(
            @"[a-zA-Z(),-]*",
            ErrorMessage = "{0} must be alphabetic characters only, can include commas(,), can include hyphens(-), can include first brackets(())."
        )]
        public string Name { get; set; } = null!;
        public int LevelId { get; set; }
    }

    public class ScopeFetchDTO
    {
        public long ScopeId { get; set; }
        public string ScopeName { get; set; } = null!;
        public string ScopeValue { get; set; } = null!;
        public int? LevelId { get; set; }
        // Override Equals and GetHashCode to ensure uniqueness by ScopeId
        public override bool Equals(object obj)
        {
            if (obj is ScopeFetchDTO other)
            {
                return ScopeId == other.ScopeId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ScopeId.GetHashCode();
        }
    }
    public class ScopesDTO
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
        public List<string>? accessedByScopes { get; set; }
        public bool IsActive { get; set; }
        public short Status { get; set; }
        public bool IsGlobal { get; set; }
    }
    public class ScopeDataForSearchDTO
    {
        public long Id { get; set; }
        public int LevelId { get; set; }
    }

    public class ScopeDataForSearchOwnOfficeDTO
    {

        public int LevelId { get; set; }
        public string LevelName { get; set; } = null!;
        public List<ScopeDto> Scopes { get; set; } = new List<ScopeDto>();
    }
    public class ScopeDto
    {
        public long ScopeId { get; set; }
        public string ScopeName { get; set; } = null!;
    }
    //public class CSVScopeDataDTO
    //{
    //    public int AppId { get; set; }
    //    public int LevelId { get; set; }
    //    public List<ScopeDetailsDto> Scopes { get; set; } = new List<ScopeDetailsDto>();
    //}

    //public class ScopeDetailsDto
    //{
    //    public string Name { get; set; } = null!;
    //    public string Value { get; set; } = null!;
    //    public string ParentScopeCode { get; set; } = null!;
    //    public string ParentLevelName { get; set; } = null!;
    //}


    public class CSVScopeDataDTO
    {
        public int appId { get; set; }
        public int levelId { get; set; }
        public List<ScopeDetailsDto> scopes { get; set; } = new();
    }

    public class ScopeDetailsDto
    {
        public string name { get; set; } = null!;
        public string value { get; set; } = null!;
        public List<ParentScopeDto> parents { get; set; } = new();
    }

    public class ParentScopeDto
    {
        public string parentScopeCode { get; set; } = null!;
        public string parentLevelName { get; set; } = null!;
        public long parentLevelId { get; set; } 
    }

    public class ScopeGetDTO : ScopeDto
    {
        public string ScopeCode { get; set; } = null!;
        public int? ParentScopeLevelId { get; set; }
    }
    public class GetAllAndSelectedScopeDTO
    {
        public List<ScopeGetDTO> AllScopes { get; set; } = [];
        public List<ScopeGetDTO> SelectedScopes { get; set; } = [];
    }
    public class ScopeReturnDTO
    {
        public List<ScopesDTO> Scopes { get; set; } = [];
        public long TotalCount { get; set; }
    }
    public class ScopeFetchReturnDTO
    {
        public List<ScopeFetchDTO> Scopes { get; set; } = [];
        public long? TotalCount { get; set; }
    }

    public class ScopeUpdateDTO
    {
        public long ScopeId { get; set; }
        public int LevelId { get; set; }
        public int ParentLevelId { get; set; }
        public int AppId { get; set; }
        public short Status { get; set; }

        public bool IsActive { get; set; }

        [RegularExpression(
       @"[a-zA-Z0-9(),_.\s-]*",
        ErrorMessage = "{0} must contain only alpha numeric  characters, spaces, commas (,), hyphens (-), white space ( ), dot (.), underscore(_) and parentheses ()."
         )]
        public string Name { get; set; } = null!;

        // [RegularExpression(
        //     @"[a-zA-Z0-9]*",
        //     ErrorMessage = "{0} must be alphanumeric characters only"
        // )]
        public string Code { get; set; } = null!;
        public List<ScopeGetDTO> accessedByScopes { get; set; } = [];
        public bool IsGlobal { get; set; }
    }



    

}
