using UserManagement.Models.DTO;

namespace UserManagement.BAL.Interfaces
{
    public interface ITempHrmService
    {
        public Task<HrmsDeatilsDTO> HrmsDeatilsByHremsId(string HrmsID);
    }
}