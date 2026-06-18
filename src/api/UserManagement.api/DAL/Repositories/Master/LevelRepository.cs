using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Text.Json;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories.Master
{
    public class LevelRepository : Repository<ApplicationLevel, UserManagementDBContext>, ILevelRepository
    {
        private readonly IClaimService _claimService;
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly IScopeRepository _scopeRepository;

        public LevelRepository(IConfiguration config, UserManagementDBContext context, IClaimService claimService, IScopeRepository scopeRepository) : base(context)
        {
            _config = config;
            _context = context;
            _claimService = claimService;
            _scopeRepository = scopeRepository;
        }

        public async Task<(bool, string, string)> CreateLevel(List<LevelPayloadDTO> levels)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("levels", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = levels
                },
                new NpgsqlParameter("created_by", NpgsqlTypes.NpgsqlDbType.Bigint)
                {
                    Value = _claimService.GetUserId()
                },
                new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = false
                },
                new NpgsqlParameter("res_text", NpgsqlTypes.NpgsqlDbType.Text)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = String.Empty
                },
                new NpgsqlParameter("level_data", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Direction = ParameterDirection.InputOutput,
                    Value = new[] {
                        new { id = "id", title = "title" }
                    }
                }
            };


            var commandText = "CALL master.insert_level(@levels, @created_by, @is_done, @res_text, @level_data)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[2].Value;
            string responseMessage = parameters[3].Value as string;
            var level_data = parameters[4].Value as string;

            return (isSuccess, responseMessage, level_data);
        }

    }


}
