using Newtonsoft.Json.Linq;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface IScopeService
    {
        public Task<(bool, string)> InsertScopeData(List<ScopeDataInsertDTO> scopes);
        public Task<bool> CreateNewScope(ScopeCreateDTO scope);
        //Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? search);
        public Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(int levelId, string? filter);
        public Task<List<ScopeStructureDTO>> GetScopesByLevelId(int levelId);
        public Task<ScopeReturnDTO> GetScopesByLevelIdForViewOnly(int levelId, int first, int rows, string globalSearch);
        public Task<(string, ScopeFetchReturnDTO)> GetScopeByLevelIdForOtherOffice(int appId, int levelId, int offset, int limit, string search);
        public Task<ScopeFetchReturnDTO> GetScopeByLevelIdForOwnOffice(int appId, int levelId, int offset, int limit, string search);
        public Task<(bool, string)> ImportScopesFromCSV(CSVScopeDataDTO scopes);
        Task<ScopeFetchReturnDTO> GetScopeByLevelIdSuperAdminAndUserAdmin(int levelId, int offset, int limit, string search);
        Task<List<ScopeFetchDTO>> GetScopeByLevelIdForApplicationSuperAdmin(int appId, int levelId);
        Task<GetAllAndSelectedScopeDTO> GetAllAndSelectedScope(int ScopeId, int levelId);
        Task<(string, bool)> UpadateScope(ScopeUpdateDTO scopeUpdateDTO);
        Task<(string, bool)> DeleteScope(int Id);
        Task<ScopeReturnDTO> GetScopeByLevelIdWithPaginationAsync(int levelId, int offset, int limit, string filter, string search);
    }
}
