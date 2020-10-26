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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extensions.Common;
using JastusaMetadata;
using Playnite.SDK;
using Xunit;
using Xunit.Abstractions;

namespace Extensions.Test
{
    public class JastusaTest : IClassFixture<LoggerFixture>
    {
        private readonly ILogger _logger;

        public JastusaTest(LoggerFixture fixture, ITestOutputHelper output)
        {
            fixture.Logger.SetOutputHelper(output);
            _logger = fixture.Logger;
        }
        
        [Fact]
        public async Task LoadGameTest()
        {
            var games = new List<string>
            {
                "saya-no-uta-the-song-of-saya",
                "the-curse-of-kubel",
                "aokana-four-rhythms-across-the-blue"
            };

            foreach (var game in games.Select(item => new JastusaGame{ID = item, Logger = _logger}))
            {
                var res = await game.LoadGame();
            
                Assert.NotNull(res);
                Assert.NotNull(game.Name);
                Assert.NotEmpty(game.Images);
                Assert.NotNull(game.Description);
                Assert.NotNull(game.CoverImage);
                Assert.NotNull(game.Studio);
                Assert.NotNull(game.Publisher);
                Assert.NotEmpty(game.AdditionalLinks);
                Assert.NotEqual(DateTime.MinValue, game.ReleaseDate);
                Assert.True(game.HasAdultRating);
            }
        }
    }
}