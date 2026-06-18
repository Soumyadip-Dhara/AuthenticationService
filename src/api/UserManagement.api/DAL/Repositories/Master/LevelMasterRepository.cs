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
    public class LevelMasterRepository : Repository<LevelMaster, UserManagementDBContext>, ILevelMasterRepository
    {
        private readonly IClaimService _claimService;
        private readonly IConfiguration _config;
        private readonly UserManagementDBContext _context;
        private readonly IScopeRepository _scopeRepository;

        public LevelMasterRepository(IConfiguration config, UserManagementDBContext context, IClaimService claimService, IScopeRepository scopeRepository) : base(context)
        {
            _config = config;
            _context = context;
            _claimService = claimService;
            _scopeRepository = scopeRepository;
        }

        







    }


}
