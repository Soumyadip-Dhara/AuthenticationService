using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UserManagement.Models.DTO;
using UserManagement.Models.DTO.Pagination;
using UserManagement.DAL.Interfaces;


namespace UserManagement.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly UserManagementDBContext _userManagementDBContext;

        public UserRepository(UserManagementDBContext context)
        {
            _userManagementDBContext = context;
        }

        public async Task<PaginatedResult<UserDetailsDTO>> FetchUserList(QueryParameters payload, string userRoles, long userId)
        {
            try
            {
                var res = new PaginatedResult<UserDetailsDTO>
                {
                    PageNumber = payload.PageNumber,
                    PageSize = payload.PageSize
                };

                int pFirst = (payload.PageNumber - 1) * payload.PageSize;
                int pRows = payload.PageSize;

                var parameters = new[]
                {
                    new NpgsqlParameter("p_first", NpgsqlTypes.NpgsqlDbType.Integer) { Value = pFirst },
                    new NpgsqlParameter("p_rows", NpgsqlTypes.NpgsqlDbType.Integer) { Value = pRows },
                    new NpgsqlParameter("user_role", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = userRoles },
                    new NpgsqlParameter("user_id", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = userId },
                    new NpgsqlParameter("filters", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = payload.Filters != null && payload.Filters.Count > 0 ? JsonSerializer.Serialize(payload.Filters) : DBNull.Value },
                    new NpgsqlParameter("scope_value", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = DBNull.Value },
                    new NpgsqlParameter("global_filter", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = DBNull.Value },
                    new NpgsqlParameter("user_details", NpgsqlTypes.NpgsqlDbType.Jsonb) { Direction = ParameterDirection.InputOutput, Value = DBNull.Value },
                    new NpgsqlParameter("total_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 },
                    new NpgsqlParameter("active_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 },
                    new NpgsqlParameter("inactive_users_count", NpgsqlTypes.NpgsqlDbType.Bigint) { Direction = ParameterDirection.InputOutput, Value = 0 }
                };

                var commandText = @"CALL ""user"".get_users(@p_first, @p_rows, @user_role, @user_id, @filters, @scope_value, @global_filter, @user_details, @total_users_count, @active_users_count, @inactive_users_count)";

                await _userManagementDBContext.Database.ExecuteSqlRawAsync(commandText, parameters);

                var userDetailsJson = parameters[7].Value;
                var userDetails = userDetailsJson == DBNull.Value || string.IsNullOrEmpty(userDetailsJson?.ToString())
                    ? new List<UserDetailsDTOForDeserialize>()
                    : JsonSerializer.Deserialize<List<UserDetailsDTOForDeserialize>>(userDetailsJson!.ToString()!);

                res.TotalCount = Convert.ToInt32(parameters[8].Value);
                if (res.PageSize > 0)
                {
                    res.TotalPages = (int)Math.Ceiling(res.TotalCount / (double)res.PageSize);
                }

                if (userDetails != null)
                {
                    res.Data = userDetails.Select(u => new UserDetailsDTO
                    {
                        userId = u.id,
                        userName = u.userName,
                        hrmsId = u.hrmsId,
                        name = u.name,
                        designation = u.designation,
                        mobile = u.mobileNumber,
                        email = u.email,
                        active = u.isActive,
                        blocked = u.isBlocked,
                        createdAt = u.createdAt?.ToString("dd-MM-yyyy"),
                    }).ToList();
                }

                return res;
            }
            catch (Exception ex)
            {
                // In a real application, you might want to log the exception here
                Console.WriteLine($"Error fetching user list: {ex.Message}");
                return new PaginatedResult<UserDetailsDTO>();
            }
        }
    }
}
