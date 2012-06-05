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
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Security;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using eStreamChat.Interfaces;
using System.Collections.Generic;

namespace eStreamChat.Classes
{
    internal class RemoteAuthUserProvider : IChatUserProvider
    {
        readonly ICacheProvider cacheProvider;

        public RemoteAuthUserProvider(ICacheProvider cacheProvider)
        {
            this.cacheProvider = cacheProvider;
        }

        private static readonly Random Rand = new Random();

        #region IChatUserProvider Members

        public User GetCurrentlyLoggedUser()
        {
            var href = HttpContext.Current.Items["href"] as string;
            if (string.IsNullOrWhiteSpace(href)) return null;
            var hrefUri = new Uri(href);
            NameValueCollection hrefParams = HttpUtility.ParseQueryString(hrefUri.Query);
            if (hrefParams["timestamp"] != null)
            {
                #region Validate timestamp
                try
                {
                    DateTime authDate;
                    if (!DateTime.TryParseExact(hrefParams["timestamp"], "yyMMddhhmmss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out authDate))
                    {
                        authDate = DateTime.FromFileTimeUtc(Convert.ToInt64(hrefParams["timestamp"]));
                    }
                    if (DateTime.Now.Subtract(authDate) > TimeSpan.FromHours(24))
                    {
                        throw new SecurityException("Timestamp has expired!");
                    }
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new SecurityException("Invalid timestamp!");
                }
                catch (FormatException)
                {
                    throw new SecurityException("Invalid timestamp!");
                }

                var calculatedHash = Miscellaneous.CalculateChatAuthHash(hrefParams["id"] ?? String.Empty,
                    hrefParams["target"] ?? String.Empty, hrefParams["timestamp"]);

                if (hrefParams["hash"] != calculatedHash)
                {
                    throw new SecurityException("Hash is invalid!");
                }
                #endregion

                // Create user object
                var user = new User {DisplayName = hrefParams["name"] ?? hrefParams["id"] ?? "guest_" + Rand.Next(1000)};
                user.Id = hrefParams["id"] ?? Regex.Replace(user.DisplayName, "[\\{\\}\\'\\\"]", String.Empty);
                user.ThumbnailUrl =
                    hrefParams["thumbUrl"] ??
                    (string.Format("http://www.gravatar.com/avatar/{0}.jpg?s=30&d=monsterid",
                                   FormsAuthentication.HashPasswordForStoringInConfigFile(user.Id, "md5")).ToLower());
                cacheProvider.Set("RemoteAuthUserProvider_" + user.Id, user);
                return user;
            }

            return null;
        }

        public User GetUser(string userId)
        {
            return cacheProvider.Get("RemoteAuthUserProvider_" + userId) as User;
        }

        public void IgnoreUser(string userId, string ignoredUser)
        {
            string cacheKey = "IgnoredUsers_" + userId;
            var ignoredUsers = cacheProvider.Get(cacheKey) as IList<string>;

            if (ignoredUsers == null)
                ignoredUsers = new List<string>();

            if (!ignoredUsers.Contains(ignoredUser))
                ignoredUsers.Add(ignoredUser);

            cacheProvider.Set(cacheKey, ignoredUsers);
        }

        public bool IsUserIgnored(string userId, string ignoredUserId)
        {
            string cacheKey = "IgnoredUsers_" + userId;
            var ignoredUsers = cacheProvider.Get(cacheKey) as IList<string>;

            if (ignoredUsers == null || !ignoredUsers.Contains(ignoredUserId))
                return false;

            return true;
        }

        public bool IsChatAdmin(string userId, string chatRoomId)
        {
            return true;
        }

        public string GetLoginUrl(string backURL)
        {
            return HttpUtility.UrlPathEncode(
                String.Format("{0}{2}timestamp={1}&back_url={3}",
                              ConfigurationManager.AppSettings["RemoteAuthUrl"],
                              DateTime.Now.ToFileTimeUtc(),
                              ConfigurationManager.AppSettings["RemoteAuthUrl"].Contains("?") ? "&" : "?",
                              HttpUtility.UrlEncode(backURL)));
            // TODO: Redirect to KB if null
        }

        #endregion
    }
}