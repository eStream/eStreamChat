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
using System.Security.Cryptography;
using System.Text;

namespace eStreamChat.Classes
{
    public static class Miscellaneous
    {
        public static long GetTimestamp()
        {
            return DateTime.Now.AddYears(-2000).Ticks;
        }

        public static string CalculateChatAuthHash(string userID, string targetUserID, string timestamp)
        {
            if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["AuthSecretKey"]))
                throw new Exception("AuthSecretKey must be specified in your web.config file");

            var sha1 = new SHA1Managed();
            var paramBytes = Encoding.UTF8.GetBytes(userID + targetUserID + timestamp + ConfigurationManager.AppSettings["AuthSecretKey"]);
            var hashBytes = sha1.ComputeHash(paramBytes);
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hash;
        }
    }
}