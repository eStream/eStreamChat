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
using System.Reflection;
using System.Web;
using eStreamChat.Interfaces;
using eStreamChat.Properties;
using System.Configuration;
using System.IO;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;

namespace eStreamChat
{
    public class Global : HttpApplication
    {
        public static readonly object CompositionLock = new object();
        public static IUnityContainer Container;

        private string providersPath;
        private ILogger Logger { get; set; }

        private void Application_Start(object sender, EventArgs e)
        {            
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            Container = new UnityContainer();

            var section = (UnityConfigurationSection)ConfigurationManager.GetSection("unity");

            section.Configure(Container);

            Logger = Container.Resolve<ILogger>();
            Logger.Log("Application Start");
        }

        private void Application_End(object sender, EventArgs e)
        {
            Container.Dispose();
            Logger.Log("Application End");
        }

        private void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
        }

        private void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started
        }

        private void Session_End(object sender, EventArgs e)
        {
            // Code that runs when a session ends. 
            // Note: The Session_End event is raised only when the sessionstate mode
            // is set to InProc in the Web.config file. If session mode is set to StateServer 
            // or SQLServer, the event is not raised.
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = args.Name.Split(',')[0];

            if (providersPath == null)
                providersPath = Server.MapPath(Settings.Default.ProvidersPath);

            var assembly = Assembly.LoadFrom(
                                       Path.Combine(providersPath, assemblyName + ".dll"));
            return assembly;
        }
    }
}