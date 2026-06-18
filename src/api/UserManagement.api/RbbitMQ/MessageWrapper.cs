namespace UserManagement.RbbitMQ
{
    public class MessageWrapper<T> where T : class
    {
        public string UniqueId { get; set; }
        public T Data { get; set; }
    }
 
}