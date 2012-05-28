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
namespace eStreamChat.Interfaces
{
    public enum MessageTypeEnum
    {
        System = 1,
        User = 2,
        Status = 3,
        UserJoined = 4,
        UserLeft = 5,
        SendFile = 6,
        SendImageFile = 7,
        Kicked = 8, //used also for banned
        VideoBroadcast = 9,
        StopVideoBroadcast = 10,
        //ChatRequest = 11,
        RequestAccepted = 12,
        RequestDeclined = 13
    }

    public class MessageFormatOptions
    {
        public bool Bold;
        public string Color;
        public string FontName;
        public int FontSize;
        public bool Italic;
        public bool Underline;
    }

    public class Message
    {
        public string Content;
        public MessageFormatOptions FormatOptions;
        public string FromUserId;
        public MessageTypeEnum MessageType;
        public long Timestamp;
        public string ToUserId;
    }

    public class Broadcast
    {
        public string Guid;
        public string ReceiverId;
        public string SenderId;
    }
}