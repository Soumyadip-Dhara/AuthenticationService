using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Cors.Infrastructure;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserManagement.DAL.Interfaces.MQueue;
using UserManagement.Helper;
using UserManagement.Models.MQueue;
using UserManagement.RbbitMQ;
using UserManagement.RbbitMQ;
using UserMangement.BAL.Interfaces.MQueue;

namespace UserManagement.BAL.Services.MQueue
{
    public class LogsService : ILogsService
    {
        private readonly IRabbitMQLogsRepository _repo;
        private readonly IRabbitMqService _rabbit;

        public LogsService(IRabbitMQLogsRepository repo, IRabbitMqService rabbit)
        {
            _repo = repo;
            _rabbit = rabbit;
        }

        public async Task<PagedResponse<PublishedLogDTO>> GetPublishedLogsAsync(BaseLogFilterDTO filter)
        {
            return  await _repo.GetPublishedLogsAsync(filter);
        }

        public async Task<PagedResponse<ConsumedLogDTO>> GetConsumedLogsAsync(BaseLogFilterDTO filter)
        {
            return await _repo.GetConsumedLogsAsync(filter);
        }

        public async Task<PagedResponse<FailedConsumeLogDTO>> GetFailedConsumeLogsAsync(BaseLogFilterDTO filter)
        {
            return await _repo.GetFailedConsumeLogsAsync(filter);

        }
        public async Task<PagedResponse<PublishAckLogDTO>> GetPublishAckLogsAsync(BaseLogFilterDTO filter)
        {
            return await _repo.GetPublishAckLogsAsync(filter);

        }
        public async Task<PagedResponse<ConsumeAckLogDTO>> GetConsumeAckLogsAsync(BaseLogFilterDTO filter)
        {
            return await _repo.GetConsumeAckLogsAsync(filter);

        }
        public async Task<List<AckSummaryDTO>> GetAckSummaryAsync(AckSummaryFilterDTO filter)
        {

                return await _repo.GetAckSummaryAsync(
                    filter.FromDate,
                    filter.ToDate,
                    filter.QueueName,
                    filter.Mode
                );
          
        }


        // Currently not in use, can be implemented in future if needed

        //public async Task<APIResponseClass<string>> RetryFailedConsumeAsync(long id)
        //{
        //    var response = new APIResponseClass<string>();

        //    var failed = await _repo.GetFailedById(id);

        //    if (failed == null)
        //        throw new Exception("Not found");

        //    if (failed.ActionStatus == "RESOLVED")
        //        throw new Exception("Already resolved");

        //    await _repo.UpdateStatus(id, "RETRYING");

        //    try
        //    {
        //        await _rabbit.PublishAsync(new RabbitMessage
        //        {
        //            QueueName = failed.QueueName,
        //            ExchangeName = failed.ExchangeName,
        //            RoutingKey = failed.RoutingKey,
        //            MessageBody = failed.MessageBody
        //        });

        //        await _repo.MarkResolved(id);

        //        response.apiResponseStatus = Enum.APIResponseStatus.Success;
        //        response.message = "Retry success";
        //    }
        //    catch (Exception ex)
        //    {
        //        await _repo.UpdateStatus(id, "PENDING");

        //        response.apiResponseStatus = Enum.APIResponseStatus.Error;
        //        response.message = ex.Message;
        //    }

        //    return response;
        //}
    }
}