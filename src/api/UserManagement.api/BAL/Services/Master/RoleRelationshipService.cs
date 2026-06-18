using AutoMapper;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services.Master
{
    public class RoleRelationshipService : IRoleRelationshipService
    {
        private readonly IMapper _mapper;
        private readonly IRoleRelationshipRepository _RoleRelationshipRepository;

        public RoleRelationshipService(IRoleRelationshipRepository RoleRelationshipRepository, IMapper mapper)
        {
            _RoleRelationshipRepository = RoleRelationshipRepository;
            _mapper = mapper;
        }

        public async Task<List<GroupItemDTO>> RolesByAccessRoleIds(List<int> accessRoleIds)
        {
            List<GroupItemDTO> result = (List<GroupItemDTO>)await _RoleRelationshipRepository.GetSelectedColumnByConditionAsync(entity => accessRoleIds.Contains(entity.AccessRoleId),
               entity => new GroupItemDTO
               {
                   Label = entity.Role.Application.Title,
                   Items = new List<ItemDTO>
                   {
                       new ItemDTO{Label=entity.Role.Title,Value=entity.RoleId.ToString(),ParentId=entity.Role.ApplicationId}
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

        public async Task<bool> Insert(RoleRelationshipModel roleRelationshipSetDTO)
        {
            RoleRelationship roleRelationship = _mapper.Map<RoleRelationship>(roleRelationshipSetDTO);
            if (_RoleRelationshipRepository.Add(roleRelationship))
            {
                _RoleRelationshipRepository.SaveChangesManaged();
                return true;
            }
            return false;
        }
    }
}