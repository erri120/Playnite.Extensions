using System.Threading.Tasks;
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
        public async Task TestLoadGame()
        {
            var game = await F95ZoneGame.LoadGame(
                "https://f95zone.to/threads/alien-quest-eve-v1-01-grimhelm.6016/", _logger);

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
