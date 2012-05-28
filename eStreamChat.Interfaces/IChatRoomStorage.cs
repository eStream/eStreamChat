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

namespace eStreamChat.Interfaces
{
    public interface IChatRoomStorage
    {
        string GenerateUserToken(string userId);
        string GetUserIdByToken(string token);

        void AddUserToRoom(string chatRoomId, string userId);
        void RemoveUserFromRoom(string chatRoomId, string userId);
        string[] GetUsersInRoom(string chatRoomId);
        bool IsUserInRoom(string chatRoomId, string userId);
        void UpdateOnline(string chatRoomId, string userId);
        Dictionary<string, string> RemoveInactiveUsers(TimeSpan timeOfInactivity);

        void AddMessage(string chatRoomId, Message message);
        Message[] GetMessages(string chatRoomId, string userId, long fromTimestamp);
        void DeleteAllMessagesFor(string chatRoomId, string userId);

        void RegisterBroadcast(string chatRoomId, string userId, string targetUserId, string guid);
        bool UnregisterUserBroadcasts(string chatRoomId, string userId);
        Dictionary<string, string> GetBroadcasts(string chatRoomId, string receiverId);
    }
}