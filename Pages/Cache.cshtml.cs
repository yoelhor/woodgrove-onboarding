using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using woodgrove_portal.Controllers;

namespace woodgrove_portal.Pages
{

    public class CacheModel : PageModel
    {
        private IMemoryCache _cache;
        public List<string> Items = new List<string>();

        public CacheModel(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void OnGet()
        {
            string domainName = string.Empty;

            if (User.Identity!.IsAuthenticated && @User.Identity?.Name.Split("@").Length == 2)
            {
                domainName = User.Identity?.Name.Split("@")[1];
            }
            else
            {
                return;
            }

            var keys = GetCacheKeys();

            foreach (var key in keys)
            {
                if (_cache.TryGetValue(key, out string cacheValue))
                {
                    try
                    {
                        UsersCache usersCache = UsersCache.Parse(cacheValue);

                        if (string.IsNullOrEmpty(usersCache.UPN) == false && usersCache.UPN.Split("@").Length == 2 && usersCache.UPN.Split("@")[1] == domainName)
                        {
                            Items.Add(cacheValue);
                        }

                    }
                    catch (System.Exception)
                    {

                    }
                }
            }
        }

        private List<string> GetCacheKeys()
        {
            var coherentState = typeof(MemoryCache).GetField("_coherentState", BindingFlags.NonPublic | BindingFlags.Instance);

            var coherentStateValue = coherentState.GetValue(_cache);
            var entriesCollection = coherentStateValue.GetType().GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var entriesCollectionValue = entriesCollection.GetValue(coherentStateValue) as ICollection;

            var keys = new List<string>();
            if (entriesCollectionValue != null)
            {
                foreach (var item in entriesCollectionValue)
                {
                    var methodInfo = item.GetType().GetProperty("Key");
                    var val = methodInfo.GetValue(item);
                    keys.Add(val.ToString());
                }
            }

            return keys;
        }
    }
}
