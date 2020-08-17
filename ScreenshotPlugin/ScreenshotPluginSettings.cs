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

using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Playnite.SDK;
using ScreenshotPlugin.Hotkey;

namespace ScreenshotPlugin
{
    public class ScreenshotPluginSettings : ISettings
    {
        private readonly ScreenshotExtensionPlugin _plugin;
        
        public bool NoGameScreenshots { get; set; }
        public string FullscreenScreenshotsPath { get; set; }
        public Hotkey.Hotkey FullscreenWPFHotkey { get; set; }
        public Hotkey.Hotkey ActiveWindowWPFHotkey { get; set; }

        [UsedImplicitly]
        public ScreenshotPluginSettings() { }

        public ScreenshotPluginSettings(ScreenshotExtensionPlugin plugin)
        {
            _plugin = plugin;
            
            var savedSettings = plugin.LoadPluginSettings<ScreenshotPluginSettings>();

            if (savedSettings == null) return;
            NoGameScreenshots = savedSettings.NoGameScreenshots;
            FullscreenScreenshotsPath = savedSettings.FullscreenScreenshotsPath;
            FullscreenWPFHotkey = savedSettings.FullscreenWPFHotkey;
            ActiveWindowWPFHotkey = savedSettings.ActiveWindowWPFHotkey;
        }
        
        public void BeginEdit()
        {
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(this);
        }

        public void CancelEdit()
        {
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}