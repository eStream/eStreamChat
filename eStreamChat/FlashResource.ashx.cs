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
using System.IO;
using System.Reflection;
using System.Web;
using eStreamChat.Interfaces;
using Microsoft.Practices.Unity;

namespace eStreamChat
{
    public class FlashResource : IHttpHandler
    {
        private static IChatSettings chatSettings;
        private static bool importsSatisfied = false;

        public void ProcessRequest(HttpContext context)
        {
            if (!importsSatisfied)
            {
                lock (Global.CompositionLock)
                {
                    chatSettings = Global.Container.Resolve<IChatSettings>();
                }
                importsSatisfied = true;
            }

            string resourceName = context.Request.Params["resname"];
            if (resourceName != "DetectWebcam")
                resourceName = resourceName + "_" + 
                    (String.IsNullOrWhiteSpace(chatSettings.FlashServerType) ? "fms" : chatSettings.FlashServerType.Trim());

            using (Stream swfFile = Assembly.GetExecutingAssembly().GetManifestResourceStream("eStreamChat." + resourceName + ".swf"))
            {
                context.Response.ContentType = "application/x-shockwave-flash";
                context.Response.AddHeader("content-length", swfFile.Length.ToString());
                
                byte[] buffer = new byte[swfFile.Length];
                
                while(swfFile.Read(buffer, 0, buffer.Length) > 0)
                {
                    context.Response.BinaryWrite(buffer);
                }

                context.Response.Flush();
                context.Response.End();
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
