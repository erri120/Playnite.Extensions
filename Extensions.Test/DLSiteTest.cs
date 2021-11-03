using System;
using System.IO;
using System.Threading.Tasks;
using DLSiteMetadata;
using Extensions.Common;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;
using Xunit;
using Xunit.Abstractions;

namespace Extensions.Test
{
    public class DLSiteTest : IClassFixture<LoggerFixture>
    {
        private readonly ILogger _logger;

        public DLSiteTest(LoggerFixture fixture, ITestOutputHelper output)
        {
            fixture.Logger.SetOutputHelper(output);
            _logger = fixture.Logger;
        }

        [Fact]
        public async Task TestLoadENGGame()
        {
            //https://www.dlsite.com/ecchi-eng/work/=/product_id/RE234198.html
            var game = new DLSiteGame(_logger, "RE234198");
            await game.LoadGame();
            TestGame(game);
        }

        [Fact]
        public async Task TestLoadJPNGame()
        {
            //https://www.dlsite.com/maniax/work/=/product_id/RJ173356.html
            var game = new DLSiteGame(_logger, "RJ173356");
            await game.LoadGame();
            TestGame(game);
        }

        [Fact]
        public void TestGenres()
        {
            if(File.Exists("genres.json"))
                File.Delete("genres.json");

            var genre = new DLSiteGenre(60) {JPN = "女性視点"};
            var eng = DLSiteGenres.ConvertTo(genre, _logger, false);
            Assert.True(!string.IsNullOrEmpty(eng));
            Assert.True(eng.Equals("Woman's Viewpoint", StringComparison.OrdinalIgnoreCase));

            genre.ENG = eng;
            DLSiteGenres.AddGenres(new []{genre});

            var res = DLSiteGenres.TryGetGenre(60, out var cachedGenre);
            Assert.True(res);
            Assert.NotNull(cachedGenre);
            Assert.Equal(genre, cachedGenre);

            DLSiteGenres.SaveGenres("");

            var count = DLSiteGenres.LoadGenres("");
            Assert.Equal(1, count);
        } 

        [AssertionMethod]
        private static void TestGame(DLSiteGame game)
        {
            Assert.NotNull(game);
            Assert.NotNull(game.Link);
            Assert.NotNull(game.Name);
            Assert.NotNull(game.Description);
            Assert.NotNull(game.Circle);
            Assert.NotNull(game.Circle);
            Assert.NotNull(game.Genres);
            Assert.NotEmpty(game.Genres);
            Assert.NotNull(game.ImageURLs);
            Assert.NotEmpty(game.ImageURLs);
            Assert.NotEqual(new ReleaseDate(DateTime.MinValue), game.Release);
            //Assert.Equal(AGame.AgeRatingAdult,game.GetAgeRating());
            Assert.NotEmpty(game.GetAgeRatings());
        }
    }
}
