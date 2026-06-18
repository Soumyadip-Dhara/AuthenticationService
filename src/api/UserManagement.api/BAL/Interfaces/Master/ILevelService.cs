using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface ILevelService
    {
        public Task<List<LevelGetDTO>> GetLevelByApplication(int applicationId);
        public Task<List<LevelGetDTO>> GetLevelByApplicationIdForSuperAdminAndUserAdmin(int applicationId);
        public Task<List<LevelGetDTO>> GetLevelByApplicationForSuperAdmin(int applicationId);
        public Task<List<LevelGetDTO>> GetLevelByApplicationForApplicationUserAdmin(int applicationId);
        public Task<List<LevelGetDTO>> GetLevelByApplicationOtherRoleId(int applicationId, int? levelId);
        public Task<int> GetLevelCodeById(int levelId);
        public Task<LevelGetDTO> GetLevelById(int levelId);
        public Task<List<GroupItemDTO>> LevelByApplications(List<int> applicationIds);
        public Task<(bool, string)> LevelInsert(List<LevelPayloadDTO> levels);

        //own - other
        public Task<List<LevelGetDTO>> GetLevelsByApplicationIdRoleIdForUMOwnOffice(int applicationId, int umRoleId);
        public Task<List<LevelGetDTO>> GetLevelsForOtherOffice(int applicationId);
        public Task<List<LevelFetchDTO>> GetOwnLevels(int applicationId);
        public Task<List<LevelGetDTO>> GetLevelsForOwnOffice(int applicationId);

        public Task<List<LevelFetchDTO>> GetParentsOfLevelsForScopeParentEntry(int levelid);
        public Task<AcessLevelAndAllLevel> GetLevelAccesByAppId(int AppId, int LevelId);
        public Task<(string, bool)> UpdateLevel(LevelUpdateDTO levelUpdateDTO);
        public Task<(string, bool)> DeleteLevel(int Id);
        public Task<List<LevelGetDTO>> GetLevelsByApplicationIdForDropdown(int applicationId);
        public Task<List<LevelGetDTO>> GetGlobalLevels(int applicationId);
        public Task<FetchRoleDataByLevel> GetAllowedRolesForSpecificLevel(int LevelId, int AppId);
        public Task<List<LevelGetDTO>> GetAllLevelsFromLevelMaster();
    }
}
