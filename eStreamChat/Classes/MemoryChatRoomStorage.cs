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
using System.Linq;
using eStreamChat.Interfaces;

namespace eStreamChat.Classes
{
    public class MemoryChatRoomStorage : IChatRoomStorage
    {
        private readonly Dictionary<string, List<Message>> messagesStorage;
        private readonly Dictionary<string, string> userTokens;
        private readonly Dictionary<string, Dictionary<string, DateTime>> usersStorage;
        private readonly Dictionary<string, List<Broadcast>> registeredBroadcasts;

        public MemoryChatRoomStorage()
        {
            usersStorage = new Dictionary<string, Dictionary<string,DateTime>>();
            messagesStorage = new Dictionary<string, List<Message>>();
            userTokens = new Dictionary<string, string>();
            registeredBroadcasts = new Dictionary<string, List<Broadcast>>();
        }

        #region IChatRoomStorage Members

        public string GenerateUserToken(string userId)
        {
            string token = Guid.NewGuid().ToString();
            lock (userTokens)
            {
                userTokens[token] = userId;
            }
            return token;
        }

        public string GetUserIdByToken(string token)
        {
            lock (userTokens)
            {
                return userTokens.ContainsKey(token)
                           ? userTokens[token]
                           : null;
            }
        }

        public void UpdateOnline(string chatRoomId, string userId)
        {
            lock (usersStorage)
            {
                //if room does not exist then do nothing
                if (!usersStorage.ContainsKey(chatRoomId))
                    return;

                Dictionary<string, DateTime> users = usersStorage[chatRoomId];

                lock (users)
                {
                    //if user does not exist then do nothing
                    if (!users.ContainsKey(userId))
                        return;
                    
                    users[userId] = DateTime.Now;
                }
            }
        }

        public void AddUserToRoom(string chatRoomId, string userId)
        {
            lock (usersStorage)
            {
                if (!usersStorage.ContainsKey(chatRoomId))
                    usersStorage.Add(chatRoomId, new Dictionary<string,DateTime>());

                Dictionary<string,DateTime> users = usersStorage[chatRoomId];

                lock (users)
                {
                    if (!users.ContainsKey(userId))
                        users.Add(userId, DateTime.Now);
                    else
                        users[userId] = DateTime.Now;
                }
            }
        }

        public void RemoveUserFromRoom(string chatRoomId, string userId)
        {
            lock (usersStorage)
            {
                if (!usersStorage.ContainsKey(chatRoomId))
                    return;

                Dictionary<string, DateTime> users = usersStorage[chatRoomId];

                lock (users)
                {
                    users.Remove(userId);
                }
            }
        }

        //returns removed users (roomId, userId)
        public Dictionary<string, string> RemoveInactiveUsers(TimeSpan timeOfInactivity)
        {
            var removedUsers = new Dictionary<string, string>();

            lock (usersStorage)
            {
                foreach (var room in usersStorage)
                {
                    var users = usersStorage[room.Key];

                    lock (users)
                    {
                        var userIds = users.Where(pair => DateTime.Now - pair.Value > timeOfInactivity).Select(pair => pair.Key).ToArray();

                        foreach (var userId in userIds)
                        {
                            users.Remove(userId);
                            removedUsers.Add(userId, room.Key);
                        }
                    }
                }
            }

            return removedUsers;
        }

        public string[] GetUsersInRoom(string chatRoomId)
        {
            lock (usersStorage)
            {
                if (!usersStorage.ContainsKey(chatRoomId))
                    return new string[0];

                Dictionary<string, DateTime> users = usersStorage[chatRoomId];

                lock (users)
                {
                    return users.Keys.ToArray();
                }
            }
        }

        public bool IsUserInRoom(string chatRoomId, string userId)
        {
            lock (usersStorage)
            {
                if (!usersStorage.ContainsKey(chatRoomId))
                    return false;

                Dictionary<string, DateTime> users = usersStorage[chatRoomId];

                lock (users)
                {
                    return users.Keys.Any(u => u == userId);
                }
            }
        }

        public void AddMessage(string chatRoomId, Message message)
        {
            lock (messagesStorage)
            {
                if (!messagesStorage.ContainsKey(chatRoomId))
                    messagesStorage.Add(chatRoomId, new List<Message>());

                List<Message> messages = messagesStorage[chatRoomId];

                lock (messages)
                {
                    messages.Add(message);
                }
            }
        }

        public Message[] GetMessages(string chatRoomId, string userId, long fromTimestamp)
        {
            lock (messagesStorage)
            {
                if (!messagesStorage.ContainsKey(chatRoomId))
                    return new Message[0];

                List<Message> messages = messagesStorage[chatRoomId];

                lock (messages)
                {
                    IEnumerable<Message> result = from m in messages
                                                  where (fromTimestamp == 0 || m.Timestamp > fromTimestamp)
                                                        && (m.ToUserId == null || m.ToUserId == userId)
                                                  select m;
                    // If first request
                    if (fromTimestamp == 0)
                    {
                        //do not get my kick message
                        result = result.Where(m=>!(m.FromUserId == userId && m.MessageType == MessageTypeEnum.Kicked));
                        //
                        result = result.OrderByDescending(m => m.Timestamp).Take(20);
                    }
                    else
                    {
                        //do not get my own messages except if it is a kick message
                        result = result.Where(m => m.FromUserId != userId || m.MessageType == MessageTypeEnum.Kicked);
                    }
                    return result.OrderBy(m => m.Timestamp).ToArray();
                }
            }
        }

        public void DeleteAllMessagesFor(string chatRoomId, string userId)
        {
            lock (messagesStorage)
            {
                if (!messagesStorage.ContainsKey(chatRoomId))
                    return;

                List<Message> messages = messagesStorage[chatRoomId];

                lock (messages)
                {
                    messages.RemoveAll(m => m.ToUserId == userId);
                }
            }
        }

        public void RegisterBroadcast(string chatRoomId, string userId, string targetUserId, string guid)
        {
            lock (registeredBroadcasts)
            {
                if (!registeredBroadcasts.ContainsKey(chatRoomId))
                    registeredBroadcasts.Add(chatRoomId, new List<Broadcast>());

                List<Broadcast> broadcasts = registeredBroadcasts[chatRoomId];

                lock (broadcasts)
                {
                    broadcasts.Add(new Broadcast { SenderId = userId, ReceiverId = targetUserId, Guid = guid });
                }
            }            
        }

        public bool UnregisterUserBroadcasts(string chatRoomId, string userId)
        {
            lock (registeredBroadcasts)
            {
                if (registeredBroadcasts.ContainsKey(chatRoomId))
                {
                    List<Broadcast> broadcasts = registeredBroadcasts[chatRoomId];
                    lock (broadcasts)
                    {
                        return broadcasts.RemoveAll(b => b.SenderId == userId) > 0;
                    }
                }
            }

            return false;
        }

        public Dictionary<string, string> GetBroadcasts(string chatRoomId, string receiverId)
        {
            lock (registeredBroadcasts)
            {
                if (!registeredBroadcasts.ContainsKey(chatRoomId))
                    return new  Dictionary<string, string>();

                List<Broadcast> broadcasts = registeredBroadcasts[chatRoomId];

                lock (broadcasts)
                {
                    return (from b in broadcasts
                            where (b.ReceiverId == null || b.ReceiverId == receiverId)
                            group b by b.SenderId into g
                            select g).ToDictionary(g => g.Key, g => g.First().Guid);
                }
            }
        }

        #endregion
    }
}