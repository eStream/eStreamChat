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
using System.Collections.Generic;
using eStreamChat.Interfaces;
using System;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace eStreamChat.SampleProviders
{
    /// <summary>
    /// This is a sample chat room provider. It provides one main chat room
    /// </summary>
    public class ChatRoomProvider : IChatRoomProvider
    {
        #region IChatRoomProvider Members

        public IEnumerable<Room> GetChatRooms()
        {
            return new[] {GetChatRoom("1")};
        }

        public Room GetChatRoom(string chatRoomId)
        {
            return new Room
                       {
                           Id = chatRoomId,
                           MaxUsers = 100,
                           Name = "Main Chat",
                           Password = null,
                           Topic = "Welcome to eStreamChat!",
                           Visible = true
                       };
        }

        public void BanUser(string chatRoomId, string userId, string bannedUserId)
        {
            string cacheKey = "BannedUsers_" + chatRoomId;
            var bannedUsers = HttpRuntime.Cache.Get(cacheKey) as IList<Tuple<string, string>> ??
                              new List<Tuple<string, string>>();

            if (!bannedUsers.Any(t => t.Item1 == userId && t.Item2 == bannedUserId))
                bannedUsers.Add(new Tuple<string, string>(userId, bannedUserId));

            if (bannedUserId == null)
                HttpRuntime.Cache.Insert(cacheKey, bannedUsers, null, DateTime.Now.AddDays(7), Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);
        }

        private bool IsUserBanned(string chatRoomId, string bannedUser)
        {
            string cacheKey = "BannedUsers_" + chatRoomId;

            var bannedUsers = HttpRuntime.Cache.Get(cacheKey) as IList<Tuple<string, string>>;

            return (bannedUsers != null && bannedUsers.Any(t => t.Item2 == bannedUser));
        }

        public bool HasChatAccess(string userId, string chatRoomId, out string reason)
        {
            reason = null;
            var room = GetChatRoom(chatRoomId);

            if (room == null)
            {
                reason = String.Format("Specified chat room does not exist!");
                return false;
            }

            if (IsUserBanned(chatRoomId, userId))
            {
                reason = String.Format("You are banned from {0}!", room.Name);
                return false;
            }

            return true;
        }

        #endregion
    }
}