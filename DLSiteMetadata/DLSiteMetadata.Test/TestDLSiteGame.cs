using System.Threading.Tasks;
using Playnite.SDK;
using Xunit;
using Xunit.Abstractions;

namespace DLSiteMetadata.Test
{
    public class TestDLSiteGame : IClassFixture<TestLoggerFixture>
    {
        private readonly ILogger _logger;

        public TestDLSiteGame(TestLoggerFixture fixture, ITestOutputHelper output)
        {
            fixture.Logger.SetOutputHelper(output);
            _logger = fixture.Logger;
        }

        [Fact]
        public async Task TestLoadENGGame()
        {
            //https://www.dlsite.com/ecchi-eng/work/=/product_id/RE234198.html
            var game = await DLSiteGame.LoadGame("RE234198", _logger);
            
            Assert.NotNull(game);
            Assert.NotNull(game.DLSiteLink);
            Assert.NotNull(game.Name);
            Assert.NotNull(game.Description);
            Assert.NotNull(game.Circle);
            Assert.NotNull(game.CircleLink);
            Assert.NotNull(game.Genres);
            Assert.NotEmpty(game.Genres);
            Assert.NotNull(game.ImageURLs);
            Assert.NotEmpty(game.ImageURLs);
        }

        [Fact]
        public async Task TestLoadJPNGame()
        {
            //https://www.dlsite.com/maniax/work/=/product_id/RJ173356.html
            var game = await DLSiteGame.LoadGame("RJ173356", _logger);

            Assert.NotNull(game);
            Assert.NotNull(game.DLSiteLink);
            Assert.NotNull(game.Name);
            Assert.NotNull(game.Description);
            Assert.NotNull(game.Circle);
            Assert.NotNull(game.CircleLink);
            Assert.NotNull(game.Genres);
            Assert.NotEmpty(game.Genres);
            Assert.NotNull(game.ImageURLs);
            Assert.NotEmpty(game.ImageURLs);
        }
    }
}
