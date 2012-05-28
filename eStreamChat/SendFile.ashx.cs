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
using eStreamChat.Interfaces;
using eStreamChat.Properties;
using System.IO;
using eStreamChat.Classes;
using Microsoft.Practices.Unity;

namespace eStreamChat
{
    public class SendFile : IHttpHandler
    {
        private static IChatRoomStorage chatRoomStorage;
        private static IChatSettings chatSettings;

        private static bool importsSatisfied;

        public void ProcessRequest(HttpContext context)
        {
            if (!importsSatisfied)
            {
                lock (Global.CompositionLock)
                {
                    chatRoomStorage = Global.Container.Resolve<IChatRoomStorage>();
                    chatSettings = Global.Container.Resolve<IChatSettings>();
                }
                importsSatisfied = true;
            }

            var request = context.Request;
            var files = request.Files;

            if (files.Count == 0)
            {
                ReturnResponse(context, "Please select a file!");
                return;
            }

            var fileName = Path.GetFileName(files[0].FileName);

            string token = request.QueryString["token"];
            string userId = chatRoomStorage.GetUserIdByToken(token);

            if (!chatSettings.EnableFileTransfer) return;

            string chatRoomIdString = request.QueryString["chatRoomId"];
            string toUserId = request.QueryString["toUserId"];

            if (String.IsNullOrWhiteSpace(chatRoomIdString))
            {
                return;
            }

            if (!chatRoomStorage.IsUserInRoom(chatRoomIdString, userId))
            {
                return;
            }

            string[] allowedExtensions = Settings.Default.SendFileAllowedExtensions.Split(',');
            bool fileIsAllowed = false;
            foreach (string extension in allowedExtensions)
            {
                if (fileName.ToLower().EndsWith("." + extension.Trim().ToLower()))
                {
                    fileIsAllowed = true;
                    break;
                }
            }

            if (!fileIsAllowed)
            {
                ReturnResponse(context, "The file type is not allowed!");
                return;
            }

            string userFilesPath = "UserFiles/" + userId;
            string userFilesDir = context.Server.MapPath(userFilesPath);
            if (!Directory.Exists(userFilesDir))
                Directory.CreateDirectory(userFilesDir);

            string filename = fileName
                .Replace('\\', '_').Replace('/', '_')
                .Replace(' ', '_').Replace('\t', '_')
                .Replace('-', '_').Replace('<', '_')
                .Replace('>', '_');
            string fileUrl = VirtualPathUtility.ToAbsolute(String.Format("~/{0}", Path.Combine(userFilesPath, filename)));
            filename = userFilesDir + @"\" + filename;

            using (Stream file = File.OpenWrite(filename))
            {
                CopyStream(files[0].InputStream, file);
            }

            var imageExtensions = new[] { "png", "gif", "bmp", "jpg" };
            bool fileIsImage = false;
            foreach (string extension in imageExtensions)
            {
                if (filename.ToLower().EndsWith("." + extension.ToLower()))
                {
                    fileIsImage = true;
                    break;
                }
            }

            chatRoomStorage.AddMessage(chatRoomIdString, new Message
            {
                Content = fileUrl,
                FromUserId = userId,
                ToUserId = toUserId,
                MessageType = fileIsImage ? MessageTypeEnum.SendImageFile :
                MessageTypeEnum.SendFile,
                Timestamp = Miscellaneous.GetTimestamp()
            });

            ReturnResponse(context, String.Empty);
        }

        private void ReturnResponse(HttpContext context, string error)
        {
            context.Response.Clear();
            context.Response.Write("{");
            context.Response.Write("error: '" + error + "'");
            context.Response.Write("}");
            context.Response.Flush();
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
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