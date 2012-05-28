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
using System.Timers;
using eStreamChat.Interfaces;

namespace eStreamChat.Classes
{
    public class MemoryMessengerPresenceProvider : IMessengerPresenceProvider
    {
        private readonly List<ChatRequest> lChatRequests = new List<ChatRequest>();
        private readonly Dictionary<string, DateTime> dLastOnline = new Dictionary<string, DateTime>();
        private readonly int presenceInterval = 20; // in seconds
        private readonly Timer cleanupTimer = new Timer();

        public MemoryMessengerPresenceProvider()
        {
            cleanupTimer.Interval = presenceInterval * 5 * 1000;
            cleanupTimer.Elapsed += cleanupTimer_Elapsed;
            cleanupTimer.Start();
        }

        void cleanupTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (dLastOnline)
            {
                var listKeys = new List<string>();
                foreach (string key in dLastOnline.Keys)
                {
                    if (DateTime.Now.Subtract(dLastOnline[key]).TotalSeconds > presenceInterval * 5)
                        listKeys.Add(key);
                }
                foreach (string key in listKeys)
                {
                    dLastOnline.Remove(key);
                }
            }
        }

        #region IMessengerPresenceProvider Members

        public void AddChatRequest(ChatRequest request)
        {
            lock (lChatRequests)
            {
                lChatRequests.Add(request);
            }
        }

        public ChatRequest GetChatRequest(string toUserId)
        {
            lock (lChatRequests)
            {
                return lChatRequests.FirstOrDefault(r => r.ToUserId == toUserId);
            }
        }

        public void RemoveChatRequest(string fromUserId, string toUserId)
        {
            lock (lChatRequests)
            {
                lChatRequests.RemoveAll(r => r.ToUserId == toUserId && r.FromUserId == fromUserId);
            }
        }

        public void UpdateLastOnline(string userId)
        {
            lock (dLastOnline)
            {
                dLastOnline[userId] = DateTime.Now;
            }
        }

        #endregion
    }
}