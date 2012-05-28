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
using Timer = System.Timers.Timer;
using System.Diagnostics.CodeAnalysis;
using eStreamChat.Interfaces;

namespace eStreamChat.Classes
{
    public static class CleanTimedOutUsers
    {
        [SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private static Timer timer;
        private static bool initialized = false;

        public static void Initialize(IChatUserProvider userProvider, IChatRoomStorage storage)
        {
            if (initialized)
                return;

            timer = InitializeTimer(userProvider, storage);

            initialized = true;
        }

        private static bool timerLock;
        private static Timer InitializeTimer(IChatUserProvider userProvider, IChatRoomStorage storage)
        {
            var t = new Timer();
            t.AutoReset = true;
            t.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            t.Elapsed += (s, e) =>
                {
                    if (timerLock){return;}

                    try
                    {
                        timerLock = true;
                        //TODO: make timeout configurable
                        var removedUsers = storage.RemoveInactiveUsers(TimeSpan.FromMinutes(1.0));

                        foreach (var userInRoom in removedUsers)
                        {
                            var userId = userInRoom.Key;
                            var roomId = userInRoom.Value;

                            var user = userProvider.GetUser(userId);

                            if (user != null)
                            {
                                storage.AddMessage(roomId, new Message
                                {
                                    Content =
                                        string.Format("User {0} has left the chat. (timeout)",
                                        user.DisplayName),
                                    FromUserId = userId,
                                    MessageType = MessageTypeEnum.UserLeft,
                                    Timestamp = Miscellaneous.GetTimestamp()
                                });

                                if (storage.UnregisterUserBroadcasts(roomId, userId))
                                {
                                    storage.AddMessage(roomId, new Message
                                    {
                                        FromUserId = userId,
                                        MessageType = MessageTypeEnum.StopVideoBroadcast,
                                        Timestamp = Miscellaneous.GetTimestamp()
                                    });
                                }
                            }
                        }

                    }
                    finally { timerLock = false; }
                };
            t.Start();
            return t;
        }
    }
}