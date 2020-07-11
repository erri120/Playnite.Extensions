using System.Threading.Tasks;
using Playnite.SDK;
using Xunit;
using Xunit.Abstractions;

namespace F95ZoneMetadata.Test
{
    public class TestF95ZoneGame : IClassFixture<TestLoggerFixture>
    {
        private readonly ILogger _logger;

        public TestF95ZoneGame(TestLoggerFixture fixture, ITestOutputHelper output)
        {
            fixture.Logger.SetOutputHelper(output);
            _logger = fixture.Logger;
        }

        [Fact]
        public async Task TestLoadGame()
        {
            var game = await F95ZoneGame.LoadGame(
                "https://f95zone.to/threads/meritocracy-of-the-oni-blade-oneone1.18664/", _logger);

            Assert.NotNull(game);
            Assert.NotNull(game.Name);
            Assert.NotNull(game.Overview);
            Assert.NotNull(game.Developer);
            Assert.NotNull(game.LabelList);
            Assert.NotEmpty(game.LabelList);
            Assert.NotNull(game.Genres);
            Assert.NotEmpty(game.Genres);
            Assert.NotNull(game.CoverImageURL);
            Assert.NotNull(game.PreviewImageURLs);
            Assert.NotEmpty(game.PreviewImageURLs);
        }
    }
}
