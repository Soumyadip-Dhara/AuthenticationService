namespace UserManagement.Utils.Interfaces
{
    public interface IRabbitMQPublisherService
    {
        public Task PublishMessage<T>(string queueName, T message, string exchange = "") where T : class;
        //public Task EmailWorker(string queueName);
        //public Task SMSWorker(string queueName);
    }
}
