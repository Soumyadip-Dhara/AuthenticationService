using AutoMapper;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services.Master
{
    public class RoleHasPermissionService : IRoleHasPermissionService
    {
        private readonly IMapper _mapper;
        private readonly IRoleHasPermissionRepository _RoleHasPermissionRepository;

        public RoleHasPermissionService(IRoleHasPermissionRepository RoleHasPermissionRepository, IMapper mapper)
        {
            _RoleHasPermissionRepository = RoleHasPermissionRepository;
            _mapper = mapper;
        }

        public async Task<bool> Insert(RoleHasPermissionModel roleHasPermissionModel)
        {
            RoleHasPermission roleHasPermission = _mapper.Map<RoleHasPermission>(roleHasPermissionModel);
            if (_RoleHasPermissionRepository.Add(roleHasPermission))
            {
                _RoleHasPermissionRepository.SaveChangesManaged();
                return true;
            }
            return false;
        }

        public async Task<List<PermissionGetDTO>> PermissionByRoleIds(int roleId)
        {
            List<PermissionGetDTO> result = (List<PermissionGetDTO>)await _RoleHasPermissionRepository.GetSelectedColumnByConditionAsync(entity => entity.RoleId == roleId,
              entity => new PermissionGetDTO
              {
                  Id = entity.PermissionId,
                  Name = entity.Permission.Name
              });
            return result;
        }
    }
}