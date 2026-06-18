using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BAL.Interfaces;
using UserManagement.Filters;
using UserManagement.Helper;
using UserManagement.Models.DTO;

namespace UserManagement.Controllers
{
    //[Authorize("Super Admin")]
    //[Authorize("Super Admin,User Admin,Level Admin,IFMS USER,Module Admin")]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class NoticeController : Controller
    {
        private readonly INoticeService _noticeService;
        private readonly IClaimService _claimService;
        public NoticeController(INoticeService noticeService, IClaimService claimService)
        {
            _noticeService = noticeService;
            _claimService = claimService;
        }

        [HttpPost("GetAllNotices")]
        public async Task<APIResponseClass<List<NoticeDTO>>> GetAllNotices(
    [FromBody] NoticeFilterDTO noticeFilterDTO)
        {
            APIResponseClass<List<NoticeDTO>> response = new();

            try
            {
                var res = await _noticeService.GetAllNoticesAsync(noticeFilterDTO);

                if (res == null || !res.Any())
                {
                    response.message = "No Notices Found";
                }
                else
                {
                    response.message = "Notices Fetched Successfully";
                }

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.result = res;
                return response;
            }
            catch (Exception e)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = "Something went wrong " + e.Message;
                return response;
            }
        }

        [HttpPost("CreateNotice")]
        public async Task<APIResponseClass<long>> CreateNotice(
    [FromBody] CreateNoticeDTO createNoticeDTO)
        {
            APIResponseClass<long> response = new();

            try
            {
                // Example: get user id from token/claim
                long createdBy = _claimService.GetUserId();

                var noticeId = await _noticeService.CreateNoticeAsync(createNoticeDTO, createdBy);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Notice created successfully";
                response.result = noticeId;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
                return response;
            }
        }
        [HttpPost("UpdateNotice")]
        public async Task<APIResponseClass<bool>> UpdateNotice(
    [FromBody] UpdateNoticeDTO updateNoticeDTO)
        {
            APIResponseClass<bool> response = new();

            try
            {
                long updatedBy = Convert.ToInt64(User.FindFirst("user_id")?.Value);

                await _noticeService.UpdateNoticeAsync(updateNoticeDTO, updatedBy);

                response.apiResponseStatus = Enum.APIResponseStatus.Success;
                response.message = "Notice updated successfully";
                response.result = true;
                return response;
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = ex.Message;
                response.result = false;
                return response;
            }
        }







    }

}