using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace YouTube.Providers
{
    public class HttpCacheProvider
    {
        public T GetItem<T>(string cacheKey)
            where T : class
        {
            return HttpContext.Current.Cache.Get(cacheKey) as T;
        }

        public void InsertItem<T>(string cacheKey, T item, TimeSpan duration)
        {
            HttpContext.Current.Cache.Insert(cacheKey, item, null, DateTime.UtcNow.Add(duration), Cache.NoSlidingExpiration);
        }

        public void DeleteItem(string cacheKey)
        {
            HttpContext.Current.Cache.Remove(cacheKey);
        }
    }
}
