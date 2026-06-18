using AutoMapper;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services.Master
{
    public class LevelRelationshipService : ILevelRelationshipService
    {
        private readonly ILevelRelationshipRepository _LevelRelationshipRepository;
        private readonly IMapper _mapper;

        public LevelRelationshipService(ILevelRelationshipRepository LevelRelationshipRepository, IMapper mapper)
        {
            _LevelRelationshipRepository = LevelRelationshipRepository;
            _mapper = mapper;
        }

        public async Task<List<GroupItemDTO>> LevelByAccessLevelIds(List<int> accessLevelIds)
        {
            List<GroupItemDTO> result = (List<GroupItemDTO>)await _LevelRelationshipRepository.GetSelectedColumnByConditionAsync(entity => accessLevelIds.Contains(entity.AccessLevelId),
               entity => new GroupItemDTO
               {
                   Label = entity.Level.App != null ? entity.Level.App.Title : string.Empty,
                   Items = new List<ItemDTO>
                   {
                       new ItemDTO{Label = entity.Level.Level != null ? entity.Level.Level.LevelName : string.Empty, Value=entity.LevelId.ToString(), ParentId=entity.Level.AppId ?? 0}
                   }
               }
            );
            List<GroupItemDTO> groupedResult = new();
            if (result != null)
            {
                groupedResult = result.GroupBy(r => r.Label)
                              .Select(group => new GroupItemDTO
                              {
                                  Label = group.Key,
                                  Items = group.SelectMany(r => r.Items).ToList()
                              })
                              .ToList();
            }

            return groupedResult;
        }
    }
}