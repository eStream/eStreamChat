/* This file is part of eStreamChat.
 * 
 * eStreamChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version. 
 * 
 * eStreamChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with eStreamChat. If not, see <http://www.gnu.org/licenses/>.
 */
using System.Web;
using System.Web.Caching;
using eStreamChat.Interfaces;

namespace eStreamChat.Classes
{
    public class MemoryCacheProvider : ICacheProvider
    {
        #region ICacheProvider Members

        public void Set(string key, object value)
        {
            HttpRuntime.Cache.Insert(key, value, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                                             CacheItemPriority.NotRemovable, null);
        }

        public object Get(string key)
        {
            return HttpRuntime.Cache.Get(key);
        }

        public void Remove(string key)
        {
            HttpRuntime.Cache.Remove(key);
        }

        #endregion
    }
}