using System;
using System.Threading.Tasks;
using StackExchange.Redis;
using TicketOnline.Data.Cloud;

namespace TicketOnline.Data
{
    public class Cache
    {
        public static ConnectionMultiplexer Connection => lazyConnection.Value;

        public async Task<T> GetFromCacheAsync<T>(string key, Func<Task<T>> missedCacheCall)
        {
            return await GetFromCacheAsync<T>(key, missedCacheCall, TimeSpan.FromMinutes((5)));
        }

        public async Task<T> GetFromCacheAsync<T>(string key, Func<Task<T>> missedCacheCall, TimeSpan timeToLive)
        {
            IDatabase cache = Connection.GetDatabase();
            var obj = await cache.GetAsync<T>(key);
            if (obj == null)
            {
                obj = await missedCacheCall();
                if (obj != null)
                {
                    cache.Set(key, obj);
                }
            }
            return obj;
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            ConfigurationOptions options = new ConfigurationOptions();
            options.EndPoints.Add("rredis.redis.cache.windows.net");
            options.Ssl = true;
            options.Password = "XFORrBS2dGQhgPKORNAfE1Nj80Pp8AIow2eQdtqLTk8=";
            options.ConnectTimeout = 1000;
            options.SyncTimeout = 2500;
            return ConnectionMultiplexer.Connect(options);
        });

        public void InvalidateCache(string key)
        {
            IDatabase cache = Connection.GetDatabase();
            cache.KeyDelete(key);
        }
    }
}
