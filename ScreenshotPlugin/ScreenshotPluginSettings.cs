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
using System.Drawing.Imaging;
using System.IO;
using Extensions.Common;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Playnite.SDK;

namespace ScreenshotPlugin
{
    public class ScreenshotPluginSettings : ISettings
    {
        private readonly ScreenshotExtensionPlugin _plugin;
        
        //public bool SaveToGame { get; set; }
        //public bool SaveToFolder { get; set; }
        public string ScreenshotsPath { get; set; }
        public Hotkey CaptureActiveMonitorHotkey { get; set; }
        public Hotkey CaptureActiveWindowHotkey { get; set; }
        public Hotkey CaptureFullscreenHotkey { get; set; }

        [UsedImplicitly]
        public ScreenshotPluginSettings() { }

        public ScreenshotPluginSettings(ScreenshotExtensionPlugin plugin)
        {
            _plugin = plugin;

            var savedSettings = plugin.LoadPluginSettings<ScreenshotPluginSettings>();

            if (savedSettings != null)
            {
                //SaveToGame = savedSettings.SaveToGame;
                //SaveToFolder = savedSettings.SaveToFolder;
                ScreenshotsPath = savedSettings.ScreenshotsPath;
                CaptureActiveMonitorHotkey = savedSettings.CaptureActiveMonitorHotkey;
                CaptureActiveWindowHotkey = savedSettings.CaptureActiveWindowHotkey;
                CaptureFullscreenHotkey = savedSettings.CaptureFullscreenHotkey;
            }

            if (ScreenshotsPath.IsEmpty())
            {
                ScreenshotsPath = Path.Combine(_plugin.PlayniteApi.Paths.ExtensionsDataPath, _plugin.Id.ToString());
            }
        }

        private ScreenshotPluginSettings _before;
        
        public void BeginEdit()
        {
            _before = new ScreenshotPluginSettings
            {
                CaptureActiveWindowHotkey = CaptureActiveWindowHotkey,
                CaptureActiveMonitorHotkey = CaptureActiveMonitorHotkey,
                CaptureFullscreenHotkey = CaptureFullscreenHotkey
            };
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(this);
            _plugin.UpdateHotkeys(_before, this);

            if (!Directory.Exists(ScreenshotsPath))
                Directory.CreateDirectory(ScreenshotsPath);
        }

        public void CancelEdit()
        {
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            /*if (SaveToFolder && ScreenshotsPath.IsEmpty())
            {
                errors.Add("Screenshots Folder Path must not be empty if you want to save the screenshots to a folder!");
            }*/

            if (ScreenshotsPath.IsEmpty())
            {
                errors.Add("Screenshots Folder Path must not be empty!");
            }
            
            var hotkeys = new HashSet<Hotkey>(new HotkeyComparer());
            if (!hotkeys.Add(CaptureFullscreenHotkey))
            {
                errors.Add("Duplicate hotkeys are not allowed!");
            }

            if (!hotkeys.Add(CaptureActiveMonitorHotkey))
            {
                errors.Add("Duplicate hotkeys are not allowed!");
            }

            if (!hotkeys.Add(CaptureActiveWindowHotkey))
            {
                errors.Add("Duplicate hotkeys are not allowed!");
            }
            
            return errors.Count <= 0;
        }
    }
}