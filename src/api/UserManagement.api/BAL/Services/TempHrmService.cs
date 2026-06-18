using AutoMapper;
using UserManagement.BAL.Interfaces;
using UserManagement.DAL.Interfaces;
using UserManagement.Models.DTO;

namespace UserManagement.BAL
{
    public class TempHrmService : ITempHrmService
    {
        private readonly IMapper _mapper;
        private readonly ITempHrmRepository _TempHrmRepository;

        public TempHrmService(ITempHrmRepository TempHrmRepository, IMapper mapper)
        {
            _TempHrmRepository = TempHrmRepository;
            _mapper = mapper;
        }

        public async Task<HrmsDeatilsDTO> HrmsDeatilsByHremsId(string HrmsID)
        {
            HrmsDeatilsDTO hrmsDeatils = await _TempHrmRepository.GetSingleSelectedColumnByConditionAsync(entity => entity.HrmsId == HrmsID,
                entity => new HrmsDeatilsDTO
                {
                    UniqueId = entity.HrmsId,
                    Name = entity.Name,
                    Designation = entity.Designation,
                    Email = entity.Email,
                    MobileNumber = entity.Mobile
                }
            );
            return hrmsDeatils;
        }
    }
}