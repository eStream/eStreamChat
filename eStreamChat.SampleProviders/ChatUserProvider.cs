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
using System.Collections.Generic;
using System.Diagnostics;
using eStreamChat.Interfaces;

namespace eStreamChat.SampleProviders
{
    /// <summary>
    /// This is a sample chat user provider. It logs in users with random guest usernames
    /// </summary>
    public class ChatUserProvider : IChatUserProvider
    {
        #region IUserProvider Members

        private static readonly Dictionary<string, User> users = new Dictionary<string, User>();
        private static readonly Random rand = new Random();

        public User GetCurrentlyLoggedUser()
        {
            var user = new User {DisplayName = "guest_" + rand.Next(1000), Id = Guid.NewGuid().ToString()};
            user.ThumbnailUrl = string.Format("http://www.gravatar.com/avatar/{0}.jpg?s=30&d=monsterid", 
                System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(user.Id, "md5")).ToLower();
            if (!users.ContainsKey(user.Id)) users.Add(user.Id, user);

            Debug.WriteLine("GetCurrentlyLoggedUser() called; returned user " + user.Id + " " + user.DisplayName);

            return user;
        }

        public User GetUser(string userId)
        {
            var user = users.ContainsKey(userId) ? users[userId] : new User {Id = userId, DisplayName = userId};
            Debug.WriteLine("GetUser(" + userId + ") called; returned user " + user.Id + " " + user.DisplayName);
            return user;
        }

        public bool IsChatAdmin(string userId, string chatRoomId)
        {
            return false;
        }

        public bool HasChatAccess(string userId, string chatRoomId)
        {
            return true;
        }

        public string GetLoginUrl(string backURL)
        {
            return "http://www.estreamchat.com";
        }

        public void IgnoreUser(string userId, string ignoredUserId)
        {
            // Not implemented
        }

        public bool IsUserIgnored(string userId, string ignoredUserId)
        {
            // Not implemented
            return false;
        }

        #endregion
    }
}