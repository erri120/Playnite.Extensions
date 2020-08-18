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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Controls;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using ScreenshotPlugin.ShareX;

namespace ScreenshotPlugin
{
    [UsedImplicitly]
    public class ScreenshotExtensionPlugin : Plugin
    {
        private readonly IPlayniteAPI _playniteAPI;
        private readonly ILogger _logger;
        private readonly GlobalHotkeyService _globalHotkeyService;
        private readonly ScreenshotPluginSettings _settings;
        
        public override Guid Id { get; } = Guid.Parse("2ace02d2-1f1d-4d11-b430-63d7613eaa1f");

        public ScreenshotExtensionPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
            _playniteAPI = playniteAPI;
            _logger = playniteAPI.CreateLogger();
            _globalHotkeyService = new GlobalHotkeyService(_logger);
            _settings = new ScreenshotPluginSettings(this);

            _globalHotkeyService.HotkeyPress += GlobalHotkeyServiceOnHotkeyPress;
        }

        [CanBeNull]
        private Game _currentlyRunningGame;
        
        private void GlobalHotkeyServiceOnHotkeyPress(Hotkey hotkey)
        {
            try
            {
                if (_currentlyRunningGame == null && _settings.OnlyGameScreenshots)
                    return;

                Bitmap bitmap;
                string region;

                if (hotkey == _settings.CaptureFullscreenHotkey)
                {
                    bitmap = Screenshot.CaptureFullscreen();
                    region = "fullscreen";
                } else if (hotkey == _settings.CaptureActiveMonitorHotkey)
                {
                    bitmap = Screenshot.CaptureActiveMonitor();
                    region = "active monitor";
                } else if (hotkey == _settings.CaptureActiveWindowHotkey)
                {
                    bitmap = Screenshot.CaptureActiveWindow();
                    region = "active window";
                }
                else
                {
                    _logger.Error($"Unknown hotkey: {hotkey.DebugString()}");
                    return;
                }

                if (bitmap == null)
                {
                    _logger.Error($"Unable to capture {region} with hotkey {hotkey.DebugString()}");
                    return;
                }

                var fileName = DateTime.Now.ToString("yyyy-MM-ddThh-mm-ss");
                var folder = _currentlyRunningGame == null
                    ? _settings.ScreenshotsPath
                    : Path.Combine(_settings.ScreenshotsPath, _currentlyRunningGame.GameId);
                
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                
                var path = Path.Combine(folder, fileName + ".png");

                bitmap.Save(path, ImageFormat.Png);
                //bitmap.Dispose();

                _playniteAPI.Notifications.Add(new NotificationMessage(fileName, $"Saved new screenshot to {path}", NotificationType.Info,
                    () =>
                    {
                        try
                        {
                            Process.Start(path);
                        }
                        catch (Exception inner)
                        {
                            _logger.Error(inner, $"Exception while opening {path}");
                        }
                    }));
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Exception while handling hotkey {hotkey.DebugString()}\n");
            }
        }

        public override void OnGameStarted(Game game)
        {
            _currentlyRunningGame = game;
        }

        public override void OnGameStopped(Game game, long ellapsedSeconds)
        {
            _currentlyRunningGame = null;
        }

        public override void OnApplicationStarted()
        {
            UpdateHotkeys(null, _settings);
        }

        public void UpdateHotkeys([CanBeNull] ScreenshotPluginSettings before, ScreenshotPluginSettings after)
        {
            if (before != null)
            {
                _globalHotkeyService.UnregisterHotkey(before.CaptureFullscreenHotkey);
                _globalHotkeyService.UnregisterHotkey(before.CaptureActiveMonitorHotkey);
                _globalHotkeyService.UnregisterHotkey(before.CaptureActiveWindowHotkey);
            }
            
            _globalHotkeyService.RegisterHotkey(after.CaptureFullscreenHotkey);
            _globalHotkeyService.RegisterHotkey(after.CaptureActiveMonitorHotkey);
            _globalHotkeyService.RegisterHotkey(after.CaptureActiveWindowHotkey);
        }
        
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settings;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new ScreenshotSettingsView();
        }

        public override void Dispose()
        {
            _globalHotkeyService.Dispose();
        }
    }
}