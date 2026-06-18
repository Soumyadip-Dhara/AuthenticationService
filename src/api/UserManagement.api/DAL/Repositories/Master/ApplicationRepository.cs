using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using System.Data;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.DAL.Repositories.Master
{
    public class ApplicationRepository : Repository<Application, UserManagementDBContext>, IApplicationRepository
    {
        private readonly UserManagementDBContext _context;

        public ApplicationRepository(UserManagementDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(bool, string)> CreateApplication(ApplicationCreateDTO application, string photoPath, long createdBy, string key)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("application_name", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = application.Title },
                new NpgsqlParameter("url", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = application.Url },
                new NpgsqlParameter("key", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = key },
                new NpgsqlParameter("logo_url", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = photoPath },
                new NpgsqlParameter("email", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = application.Email },
                new NpgsqlParameter("mobile", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = application.Mobile },
                new NpgsqlParameter("created_by", NpgsqlTypes.NpgsqlDbType.Bigint) { Value = createdBy },
                new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput,Value = false },
                new NpgsqlParameter("out_message", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = String.Empty }
            };

            var commandText = "CALL master.insert_application(" +
                             "@application_name, @url, @key, @logo_url, @email, @mobile," +
            "@created_by, @is_done, @out_message)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[7].Value;
            string responseMessage = parameters[8].Value as string;

            return (isSuccess, responseMessage);
        }

        public async Task<(string, bool)> DeleteApplication(int AppId)
        {
            var parameters = new[]
            {
                new NpgsqlParameter("app_id", NpgsqlTypes.NpgsqlDbType.Integer) { Value = AppId },
                new NpgsqlParameter("is_done", NpgsqlTypes.NpgsqlDbType.Boolean) { Direction = ParameterDirection.InputOutput,Value = false },
                new NpgsqlParameter("response", NpgsqlTypes.NpgsqlDbType.Text) { Direction = ParameterDirection.InputOutput, Value = String.Empty }
            };

            var commandText = "CALL master.delete_application(" +
                             "@app_id, @is_done, @response)";

            await _context.Database.ExecuteSqlRawAsync(commandText, parameters);

            bool isSuccess = (bool)parameters[1].Value;
            string responseMessage = parameters[2].Value as string;

            return (responseMessage, isSuccess);
        }

    }
}