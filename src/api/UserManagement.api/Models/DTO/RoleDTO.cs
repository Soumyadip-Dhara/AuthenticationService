using System.ComponentModel.DataAnnotations;

namespace UserManagement.Models.DTO
{
    public class RoleGetDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string ApplicationName { get; set; }
        public List<string>? permissions { get; set; }
        public List<string>? VisibleTo { get; set; }
        public string CreatedBy { get; set; }
        public bool IsOperational { get; set; }
    }
    public class RoleByApplicationIdsDTO
    {
        public List<int> applicationIds { get; set; }
    }

    public class RoleData
    {
        public string Name { get; set; }
        public int ApplicationId { get; set; }
    }

    public class PermissionDTO
    {
        public int Id { get; set; }
    }

    public class VisibleToRoleDTO
    {
        public int Id { get; set; }
    }

    public class RoleSetDTO
    {
        public RoleData Role { get; set; } = null!;
        public List<PermissionDTO> Permissions { get; set; } = null!;
        public List<VisibleToRoleDTO>? VisibleToRoles { get; set; }
    }

    public class RoleFetchDTO
    {
        public long RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public long OriginalId { get; set; }

    }
    public class RoleSelectedDataFetchDTO
    {
        public List<LevelFetchDTO> LevelFetch { get; set; } = null!;
        public List<string> PermissionNames { get; set; } = null!;

    }
    public class Permissions
    {
        public int Id { get; set; }
    }

    public class InsertRoleDTO
    {
        [RegularExpression(
            @"[a-zA-Z-_]*",
            ErrorMessage = "{0} must be alphabetic characters only, can include hyphens(-)."
        )]
        public string? Name { get; set; }
        public int ApplicationId { get; set; }
        public List<Permissions>? Permissions { get; set; }
        public List<VisibleToRoleDTO>? VisibleToRoles { get; set; }
    }

    public class SelectedPermissionDTO
    {
        public int Id { get; set; }

        [RegularExpression(
            @"[a-zA-Z\s-_]*",
            ErrorMessage = "{0} must be alphabetic characters only, can include spaces, _ and hyphens(-)."
        )]
        public string Name { get; set; } = null!;
    }
    public class AllPermissionDTO : SelectedPermissionDTO
    {

    }
    public class PermissionAllAndPreselectedDTO
    {
        public List<SelectedPermissionDTO>? SelectedPermission { get; set; }
        public List<AllPermissionDTO>? AllPermission { get; set; }
    }
    public class AllRolesDTO : SelectedPermissionDTO
    {

    }
    public class SelectedRolesDTO : SelectedPermissionDTO
    {

    }
    public class AllAndSelectedRoles
    {
        public List<AllRolesDTO>? AllRoles { get; set; }
        public List<SelectedRolesDTO>? SelectedRoles { get; set; }
    }

    public class RoleSelectionUsingAppIdDTO
    {
        public int RoleId { get; set; }
        public int Appid { get; set; }
    }
    public class RoleUpdateDTO : SelectedPermissionDTO
    {
        public List<SelectedPermissionDTO>? Permissions { get; set; }
        public List<SelectedRolesDTO>? Roles { get; set; }
        public bool IsOperational {get; set;}

    }
}
