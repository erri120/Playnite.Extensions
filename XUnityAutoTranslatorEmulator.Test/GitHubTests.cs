using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace XUnityAutoTranslatorEmulator.Test
{
    public class GitHubTests
    {
        [Fact]
        public async Task TestGetGitHubReleases()
        {
            List<GitHubRelease> releases = await GitHub.GetGitHubReleases("bbepis/XUnity.AutoTranslator");
            Assert.NotNull(releases);
            Assert.NotEmpty(releases);

            var release = releases.First();
            Assert.NotNull(release.assets);
            Assert.NotEmpty(release.assets);

            var asset = release.assets.OrderBy(x => x.size).First();
            Assert.NotNull(asset);
            Assert.NotNull(asset.name);
            Assert.NotNull(asset.browser_download_url);

            if(File.Exists(asset.name))
                File.Delete(asset.name);

            await GitHub.DownloadGitHubReleaseAsset(asset, asset.name);
            Assert.True(File.Exists(asset.name));

            var fsi = new FileInfo(asset.name);
            Assert.Equal(asset.size, fsi.Length);
        }
    }
}
