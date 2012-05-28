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
using System.Web;
using System.Text;
using eStreamChat.Interfaces;
using Microsoft.Practices.Unity;
using eStreamChat.Classes;

namespace eStreamChat
{
    public class MessengerCommand : IHttpHandler
    {
        private static IChatRoomStorage chatRoomStorage;
        private static IChatUserProvider chatUserProvider;
        private static IMessengerPresenceProvider messengerProvider;

        private static bool importsSatisfied;

        public MessengerCommand()
        {
            if (!importsSatisfied)
            {
                lock (Global.CompositionLock)
                {
                    chatRoomStorage = Global.Container.Resolve<IChatRoomStorage>();
                    chatUserProvider = Global.Container.Resolve<IChatUserProvider>();
                    messengerProvider = Global.Container.Resolve<IMessengerPresenceProvider>();
                }
                importsSatisfied = true;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;

            HttpContext.Current.Items["href"] = context.Request.Url.ToString();
            var user = chatUserProvider.GetCurrentlyLoggedUser();
            var fromUserId = context.Request.QueryString["reqInitiator"];
            bool rejected;

            if (context.Request.QueryString["reject"] != null &&
                Boolean.TryParse(context.Request.QueryString["reject"], out rejected) && !String.IsNullOrWhiteSpace(fromUserId))
            {
                SendResponse(fromUserId, user.Id, rejected);

                context.Response.Write(context.Request.QueryString["callback"] + "({});");
            }
            else
            {
                var chatRequest = UpdateMessengerPresence(context, user);

                if (chatRequest != null)
                {
                    context.Response.Write(context.Request.QueryString["callback"] + "({" +
                        "'MessengerUrl': '" + chatRequest.MessengerUrl + "'," +
                        "'FromUserId' : '" + chatRequest.FromUserId + "'," +
                        "'ToUserId' : '" + chatRequest.ToUserId + "'," +
                        "'FromThumbnailUrl' : '" + chatRequest.FromThumbnailUrl + "'," +
                        "'FromProfileUrl' : '" + chatRequest.FromProfileUrl + "'," +
                        "'ChatRequestMessage' : '" + chatRequest.ChatRequestMessage + "'});");
                }
            }
        }

        public ChatRequest UpdateMessengerPresence(HttpContext context, User user)
        {
            messengerProvider.UpdateLastOnline(user.Id);

            ChatRequest request = messengerProvider.GetChatRequest(user.Id);

            if (request != null)
            {
                request.ChatRequestMessage = String.Format("User <b>{0}</b> wants to chat with you!",
                                                           request.FromUsername);
            }

            return request;
        }

        public void SendResponse(string fromUserId, string toUserId, bool rejected)
        {
            ChatRequest request = messengerProvider.GetChatRequest(toUserId);
            if (request == null) return;

            messengerProvider.RemoveChatRequest(request.FromUserId, request.ToUserId);

            if (rejected)
            {
                string message = String.Format("User {0} rejected your chat request", toUserId);

                chatRoomStorage.AddMessage("-2",
                    new Message
                    {
                        Content = message,
                        FromUserId = toUserId,
                        ToUserId = fromUserId,
                        Timestamp = Miscellaneous.GetTimestamp(),
                        MessageType = MessageTypeEnum.RequestDeclined
                    });
            }
            else
            {
                string message = String.Format("User {0} accepted your chat request", toUserId);

                chatRoomStorage.AddMessage("-2",
                    new Message
                    {
                        Content = message,
                        FromUserId = toUserId,
                        ToUserId = fromUserId,
                        Timestamp = Miscellaneous.GetTimestamp(),
                        MessageType = MessageTypeEnum.RequestAccepted
                    });
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}