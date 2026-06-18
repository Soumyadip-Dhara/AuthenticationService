using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories.Master
{
    public class RoleRepository : Repository<Entities.Role, UserManagementDBContext>, IRoleRepository
    {
        private readonly UserManagementDBContext _context;
        private readonly IClaimService _claimService;

        public RoleRepository(UserManagementDBContext context, IClaimService claimService) : base(context)
        {
            _context = context;
            _claimService = claimService;
        }
        public async Task<List<RoleFetchDTO>> GetRolesOfAuthenticatedUserByApplicationId(long userId, int applicationId)
        {
            var userHasApplicationIds = await _context.UserHasApplications.Where(a => a.UserId == userId && a.AppId == applicationId).Select(e => e.Id).ToListAsync();
            var res = await _context.UserApplicationHasUserRoles.Where(e => userHasApplicationIds.Contains(e.UserHasAppId)).Select(e => new RoleFetchDTO
            {
                RoleId = e.Id,
                RoleName = e.Role.Title,
                OriginalId = e.RoleId
            }).ToListAsync();
            return res;
        }

        public async Task<(bool, string, int)> CreateRole(InsertRoleDTO insertRoleDTO)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("role_name", NpgsqlTypes.NpgsqlDbType.Varchar)
                {
                    Value = insertRoleDTO.Name
                },
                new NpgsqlParameter("application_id", NpgsqlTypes.NpgsqlDbType.Smallint)
                {
                    Value = insertRoleDTO.ApplicationId
                },
                new NpgsqlParameter("permissions", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = insertRoleDTO.Permissions
                },
                new NpgsqlParameter("visible_to_roles", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = insertRoleDTO.VisibleToRoles
                },
                new NpgsqlParameter("created_by", NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    Value = _claimService.GetUserId()
                },
                new NpgsqlParameter("res", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                },
                new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = String.Empty
                },
                new NpgsqlParameter("_role_id", NpgsqlTypes.NpgsqlDbType.Integer)
                {
                    Value = -1
                }
            };

            var commandText = "CALL master.insert_role(" +
                              "@role_name, @application_id, @permissions, @visible_to_roles, " +
                              "@created_by, @res, @response, @_role_id)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[5].Value;
            string responseMessage = parameters[6].Value as string;
            int roleId = (int)parameters[7].Value;
            return (isSuccess, responseMessage, roleId);
        }
        public async Task<(string, bool)> UpdateRole(RoleUpdateDTO roleUpdateDTO)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("get_role_id", NpgsqlTypes.NpgsqlDbType.Smallint)
                {
                    Value = roleUpdateDTO.Id
                },
                new NpgsqlParameter("role_name", NpgsqlTypes.NpgsqlDbType.Varchar)
                {
                    Value = roleUpdateDTO.Name
                },
                new NpgsqlParameter("permissions", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = roleUpdateDTO.Permissions
                },
                new NpgsqlParameter("roles", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = roleUpdateDTO.Roles
                },
                new NpgsqlParameter("in_updated_by", NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    Value = _claimService.GetUserId()
                },
                new NpgsqlParameter("res", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                },
                new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = String.Empty
                },
                new NpgsqlParameter("is_operational_role", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Value = roleUpdateDTO.IsOperational
                }
            };

            var commandText = "CALL master.role_update(" +
                              "@get_role_id, @role_name, @permissions, @roles, " +
                              "@in_updated_by, @res, @response, @is_operational_role)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[5].Value;
            string responseMessage = parameters[6].Value as string;
            return (responseMessage, isSuccess);
        }
    }
}