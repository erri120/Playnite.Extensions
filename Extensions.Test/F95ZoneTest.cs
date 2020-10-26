using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Extensions.Common;
using F95ZoneMetadata;
using Playnite.SDK;
using Xunit;
using Xunit.Abstractions;

namespace Extensions.Test
{
    public class F95ZoneTest : IClassFixture<LoggerFixture>
    {
        private readonly ILogger _logger;

        public F95ZoneTest(LoggerFixture fixture, ITestOutputHelper output)
        {
            fixture.Logger.SetOutputHelper(output);
            _logger = fixture.Logger;
        }

        [Fact]
        public void TestLoadGames()
        {
            var games = new List<string>
            {
                "amity-park-v0-7-3-gzone.4262",
                "the-mating-season-v1-02-akabur.13777",
                "poke-abby-hd-oxopotion.1068",
                "evenicle-v1-04-alicesoft.14403",
                "four-elements-trainer-v0-8-7d-mity.730"
            };

            games.Do(x =>
            {
                var game = new F95ZoneGame(_logger, x);
                var res = game.LoadGame().Result;
                Assert.NotNull(res);

                Assert.NotNull(game.Name);
                Assert.NotNull(game.Description);
                Assert.NotNull(game.Link);
                Assert.NotNull(game.Developer);
                Assert.NotEmpty(game.Labels);
                Assert.NotEmpty(game.Genres);
                Assert.NotEmpty(game.Images);
                Assert.NotEqual(DateTime.MinValue, game.ReleaseDate);
                Assert.NotEqual(-1.0, game.Rating);
                Assert.Equal(AGame.AgeRatingAdult, game.GetAgeRating());
            });
        }
    }
}
