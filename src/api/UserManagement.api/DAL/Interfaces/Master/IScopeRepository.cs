using UserManagement.DAL.Entities;
using UserManagement.Models.DTO;
using UserManagement.Models.MQueue;

namespace UserManagement.DAL.Interfaces.Master
{
    public interface IScopeRepository : IRepository<ScopeMaster>
    {
        public Task<(bool, string, string, string)> InsertScopeValue(List<ScopeDataInsertDTO> scopes);
        public bool CreateNewScope(ScopeCreateDTO scope);
        public List<ScopeStructureDTO> GetScopesByLevelId(int levelId);
        public Task<int> GetAppScopeId(int scopeId);
        public Task<bool> DeleteApplicationScopesByScopeId(int scopeId, long? updatedBy = null);
        //Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(List<ScopeDataForSearchDTO> data, string? search);
        public Task<List<ScopeFetchDTO>> GetScopesOfAuthenticatedUserByLevelId(List<ScopeDataForSearchDTO> dataForSearch, string? filter);
        public Task<List<ScopeFetchDTO>> GetScopesByScopeIds(List<long> ids);
        public Task<List<ScopeFetchDTO>> GetScopesByScopeIdsForOtherOffice(List<long> ids);
        public Task<(bool, string)> ImportScopesFromCSV(CSVScopeDataDTO scopes, long createdBy);
        public Task<(bool, string)> DeleteScopePartition(string partitionTableName);
        public Task<(bool, string)> UpdatePartitionTable(string oldLevel, string newLevel);
        Task<List<string>> GetParentScopeValuesAsync(string scopeValue, long levelId);
        Task UpsertMasterTreasuryScopeAsync(MasterTreasuryConsumerPayload message);
        Task UpsertMasterDdoScopeAsync(MasterDdoConsumerPayload message);
        Task<List<LevelGetDTO>> GetAllLevelMastersAsync();
        Task<ScopeReturnDTO> GetScopeByLevelIdWithPaginationAsync(int levelId, int offset, int limit, string filter, string search);
        Task<bool> UpdateApplicationScopeStatus(int appId, int scopeId, short status);
    }
}
