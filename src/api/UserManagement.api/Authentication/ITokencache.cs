namespace UserManagement.api.Authentication
{
    public interface ITokencache
    {
        public void PutItem(string token, TokenChachedItems tokenChachedItems);
        public TokenChachedItems GetItem(string token);

        public void RemoveItem(string token);

        public void RemoveExpiredItem();
        public void InvalidateUserToken(Guid userId);
    }
}
