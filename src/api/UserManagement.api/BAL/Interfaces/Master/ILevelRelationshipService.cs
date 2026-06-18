using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces.Master
{
    public interface ILevelRelationshipService
    {
        public Task<List<GroupItemDTO>> LevelByAccessLevelIds(List<int> accessLevelIds);
    }
}