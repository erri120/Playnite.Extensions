using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Contrib.HttpClient;
using Playnite.SDK.Models;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace FanzaMetadata.Test;

public class FanzaTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public FanzaTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("216826")]
    public async Task TestScrapGamePage(string id)
    {
        var file = Path.Combine("files", $"{id}.html");
        Assert.True(File.Exists(file));

        var handler = new Mock<HttpMessageHandler>();
        handler
            .SetupAnyRequest()
            .ReturnsResponse(File.ReadAllBytes(file));

        var scrapper = new Scrapper(new XunitLogger<Scrapper>(_testOutputHelper), handler.Object);
        var res = await scrapper.ScrapGamePage(id);

        Assert.NotNull(res.Link);
        Assert.NotNull(res.Title);
        Assert.NotNull(res.Circle);
        Assert.NotNull(res.Genres);
        Assert.NotEmpty(res.Genres!);
        Assert.NotNull(res.GameGenre);
        Assert.NotNull(res.PreviewImages);
        Assert.NotEmpty(res.PreviewImages!);
    }

    [Theory]
    [InlineData("216826", "https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_216826/")]
    [InlineData("200809", "https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_200809/?dmmref=ListRanking&i3_ref=list&i3_ord=5")]
    public void TestGetIdFromGame(string id, string name)
    {
        Assert.Equal(id, FanzaMetadataProvider.GetIdFromGame(new Game(name)));
    }
}
