using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [InlineData("216826", "https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_216826/?i3_ref=search&i3_ord=1")]
    public async Task TestScrapGamePage(string id, string url)
    {
        var scrapper = new DoujinGameScrapper(new XunitLogger<DoujinGameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapGamePage(new SearchResult("爆乳忍法チチシノビ", id, url));

        Assert.NotNull(res?.Link);
        Assert.NotNull(res?.Title);
        Assert.NotNull(res?.Circle);
        Assert.NotNull(res?.Genres);
        Assert.NotEmpty(res?.Genres!);
        Assert.NotNull(res?.GameGenre);
        Assert.NotNull(res?.PreviewImages);
        Assert.NotEmpty(res?.PreviewImages!);
        Assert.True(res?.Adult);
        Assert.NotNull(res?.Description);
    }

    [Fact]
    public async Task ShouldGetScrapSearchPage()
    {
        var scrapper = new DoujinGameScrapper(new XunitLogger<DoujinGameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapSearchPage("ゴブリンの巣穴");
        Assert.NotEmpty(res);
    }


    [Theory]
    [InlineData("https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_216826/")]
    [InlineData("https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_216826/?dmmref=ListRanking&i3_ref=list&i3_ord=5")]
    [InlineData("https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_216826/?i3_ref=search&i3_ord=1")]
    public void ShouldGetIdFromDoujinGameLink(string link)
    {
        var id = DoujinGameScrapper.ParseLinkId(link);
        Assert.Equal("216826", id);
    }


    [Theory]
    [InlineData("https://dlsoft.dmm.co.jp/detail/sitri_0011")]
    [InlineData("https://dlsoft.dmm.co.jp/detail/sitri_0011/?i3_ref=search&i3_ord=1")]
    public void ShouldGetIdFromLink(string link)
    {
        var id = GameScrapper.ParseLinkId(link);
        Assert.Equal("sitri_0011", id);
    }


    [Fact]
    public async void ShouldGetSearchResult()
    {
        var scrapper = new GameScrapper(new XunitLogger<GameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapSearchPage("家の彼女");
        Assert.NotEmpty(res);
    }

    [Fact]
    public async void ShouldGetScrapperResult()
    {
        var scrapper = new GameScrapper(new XunitLogger<GameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapGamePage(new SearchResult("美少女万華鏡 呪われし伝説の少女", "views_0669",
            "https://dlsoft.dmm.co.jp/detail/views_0669/"));
        Assert.NotNull(res);
        Assert.NotNull(res?.Title);
        Assert.NotNull(res?.Genres);
    }

    [Fact]
    public async void ShouldGetScrapperResult2()
    {
        var scrapper = new GameScrapper(new XunitLogger<GameScrapper>(_testOutputHelper));
        var searchRes = await scrapper.ScrapSearchPage("美少女万華鏡 呪われし伝説の少女");
        Assert.NotEmpty(searchRes);

        var res = await scrapper.ScrapGamePage(searchRes.First());
        Assert.NotNull(res);
        Assert.NotNull(res?.Title);
        Assert.NotNull(res?.Genres);
        Assert.NotNull(res?.Description);
        Assert.True(res?.Adult);
    }


    [Fact]
    public async void ShouldNotGetScrapperResult()
    {
        var scrapper = new GameScrapper(new XunitLogger<GameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapGamePage(new SearchResult("djsaklnla", "views_0669111111111",
            "https://d11111111lsoft.dmm.co.jp/detail/views_0669/"));
        Assert.Null(res);
    }

    [Fact]
    public async void ShouldNotGetScrapperResultNotSupportDomain()
    {
        var scrapper = new DoujinGameScrapper(new XunitLogger<DoujinGameScrapper>(_testOutputHelper));
        var res = await scrapper.ScrapGamePage(new SearchResult("djsaklnla", "views_0669111111111",
            "https://d11111111lsoft.dmm.co.jp/detail/views_0669/"));
        Assert.Null(res);
    }


    [Fact]
    public async void ShouldNotGetScrapperResultSearchAfter()
    {
        var s0 = new GameScrapper(new XunitLogger<GameScrapper>(_testOutputHelper));
        var manager = new FanzaMetadataProvider.ScrapperManager(new List<IScrapper>() { s0 });
        var test = await manager.ScrapSearchPage("美少女万華鏡_神が造りたもうた少女たち", CancellationToken.None);

        var var0 = await manager.ScrapGamePage(test.First(), CancellationToken.None);
        Assert.NotNull(var0);
    }
}
