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
using System.Web.UI;

namespace eStreamChat
{
    public partial class ChatRoomPage : Page
    {
        protected override void OnPreInit(EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request.Params["theme"]))
                Page.Theme = Request.Params["theme"];

            base.OnPreInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}