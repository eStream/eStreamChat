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

namespace eStreamChat.Interfaces
{
    public class ChatRequest
    {
        public string FromUserId { get; set; }
        public string FromUsername { get; set; }
        public string FromThumbnailUrl { get; set; }
        public string FromProfileUrl { get; set; }
        public string ToUserId { get; set; }
        public string ToUsername { get; set; }

        public DateTime Timestamp
        {
            get { return DateTime.Now; }
        }

        public string MessengerUrl { get; set; }
        public string ChatRequestMessage { get; set; }
    }
}