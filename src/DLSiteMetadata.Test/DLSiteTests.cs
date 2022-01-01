using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Contrib.HttpClient;
using Playnite.SDK.Models;
using TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace DLSiteMetadata.Test;

public class DLSiteTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DLSiteTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("RJ246037-EN.html", "RJ246037")]
    [InlineData("RJ246037-JP.html", "RJ246037")]
    public async Task TestScrapper(string file, string id)
    {
        file = Path.Combine("files", file);
        Assert.True(File.Exists(file));

        var handler = new Mock<HttpMessageHandler>();
        handler
            .SetupAnyRequest()
            .ReturnsResponse(File.ReadAllBytes(file));

        var scrapper = new Scrapper(new XunitLogger<Scrapper>(_testOutputHelper), handler.Object);
        var res = await scrapper.ScrapGamePage(id);

        Assert.NotNull(res.Title);
        Assert.NotNull(res.Categories);
        Assert.NotEmpty(res.Categories!);
        Assert.NotNull(res.Genres);
        Assert.NotEmpty(res.Genres!);
        Assert.NotNull(res.Maker);
        Assert.NotNull(res.ProductImages);
        Assert.NotEmpty(res.ProductImages!);
        Assert.NotNull(res.Illustrators);
        Assert.NotEmpty(res.Illustrators!);
        Assert.NotNull(res.MusicCreators);
        Assert.NotEmpty(res.MusicCreators!);
        Assert.NotNull(res.ScenarioWriters);
        Assert.NotEmpty(res.ScenarioWriters!);
        Assert.NotNull(res.VoiceActors);
        Assert.NotEmpty(res.VoiceActors!);
        Assert.NotNull(res.Icon);
    }

    [Theory]
    [InlineData("search-EN.html", "ONEONE1", "en_US")]
    [InlineData("search-JP.html", "ONEONE1", "ja_JP")]
    public async Task TestScrapSearchPage(string file, string term, string language)
    {
        file = Path.Combine("files", file);
        Assert.True(File.Exists(file));

        var handler = new Mock<HttpMessageHandler>();
        handler
            .SetupAnyRequest()
            .ReturnsResponse(File.ReadAllBytes(file));

        var scrapper = new Scrapper(new XunitLogger<Scrapper>(_testOutputHelper), handler.Object);
        var results = await scrapper.ScrapSearchPage(term, default, 100, language);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void TestGetIdFromGame()
    {
        Assert.Equal("RJ246037", DLSiteMetadataProvider.GetIdFromGame(new Game("https://www.dlsite.com/maniax/work/=/product_id/RJ246037.html")));
        Assert.Equal("RJ246037", DLSiteMetadataProvider.GetIdFromGame(new Game("RJ246037")));
        Assert.Equal("RJ246037", DLSiteMetadataProvider.GetIdFromGame(new Game("RJ246037")));
        Assert.Equal("RJ246037", DLSiteMetadataProvider.GetIdFromGame(new Game
        {
            Links = new ObservableCollection<Link>(new List<Link>
            {
                new("DLsite", "https://www.dlsite.com/maniax/work/=/product_id/RJ246037.html")
            })
        }));
    }
}
