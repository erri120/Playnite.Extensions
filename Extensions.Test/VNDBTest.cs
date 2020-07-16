using System.Linq;
using System.Threading.Tasks;
using VNDBMetadata;
using Xunit;

namespace Extensions.Test
{
    public class VNDBTest
    {
        [Fact]
        public async Task TestClientTCP()
        {
            using var client = new VNDBClient(false);
            await TestClient(client);
        }

        [Fact]
        public async Task TestClientTLS()
        {
            using var client = new VNDBClient(true);
            await TestClient(client);
        }

        [Fact]
        public async Task TestSearch()
        {
            using var client = new VNDBClient(true);
            var login = await client.Login();
            Assert.True(login);

            Result<VisualNovel> results = await client.SearchVN("Grisaia");
            Assert.NotNull(results);
            Assert.NotEmpty(results.items);

            Assert.Equal(10, results.items.Count);
            Assert.True(results.items.All(x => x.title.StartsWith("Grisaia")));
        }

        private static async Task TestClient(VNDBClient client)
        {
            var login = await client.Login();
            Assert.True(login);

            Result<VisualNovel> vnResults = await client.GetVNByID(11);
            Assert.NotNull(vnResults);
            Assert.NotEmpty(vnResults.items);
            Assert.True(vnResults.items.Count == 1);

            var vn = vnResults.items.First();
            Assert.NotNull(vn);
        }

        [Fact]
        public async Task TestTags()
        {
            var res = await VNDBTags.GetLatestDumb("");
            Assert.True(res);

            var count = VNDBTags.ReadTags("");
            Assert.True(count != 0);
        }
    }
}
