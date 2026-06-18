using AutoMapper;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.BAL
{
    public class MigrationService : IMigrationService
    {
        private readonly IMapper _mapper;
        private readonly IMigrationRepository _migrationRepository;

        public MigrationService(IMigrationRepository MigrationRepository, IMapper mapper)
        {
            _migrationRepository = MigrationRepository;
            _mapper = mapper;
        }
        public async Task UploadCsvAsync(IFormFile file,string entityType,Guid jobId)
        {
            await _migrationRepository.CreateJob(jobId, entityType, 5);
            await _migrationRepository.CopyCsvToStaging(file, jobId);
            //await _migrationRepository.UpdateTotalRows(jobId); //curently not required
        }

    }
}