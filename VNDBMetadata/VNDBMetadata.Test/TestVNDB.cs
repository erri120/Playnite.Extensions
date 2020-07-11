using System.Linq;
using System.Threading.Tasks;
using Playnite.SDK;
using Xunit;

namespace VNDBMetadata.Test
{
    public class TestVNDB : IClassFixture<TestLoggerFixture>
    {
        private readonly ILogger _logger;

        public TestVNDB(TestLoggerFixture fixture)
        {
            _logger = fixture.Logger;
        }

        [Fact]
        public async Task TestClient()
        {
            using var client = new VNDBClient();

            var login = await client.Login();
            Assert.True(login);

            Result<GetVN> vnResults = await client.GetVN(11);
            Assert.NotNull(vnResults);
            Assert.NotEmpty(vnResults.items);
            Assert.True(vnResults.items.Count == 1);

            var vn = vnResults.items.First();
            Assert.NotNull(vn);
        }
    }
}
