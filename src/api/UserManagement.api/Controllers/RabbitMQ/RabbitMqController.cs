using Microsoft.AspNetCore.Mvc;
using UserManagement.Helper;
using UserManagement.Models.MQueue;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class RabbitmqlogsController : Controller
    {
        private readonly ILogsService _logsService;

        public RabbitmqlogsController(ILogsService logsService)
        {
            _logsService = logsService;
        }

        [HttpPost("published-logs")]
        public async Task<APIResponseClass<PagdResponse<PublishedLogDTO>>> GetPublished([FromBody] BaseLogFilterDTO filter)
        {
            var response = new APIResponseClass<PagedResponse<PublishedLogDTO>>();

            try
            {
                var result = await _logsService.GetPublishedLogsAsync(filter);

                if (result != null && result.Data.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Published logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No published logs found.";
                    response.result = result ?? new PagedResponse<PublishedLogDTO>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new PagedResponse<PublishedLogDTO>();
            }

            return response;
        }

        [HttpPost("consumed-logs")]
        public async Task<APIResponseClass<PagedResponse<ConsumedLogDTO>>> GetConsumed([FromBody] BaseLogFilterDTO filter)
        {
            var response = new APIResponseClass<PagedResponse<ConsumedLogDTO>>();

            try
            {
                var result = await _logsService.GetConsumedLogsAsync(filter);

                if (result != null && result.Data.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Consumed logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No consumed logs found.";
                    response.result = result ?? new PagedResponse<ConsumedLogDTO>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new PagedResponse<ConsumedLogDTO>();
            }

            return response;
        }

        [HttpPost("consume-failed-logs")]
        public async Task<APIResponseClass<PagedResponse<FailedConsumeLogDTO>>> GetFailed([FromBody] BaseLogFilterDTO filter)
        {
            var response = new APIResponseClass<PagedResponse<FailedConsumeLogDTO>>();

            try
            {
                var result = await _logsService.GetFailedConsumeLogsAsync(filter);

                if (result != null && result.Data.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Failed consume logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No failed consume logs found.";
                    response.result = result ?? new PagedResponse<FailedConsumeLogDTO>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new PagedResponse<FailedConsumeLogDTO>();
            }

            return response;
        }
        [HttpPost("publish-ack-logs")]
        public async Task<APIResponseClass<PagedResponse<PublishAckLogDTO>>> GetPublishAck([FromBody] BaseLogFilterDTO filter)
        {
            var response = new APIResponseClass<PagedResponse<PublishAckLogDTO>>();

            try
            {
                var result = await _logsService.GetPublishAckLogsAsync(filter);

                if (result != null && result.Data.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Failed consume logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No failed consume logs found.";
                    response.result = result ?? new PagedResponse<PublishAckLogDTO>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new PagedResponse<PublishAckLogDTO>();
            }

            return response;
        }

        [HttpPost("consume-ack-logs")]
        public async Task<APIResponseClass<PagedResponse<ConsumeAckLogDTO>>> GetConsumeAck([FromBody] BaseLogFilterDTO filter)
        {
            var response = new APIResponseClass<PagedResponse<ConsumeAckLogDTO>>();

            try
            {
                var result = await _logsService.GetConsumeAckLogsAsync(filter);

                if (result != null && result.Data.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Failed consume logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No failed consume logs found.";
                    response.result = result ?? new PagedResponse<ConsumeAckLogDTO>();
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new PagedResponse<ConsumeAckLogDTO>();
            }

            return response;
        }

        [HttpPost("ack-summary")]
        public async Task<APIResponseClass<List<AckSummaryDTO>>> GetAckSummary([FromBody] AckSummaryFilterDTO filter)
        {
            var response = new APIResponseClass<List<AckSummaryDTO>>();

            try
            {
                var result = await _logsService.GetAckSummaryAsync(filter);

                if (result != null && result.Any())
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "Failed consume logs fetched successfully.";
                    response.result = result;
                }
                else
                {
                    response.apiResponseStatus = Enum.APIResponseStatus.Success;
                    response.message = "No failed consume logs found.";
                    response.result = result;
                }
            }
            catch (Exception ex)
            {
                response.apiResponseStatus = Enum.APIResponseStatus.Error;
                response.message = $"Exception: {ex.Message}";
                response.result = new List<AckSummaryDTO>();
            }

            return response;
        }
    }
}
