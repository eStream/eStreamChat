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
    public class User
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string PhotoUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string ProfileUrl { get; set; }
    }
}