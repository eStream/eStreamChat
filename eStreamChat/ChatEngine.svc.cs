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
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Text.RegularExpressions;
using System.Web;
using eStreamChat.Classes;
using eStreamChat.Interfaces;
using System.Net;
using System.Collections.Generic;
using Microsoft.Practices.Unity;

namespace eStreamChat
{
    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ChatEngine
    {
        private static IChatRoomProvider chatRoomProvider;
        private static IChatRoomStorage chatRoomStorage;
        private static IChatUserProvider chatUserProvider;
        private static IChatSettings chatSettingsProvider;
        private static IMessengerPresenceProvider messengerProvider;

        private static bool importsSatisfied = false;

        public ChatEngine()
        {
            if (!importsSatisfied)
            {
                lock (Global.CompositionLock)
                {
                    chatRoomProvider = Global.Container.Resolve<IChatRoomProvider>();
                    chatRoomStorage = Global.Container.Resolve<IChatRoomStorage>();
                    chatUserProvider = Global.Container.Resolve<IChatUserProvider>();
                    chatSettingsProvider = Global.Container.Resolve<IChatSettings>();
                    if (Global.Container.IsRegistered<IMessengerPresenceProvider>())
                        messengerProvider = Global.Container.Resolve<IMessengerPresenceProvider>();
                }
                importsSatisfied = true;
            }

            CleanTimedOutUsers.Initialize(chatUserProvider, chatRoomStorage);
        }

        [OperationContract]
        public JoinChatRoomResult JoinChatRoom(string chatRoomId, string href)
        {
            if (href != null) HttpContext.Current.Items["href"] = href;

            var result = new JoinChatRoomResult
                             {
                                 FileTransferEnabled = chatSettingsProvider.EnableFileTransfer,
                                 VideoChatEnabled = chatSettingsProvider.EnableVideoChat,
                                 FlashMediaServer = chatSettingsProvider.FlashMediaServer
                             };

            User user;
            try
            {
                user = chatUserProvider.GetCurrentlyLoggedUser();
            }
            catch (System.Security.SecurityException err)
            {
                result.Error = err.Message;
                return result;
            }

            if (user == null)
            {
                string loginUrl = chatUserProvider.GetLoginUrl(href);
                result.Error = "You need to login in order to use the chat!";
                if (loginUrl != null)
                {
                    result.Error += " Redirecting to login page...";
                    result.RedirectUrl = loginUrl;
                }
                return result;
            }

            Room room = chatRoomProvider.GetChatRoom(chatRoomId);

            if (room == null)
            {
                result.Error = "The specified chat room does not exist!";
                return result;
            }

            #region Messenger
            var url = (new Uri(href));
            var queryKeyValues = HttpUtility.ParseQueryString(url.Query);
            var isInitiator = queryKeyValues["init"];
            var targetUserId = queryKeyValues["target"];
            bool alreadyConnected = href.IndexOf('#') == -1 ? false : (href.Substring(href.IndexOf('#')) == "#connected");
            #endregion

            string reason;

            //if messenger
            if (room.Id == "-2")
            {
                //check chat access only for the initiators
                if (isInitiator == "1" && !chatRoomProvider.HasChatAccess(user.Id, room.Id, out reason))
                {
                    if (String.IsNullOrEmpty(reason))
                        result.Error = "You don't have access to the messenger.";
                    else
                        result.Error = reason;

                    return result;
                }
            }
            else
            {
                if (!chatRoomProvider.HasChatAccess(user.Id, room.Id, out reason))
                {
                    if (String.IsNullOrEmpty(reason))
                        result.Error = String.Format("You don't have access to chat room '{0}'.", room.Name);
                    else
                        result.Error = reason;

                    return result;
                }
            }

            // Ignore max users limit for messengerRoom (roomId = -2)
            if (room.Id != "-2" && chatRoomStorage.GetUsersInRoom(room.Id).Length >= room.MaxUsers)
            {
                result.Error = String.Format("The chat room '{0}' is full. Please try again later.", room.Name);

                return result;
            }

            chatRoomStorage.AddUserToRoom(room.Id, user.Id);
            chatRoomStorage.AddMessage(room.Id, new Message
                                                    {
                                                        Content =
                                                            string.Format("User {0} has joined the chat.",
                                                                          user.DisplayName),
                                                        FromUserId = user.Id,
                                                        ToUserId = String.IsNullOrEmpty(targetUserId) ? null : targetUserId,
                                                        MessageType = MessageTypeEnum.UserJoined,
                                                        Timestamp = Miscellaneous.GetTimestamp()
                                                    });

            #region Messenger

            //delete all messages when starting messenger (not reloading it)
            if (isInitiator != null && !alreadyConnected)
            {
                chatRoomStorage.DeleteAllMessagesFor(room.Id, user.Id);
            }

            if (!alreadyConnected && isInitiator == "1" && !String.IsNullOrWhiteSpace(targetUserId))
            {
                try
                {
                    var targetUser = chatUserProvider.GetUser(targetUserId);

                    //Not sure that MessengerUrl should be constructed here?! timestamp and hash are tied to
                    //the implementation
                    var timestamp = DateTime.Now.ToFileTimeUtc();
                    var hash = Miscellaneous.CalculateChatAuthHash(targetUserId, user.Id, timestamp.ToString());

                    var request = new ChatRequest
                                      {
                                          FromThumbnailUrl = user.ThumbnailUrl,
                                          FromProfileUrl = user.ProfileUrl,
                                          FromUserId = user.Id,
                                          FromUsername = user.DisplayName,
                                          ToUserId = targetUserId,
                                          ToUsername = targetUser.DisplayName,
                                          MessengerUrl =
                                              String.Format("{0}?init=0&id={1}&target={2}&timestamp={3}&hash={4}",
                                                            url.GetLeftPart(UriPartial.Path), targetUserId, user.Id,
                                                            timestamp, hash)
                                      };

                    messengerProvider.AddChatRequest(request);
                }
                catch (System.Security.SecurityException) { }
            }
            #endregion

            result.ChatRoomName = room.Name;
            result.ChatRoomTopic = room.Topic;
            result.Users = chatRoomStorage.GetUsersInRoom(room.Id).Select(chatUserProvider.GetUser).ToArray();
            result.Token = chatRoomStorage.GenerateUserToken(user.Id);
            result.UserId = user.Id;
            result.IsAdmin = chatUserProvider.IsChatAdmin(user.Id, room.Id);

            StopVideoBroadcast(user.Id, room.Id);
            result.Broadcasts = chatRoomStorage.GetBroadcasts(room.Id, user.Id);

            return result;
        }

        [OperationContract]
        public void LeaveChatRoom(string chatRoomId, string token, string messengerTargetUserId)
        {
            string userId = chatRoomStorage.GetUserIdByToken(token);
            if (userId == null) return;
            var user = chatUserProvider.GetUser(userId);

            Room room = chatRoomProvider.GetChatRoom(chatRoomId);
            if (room == null) return;

            //do not leave the room if messenger. The user will eventually time out when all the 
            // messenger windows are closed.
            if (chatRoomId != "-2")
                chatRoomStorage.RemoveUserFromRoom(room.Id, user.Id);

            chatRoomStorage.AddMessage(room.Id, new Message
                                                    {
                                                        Content =
                                                            string.Format("User {0} has left the chat.",
                                                                          user.DisplayName),
                                                        FromUserId = user.Id,
                                                        ToUserId = messengerTargetUserId,
                                                        MessageType = MessageTypeEnum.UserLeft,
                                                        Timestamp = Miscellaneous.GetTimestamp()
                                                    });

            StopVideoBroadcast(userId, room.Id);
        }

        [OperationContract]
        public EventsResult GetEvents(string chatRoomId, string token, long fromTimestamp, string messengerTargetUserId)
        {
            var result = new EventsResult();
            string userId = chatRoomStorage.GetUserIdByToken(token);
            if (userId == null)
            {
                result.Error = "Chat disconnected!";
                return result;
            }

            chatRoomStorage.UpdateOnline(chatRoomId, userId);

            //get all the messages for the current user
            IEnumerable<Message> messages = chatRoomStorage.GetMessages(chatRoomId, userId, fromTimestamp);

            //if in messenger mode then filter messages to be only from the target user or from the current user himself
            if (messengerTargetUserId != null)
            {
                messages = messages.Where(m => m.FromUserId == userId || m.FromUserId == messengerTargetUserId);
            }

            //get only joined, left and kicked messages for ignored users
            result.Messages = messages.Where(m =>!chatUserProvider.IsUserIgnored(userId, m.FromUserId) ||
                                                m.MessageType == MessageTypeEnum.UserJoined ||
                                                m.MessageType == MessageTypeEnum.UserLeft ||
                                                m.MessageType == MessageTypeEnum.Kicked).ToArray();

            var joinMessages = messages.Where(m => m.MessageType == MessageTypeEnum.UserJoined);
            var leaveMessages = messages.Where(m => m.MessageType == MessageTypeEnum.UserLeft || m.MessageType == MessageTypeEnum.Kicked);

            var joinedUsers = joinMessages.Where(j => !leaveMessages.Any(l => l.FromUserId == j.FromUserId && l.Timestamp > j.Timestamp)).Select(
                                m => chatUserProvider.GetUser(m.FromUserId)).ToArray();
            var leftUsers = leaveMessages.Where(l => !joinMessages.Any(j => j.FromUserId == l.FromUserId && j.Timestamp > l.Timestamp)).Select(
                                m => chatUserProvider.GetUser(m.FromUserId)).ToArray();

            //var joinedUsers = result.Messages.Where(m => m.MessageType == MessageTypeEnum.UserJoined).Select(
            //    m => chatUserProvider.GetUser(m.FromUserId)).ToArray();

            //var leftUsers = result.Messages.Where(m => m.MessageType == MessageTypeEnum.UserLeft || m.MessageType == MessageTypeEnum.Kicked).Select(
            //    m => chatUserProvider.GetUser(m.FromUserId)).ToArray();

            result.UsersJoined = joinedUsers;
            result.UsersLeft = leftUsers;

            if (ConfigurationManager.AppSettings["PollingInterval"] != null)
                result.CallInterval = Convert.ToInt32(ConfigurationManager.AppSettings["PollingInterval"]);

            return result;
        }

        [OperationContract]
        public SendMessageResult SendMessage(string chatRoomId, string token, string toUserId, string message, 
            bool bold = false, bool italic = false, bool underline = false, string fontName = null, int? fontSize = null,
            string color = null)
        {
            var result = new SendMessageResult();
            string userId = chatRoomStorage.GetUserIdByToken(token);
            if (userId == null)
            {
                result.Error = "Chat disconnected!";
                return result;
            }

            if (!chatRoomStorage.IsUserInRoom(chatRoomId, userId))
            {
                result.Error = "Chat disconnected!";
                return result;
            }

            // TODO: Process message (trim, filter)
            if (!string.IsNullOrEmpty(color))
                color = Regex.Replace(color, @"[^\w\#]", String.Empty); // Strip dangerous input
            var formatOptions = new MessageFormatOptions
                                           {
                                               Bold = bold,
                                               Italic = italic,
                                               Underline = underline,
                                               Color = color,
                                               FontName = fontName,
                                           };
            if (fontSize.HasValue) formatOptions.FontSize = fontSize.Value;
            chatRoomStorage.AddMessage(chatRoomId, new Message
                                                       {
                                                           Content = WebUtility.HtmlEncode(message),
                                                           FromUserId = userId,
                                                           ToUserId = toUserId,
                                                           MessageType = MessageTypeEnum.User,
                                                           Timestamp = Miscellaneous.GetTimestamp(),
                                                           FormatOptions = formatOptions
                                                       });

            return result;
        }

        [OperationContract]
        public SendMessageResult SendCommand(string chatRoomId, string token, string targetUserId, string command)
        {
            var result = new SendMessageResult();
            string userId = chatRoomStorage.GetUserIdByToken(token);
            if (userId == null)
            {
                result.Error = "Chat disconnected!";
                return result;
            }

            if (!chatRoomStorage.IsUserInRoom(chatRoomId, userId))
            {
                result.Error = "Chat disconnected!";
                return result;
            }

            var user = chatUserProvider.GetUser(userId);
            var targetUser = chatUserProvider.GetUser(targetUserId);

            if (command == "ignore")
            {
                chatUserProvider.IgnoreUser(userId, targetUserId);
            }
            else if (command == "kick")
            {
                if (chatUserProvider.IsChatAdmin(userId, chatRoomId) && chatRoomStorage.IsUserInRoom(chatRoomId, targetUserId))
                {
                    chatRoomStorage.RemoveUserFromRoom(chatRoomId, targetUserId);

                    chatRoomStorage.AddMessage(chatRoomId, new Message
                    {
                        Content =
                            string.Format("User {0} has been kicked off the room by {1}.",
                                          targetUser.DisplayName, user.DisplayName),
                        FromUserId = targetUserId,
                        MessageType = MessageTypeEnum.Kicked,
                        Timestamp = Miscellaneous.GetTimestamp()
                    });
                }
            }
            else if (command == "ban")
            {
                if (chatUserProvider.IsChatAdmin(userId, chatRoomId) && chatRoomStorage.IsUserInRoom(chatRoomId, targetUserId))
                {
                    chatRoomStorage.RemoveUserFromRoom(chatRoomId, targetUserId);

                    chatRoomStorage.AddMessage(chatRoomId, new Message
                    {
                        Content =
                            string.Format("User {0} has been kicked off the room by {1} (Banned).",
                                          targetUser.DisplayName, user.DisplayName),
                        FromUserId = targetUserId,
                        MessageType = MessageTypeEnum.Kicked,
                        Timestamp = Miscellaneous.GetTimestamp()
                    });

                    chatRoomProvider.BanUser(chatRoomId, userId, targetUserId);
                }
            }
            else if (command == "slap")
            {
                chatRoomStorage.AddMessage(chatRoomId, new Message
                {
                    Content =
                        string.Format("{0} slaps {1} around with a large trout.",
                                      user.DisplayName, targetUser.DisplayName),
                    FromUserId = userId,
                    MessageType = MessageTypeEnum.System,
                    Timestamp = Miscellaneous.GetTimestamp()
                });
            }

            return result;
        }

        [OperationContract]
        public string BroadcastVideo(string prevGuid, string token, int chatRoomId, string targetUserId)
        {
            if (!chatSettingsProvider.EnableVideoChat) return null;

            string userId = chatRoomStorage.GetUserIdByToken(token);
            string guid = prevGuid ?? Guid.NewGuid().ToString();

            chatRoomStorage.AddMessage(chatRoomId.ToString(), new Message
            {
                Content = guid,
                FromUserId = userId,
                ToUserId = targetUserId,
                MessageType = MessageTypeEnum.VideoBroadcast,
                Timestamp = Miscellaneous.GetTimestamp()
            });

            chatRoomStorage.RegisterBroadcast(chatRoomId.ToString(), userId, targetUserId, guid);
            return guid;
        }

        [OperationContract]
        public void StopVideoBroadcast(string token, int chatRoomId)
        {
            string userId = chatRoomStorage.GetUserIdByToken(token);

            StopVideoBroadcast(userId, chatRoomId.ToString());
        }

        private void StopVideoBroadcast(string userId, string chatRoomId)
        {
            if (chatRoomStorage.UnregisterUserBroadcasts(chatRoomId, userId))
            {
                chatRoomStorage.AddMessage(chatRoomId, new Message
                {
                    FromUserId = userId,
                    MessageType = MessageTypeEnum.StopVideoBroadcast,
                    Timestamp = Miscellaneous.GetTimestamp()
                });
            }
        }

        #region Nested type: EventsResult

        public class EventsResult
        {
            public string Error;
            public Message[] Messages;
            public User[] UsersJoined;
            public User[] UsersLeft;
            public int? CallInterval;
        }

        #endregion

        #region Nested type: JoinChatRoomResult

        public class JoinChatRoomResult
        {
            public string ChatRoomName;
            public string ChatRoomTopic;
            public string Error;
            public string RedirectUrl;
            public string Token;
            public string UserId;
            public User[] Users;
            public bool IsAdmin;
            public bool FileTransferEnabled;
            public bool VideoChatEnabled;
            public string FlashMediaServer;
            public Dictionary<string, string> Broadcasts;
        }

        #endregion

        #region Nested type: SendMessageResult

        public class SendMessageResult
        {
            public string Error;
        }

        #endregion
    }
}