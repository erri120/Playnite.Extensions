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
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace ScreenshotPlugin
{
    public class ScreenshotPlugin : Plugin
    {
        private readonly IPlayniteAPI _playniteAPI;
        private readonly ILogger _logger;
        
        public override Guid Id { get; } = Guid.Parse("2ace02d2-1f1d-4d11-b430-63d7613eaa1f");

        public ScreenshotPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
            _playniteAPI = playniteAPI;
            _logger = playniteAPI.CreateLogger();
        }

        public override void OnApplicationStarted()
        {
            base.OnApplicationStarted();
        }

        public override void OnApplicationStopped()
        {
            base.OnApplicationStopped();
        }

        public override void OnGameStarted(Game game)
        {
            base.OnGameStarted(game);
        }

        public override void OnGameStopped(Game game, long ellapsedSeconds)
        {
            base.OnGameStopped(game, ellapsedSeconds);
        }
    }
}