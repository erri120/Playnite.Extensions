using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Contrib.HttpClient;
using Playnite.SDK.Models;
using Xunit;
using Xunit.Abstractions;

namespace F95ZoneMetadata.Test;

public class F95ZoneTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public F95ZoneTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData("31912")]
    [InlineData("38582")]
    public async Task TestScrapper(string id)
    {
        var file = Path.Combine("files", $"{id}.html");
        Assert.True(File.Exists(file));

        var handler = new Mock<HttpMessageHandler>();
        handler
            .SetupAnyRequest()
            .ReturnsResponse(File.ReadAllBytes(file));

        var scrapper = new Scrapper(new XunitLogger<Scrapper>(_testOutputHelper), handler.Object);
        var result = await scrapper.ScrapPage(id);
        Assert.NotNull(result);
        Assert.NotNull(result!.Developer);
        Assert.NotNull(result.Labels);
        Assert.NotEmpty(result.Labels!);
        Assert.Equal(id, result.Id);
        Assert.NotNull(result.Images);
        Assert.NotEmpty(result.Images!);
        Assert.NotNull(result.Name);
        Assert.False(double.IsNaN(result.Rating));
        Assert.NotNull(result.Tags);
        Assert.NotEmpty(result.Tags!);
        Assert.NotNull(result.Version);
    }

    [Theory]
    [InlineData("Corruption of Champions", "search-1")]
    public async Task TestScrapSearchPage(string term, string fileName)
    {
        var file = Path.Combine("files", $"{fileName}.html");
        Assert.True(File.Exists(file));

        var handler = new Mock<HttpMessageHandler>();
        handler
            .SetupAnyRequest()
            .ReturnsResponse(File.ReadAllBytes(file));

        var scrapper = new Scrapper(new XunitLogger<Scrapper>(_testOutputHelper), handler.Object);
        var results = await scrapper.ScrapSearchPage(term);
        Assert.NotEmpty(results);
        Assert.All(results, x =>
        {
            Assert.NotNull(x.Name);
            Assert.NotNull(x.Link);
        });
    }

    [Theory]
    [InlineData("Corrupted Kingdoms [v0.12.8] [ArcGames]", "Corrupted Kingdoms", "v0.12.8", "ArcGames")]
    [InlineData("Treasure of Nadia [v1.0112] [NLT Media]", "Treasure of Nadia", "v1.0112", "NLT Media")]
    public void TestTitleBreakdown(string title, string name, string version, string developer)
    {
        var res = Scrapper.TitleBreakdown(title);
        Assert.Equal(name, res.Name);
        Assert.Equal(version, res.Version);
        Assert.Equal(developer, res.Developer);
    }

    [Theory]
    [InlineData("4.50 star(s)", 4.5)]
    public void TestGetRating(string text, double rating)
    {
        var res = Scrapper.GetRating(text, out var actualRating);
        Assert.True(res);
        Assert.Equal(rating, actualRating);
    }

    [Fact]
    public void TestGetIdFromGame()
    {
        Assert.Equal("31912", F95ZoneMetadataProvider.GetIdFromGame(new Game("https://f95zone.to/threads/corrupted-kingdoms-v0-12-8-arcgames.31912/")));
        Assert.Equal("31912", F95ZoneMetadataProvider.GetIdFromGame(new Game("https://f95zone.to/threads/31912/")));
        Assert.Equal("31912", F95ZoneMetadataProvider.GetIdFromGame(new Game("F95-31912")));
        Assert.Equal("31912", F95ZoneMetadataProvider.GetIdFromGame(new Game
        {
            Links = new ObservableCollection<Link>(new List<Link>
            {
                new("F95Zone", "https://f95zone.to/threads/corrupted-kingdoms-v0-12-8-arcgames.31912/")
            })
        }));
        Assert.Equal("31912", F95ZoneMetadataProvider.GetIdFromGame(new Game
        {
            Links = new ObservableCollection<Link>(new List<Link>
            {
                new("F95Zone", "https://f95zone.to/threads/31912/")
            })
        }));
    }

    [Theory]
    [InlineData("[Flash] [Completed] Corruption of Champions [Fenoxo]", "Corruption of Champions")]
    [InlineData("[Others] Corruption of Champions II [v0.4.28] [Savin/Salamander Studios]", "Corruption of Champions II")]
    public void TestGetNameOfSearchResult(string title, string name)
    {
        Assert.Equal(name, Scrapper.GetNameOfSearchResult(title));
    }

    private static HttpClientHandler CreateHandler()
    {
        var settings = new Settings
        {
            CookieCsrf = Environment.GetEnvironmentVariable("XF_CSRF", EnvironmentVariableTarget.Process),
            CookieUser = Environment.GetEnvironmentVariable("XF_USER", EnvironmentVariableTarget.Process),
            CookieTfaTrust = Environment.GetEnvironmentVariable("XF_TFA_TRUST", EnvironmentVariableTarget.Process)
        };

        var handler = new HttpClientHandler();

        var cookieContainer = settings.CreateCookieContainer();
        Assert.NotNull(cookieContainer);

        handler.UseCookies = true;
        handler.CookieContainer = cookieContainer;

        return handler;
    }
}
