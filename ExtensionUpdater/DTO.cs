// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;

namespace ExtensionUpdater
{
    [Serializable]
    public class PlayniteExtensionConfig
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string Module { get; set; }
        public string Type { get; set; }
        public string Icon { get; set; }
        
        public UpdaterConfig UpdaterConfig { get; set; }
    }

    [Serializable]
    public class UpdaterConfig
    {
        public string GitHubUser { get; set; }
        public string GitHubRepo { get; set; }
    }
}