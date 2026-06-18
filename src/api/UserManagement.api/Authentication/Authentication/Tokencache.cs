//using UserManagement.DAL.Enums;
using UserManagement.Model.Claims;

namespace UserManagement.Authentication
{
    public class Tokencache : ITokencache
    {
        //private static Tokencache instance = new Tokencache();

        private static Dictionary<string, TokenChachedItems> valuePairs = new Dictionary<string, TokenChachedItems>();
        private static string locketstring = string.Empty;


        private readonly IConfiguration _Configuration;
        private readonly ILogger<Tokencache> _logger;
        private readonly int TokenCacheMaxItemCount;
        private readonly int TokenCachEvictionTimeinMint;
        private List<string> orderList = new List<string>();

        public Tokencache(ILogger<Tokencache> logger, IConfiguration Configuration)
        {
            _logger = logger;
            _Configuration = Configuration;
            TokenCachEvictionTimeinMint = int.Parse(_Configuration.GetValue<string>("Auth:TokenCachEvictionTimeinMint", "30"));
            TokenCacheMaxItemCount = int.Parse(_Configuration.GetValue<string>("Auth:TokenCacheMaxItemCount", "400"));
        }
        //private Tokencache()
        //{

        //}

        //public static Tokencache GetInstance()
        //{
        //    return instance;
        //}

        public void PutItem(string token, TokenChachedItems tokenChachedItems)
        {
            lock (locketstring)
            {
                if (TokenCacheMaxItemCount == valuePairs.Count) /*Cache is full and need to remove from last*/
                {
                    var toRemove = orderList[0];
                    valuePairs.Remove(toRemove);
                    orderList.Remove(toRemove);
                }
                orderList.Add(token);
                tokenChachedItems.lastUsed = DateTime.UtcNow;
                valuePairs[token] = tokenChachedItems;
            }
        }

        public TokenChachedItems GetItem(string token)
        {
            lock (locketstring)
            {
                if (!valuePairs.ContainsKey(token))
                {
                    return null;
                }

                //put the key and value to the back of the ordered list
                var tempCachekey = orderList.FirstOrDefault(x => x == token);
                orderList.Remove(tempCachekey);
                orderList.Add(tempCachekey);

                var cachedItem = valuePairs[token];
                cachedItem.lastUsed = DateTime.UtcNow;
                valuePairs[token] = cachedItem;
                return cachedItem;
            }

        }


        public void RemoveItem(string token)
        {
            lock (locketstring)
            {
                if (valuePairs.ContainsKey(token))
                {
                    valuePairs.Remove(token);

                    var tempCachekey = orderList.FirstOrDefault(x => x == token);
                    if (tempCachekey != null)
                        orderList.Remove(tempCachekey);
                }
            }
        }


        public void RemoveExpiredItem()
        {
            lock (locketstring)
            {
                foreach (var item in valuePairs)
                {
                    TimeSpan span = DateTime.UtcNow.Subtract(item.Value.lastUsed);
                    if (span.TotalMinutes >= TokenCachEvictionTimeinMint)
                    {
                        var tempCachekey = orderList.FirstOrDefault(x => x == item.Key);
                        if (tempCachekey != null)
                        {
                            orderList.Remove(tempCachekey);
                            if (valuePairs.ContainsKey(tempCachekey))
                                valuePairs.Remove(tempCachekey);
                        }
                    }

                }

            }
        }

        public void InvalidateUserToken(Guid userId)
        {
            string existingUserId = string.Empty;
            lock (locketstring)
            {
                foreach (var item in valuePairs)
                {
                    try
                    {
                        existingUserId = item.Value.userClaimModel.UserId;
                    }
                    catch
                    {
                        continue;
                    }

                    if (existingUserId == Convert.ToString(userId))
                    {
                        var tempCachekey = orderList.FirstOrDefault(x => x == item.Key);
                        if (tempCachekey != null)
                        {
                            orderList.Remove(tempCachekey);
                            if (valuePairs.ContainsKey(tempCachekey))
                                valuePairs.Remove(tempCachekey);
                        }
                    }
                }

            }
        }
    }

    public class TokenChachedItems
    {
        public DateTime tokenValidTo { get; set; }

        public UserClaimModel userClaimModel { get; set; }

        public DateTime lastUsed { get; set; }
    }
}
