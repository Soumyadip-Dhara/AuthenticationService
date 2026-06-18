using AutoMapper;
using System.Security;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;
using UserManagement.Utils;
using UserManagement.Utils.Interfaces;

namespace UserManagement.BAL.Services.Master
{
    public class PermissionService : IPermissionService
    {
        private readonly IClaimService _claimService;
        private readonly IMapper _mapper;
        private readonly IPermissionRepository _PermissionRepository;
        private readonly IRoleHasPermissionRepository _roleHasPermissionRepository;
        private readonly IRabbitMQPublisherService _rabbitMQPublisherService;

        public PermissionService(IPermissionRepository permissionRepository, IClaimService claimService, IMapper mapper, IRoleHasPermissionRepository roleHasPermissionRepository, IRabbitMQPublisherService rabbitMQPublisherService)
        {
            _PermissionRepository = permissionRepository;
            _roleHasPermissionRepository = roleHasPermissionRepository;
            _rabbitMQPublisherService = rabbitMQPublisherService;
            _claimService = claimService;
            _mapper = mapper;
        }

        public async Task<List<PermissionGetDTO>> GetPermissionByApplicationIdForViewOnly(int applicationId)
        {
            var result = (List<PermissionGetDTO>)await _PermissionRepository.GetSelectedColumnByConditionAsync(
                entity => entity.ApplicationId == applicationId,
                entity => new PermissionGetDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Roles = entity.RoleHasPermissions.Where(r => r.PermissionId == entity.Id)
                        .Select(r => r.Role.Title)
                        .ToList(),
                    ApplicationName = entity.Application.Title,
                    CreatedBy = entity.CreatedByNavigation.Name
                });

            return result;
        }
        public async Task<List<PermissionGetDTO>> GetPermissionByApplicationId(int applicationId)
        {
            var result = (List<PermissionGetDTO>)await _PermissionRepository.GetSelectedColumnByConditionAsync(
                entity => entity.ApplicationId == applicationId && !entity.Name.ToLower().Equals("can-manage-master"),
                entity => new PermissionGetDTO
                {
                    Id = entity.Id,
                    Name = entity.Name
                });

            return result;
        }
        public async Task<List<PermissionGetDTO>> GetPermissionsNotAssignedToARoleByRoleId(int roleId, int applicationId)
        {
            var permissionIds = (List<int>)await _roleHasPermissionRepository.GetSelectedColumnByConditionAsync(
                entity => entity.RoleId == roleId,
                entity => entity.PermissionId);
            var result = (List<PermissionGetDTO>)await _PermissionRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationId == applicationId && !permissionIds.Contains(e.Id),
                e => new PermissionGetDTO
                {
                    Id = e.Id,
                    Name = e.Name,
                });
            return result;
        }

        //new
        public async Task<bool> InsertPermission(List<PermissionSetDTO> permissionSetDTO)
        {
            var permissions = _mapper.Map<List<Permission>>(permissionSetDTO);
            bool anyInserted = false;
            var permissionNames = new List<string>();
            foreach (var permission in permissions)
            {
                var res = await _PermissionRepository.GetSelectedColumnByConditionAsync(p => p.Name.ToLower() == permission.Name.ToLower() && p.ApplicationId == permission.ApplicationId,
                    e => e.Id);
                if (res.Count > 0)
                {
                    continue;
                }
                permission.CreatedBy = _claimService.GetUserId();
                if (_PermissionRepository.Add(permission))
                {
                    anyInserted = true;
                    permissionNames.Add(permission.Name);
                }
                else
                {
                    return false;
                }
            }

            if (anyInserted)
            {
                _PermissionRepository.SaveChangesManaged();
                var responsePermissions = await _PermissionRepository.GetSelectedColumnByConditionAsync(
                    e => permissionNames.Contains(e.Name), 
                    e => new
                    {
                        Id = e.Id,
                        Name = e.Name,
                        AppName = e.Application.Title,
                    });
                await _rabbitMQPublisherService.PublishMessage($"{responsePermissions.FirstOrDefault().AppName}-GETPERMISSIONS", responsePermissions);
            }

            return true;
        }

        public async Task<(string, bool)> UpdatePermission(PermissionUpdateDTO permissionUpdateDTO)
        {
            var permission = await _PermissionRepository.GetSingleAysnc(p => p.Id == permissionUpdateDTO.Id);
              if (permission !=  null)
              {
                  permission.UpdatedBy = _claimService.GetUserId();
                  permission.UpdatedAt = DateTime.Now;
                  permission.Name = permissionUpdateDTO.Name;
                  permission.Id = permissionUpdateDTO.Id;
                  if (_PermissionRepository.Update(permission))
                  {
                      _PermissionRepository.SaveChangesManaged();
                      return ("Update Succesful", true);
                  }
                  else
                  {
                    return ("Update Unsuccesful", false);
                  }
              }
            else
            {
                return (("Item Dose Not Exist!!!", false));
            }
        }
        public async Task<(string, bool)> DeletePermission(int Id)
        {
            var res = _PermissionRepository.GetSingle(p => p.Id == Id);
            if (res != null)
            {
                if( _PermissionRepository.Delete(res))
                {
                    _PermissionRepository.SaveChangesManaged();
                    return ("Permission Deleted Succesful", true);
                }
                else
                {
                    return ("Permission Deleted Unsuccesful", false);
                }
            }
            else
            {
                return (("Item Dose Not Exist!!!", false));
            }
        }
    }
}
