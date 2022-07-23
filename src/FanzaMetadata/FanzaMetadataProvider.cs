using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Common;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace FanzaMetadata;

public class FanzaMetadataProvider : OnDemandMetadataProvider
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly Settings _settings;
    private readonly ILogger<FanzaMetadataProvider> _logger;

    private readonly MetadataRequestOptions _options;
    private Game Game => _options.GameData;
    private bool IsBackgroundDownload => _options.IsBackgroundDownload;

    public override List<MetadataField> AvailableFields => FanzaMetadataPlugin.Fields;

    private ScrapperResult? _result;
    private List<SearchResult>? _searchResults;
    private bool _didRun;

    public FanzaMetadataProvider(IPlayniteAPI playniteAPI, Settings settings, MetadataRequestOptions options)
    {
        _playniteAPI = playniteAPI;
        _settings = settings;
        _options = options;

        _logger = CustomLogger.GetLogger<FanzaMetadataProvider>(nameof(FanzaMetadataProvider));
    }


    private static ScrapperManager SetupScrapperManager()
    {
        var clientHandler = new HttpClientHandler();
        clientHandler.Properties.Add("User-Agent", "Playnite.Extensions");

        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Cookie("age_check_done", "1", "/", ".dmm.co.jp")
        {
            Expires = DateTime.Now + TimeSpan.FromDays(30),
            HttpOnly = true
        });

        clientHandler.UseCookies = true;
        clientHandler.CookieContainer = cookieContainer;

        var doujinGameScrapper = new DoujinGameScrapper(
            CustomLogger.GetLogger<DoujinGameScrapper>(nameof(DoujinGameScrapper)),
            clientHandler);
        var gameScrapper = new GameScrapper(CustomLogger.GetLogger<GameScrapper>(nameof(GameScrapper)), clientHandler);

        return new ScrapperManager(new List<IScrapper>() { gameScrapper, doujinGameScrapper });
    }

    private ScrapperResult? GetResult(GetMetadataFieldArgs args)
    {
        if (_didRun) return _result;
        var scrapperManager = SetupScrapperManager();

        var linksTask = TryGetMetadataFromLinks(args, scrapperManager);
        linksTask.Wait(args.CancelToken);
        var scrapperResult = linksTask.Result;
        if (scrapperResult != null)
        {
            _result = scrapperResult;
            _didRun = true;
            return _result;
        }

        if (string.IsNullOrWhiteSpace(Game.Name))
        {
            _logger.LogError("Unable to metadata cuz links and name are not available");
            _didRun = true;
            return null;
        }

        if (IsBackgroundDownload)
        {
            // background download so we just choose the first item
            var searchTask = scrapperManager.ScrapSearchPage(Game.Name, args.CancelToken);
            searchTask.Wait(args.CancelToken);

            var searchResult = searchTask.Result;
            if (!searchResult.Any())
            {
                _logger.LogError("Search return nothing for {Name}, make sure you are logged in!", Game.Name);
                _didRun = true;
                return null;
            }

            _searchResults = searchResult;
        }
        else
        {
            var item = _playniteAPI.Dialogs.ChooseItemWithSearch(
                new List<GenericItemOption>(),
                searchString =>
                {
                    var searchTask = scrapperManager.ScrapSearchPage(searchString, args.CancelToken);
                    searchTask.Wait(args.CancelToken);
                    var searchResult = searchTask.Result;
                    _searchResults = searchResult;
                    if (searchResult is null || !searchResult.Any())
                    {
                        _logger.LogError("Search return nothing, make sure you are logged in!");
                        _didRun = true;
                        return null;
                    }

                    var items = searchResult
                        .Select(x => new GenericItemOption(x.Name, x.Href))
                        .ToList();

                    return items;
                }, Game.Name, "Search Fanza");

            if (item is null)
            {
                _didRun = true;
                return null;
            }
        }

        if (_searchResults != null)
        {
            var task = scrapperManager.ScrapGamePage(_searchResults.First(), args.CancelToken);
            task.Wait(args.CancelToken);
            _result = task.Result;
        }

        _didRun = true;
        return _result;
    }

    private async Task<ScrapperResult?> TryGetMetadataFromLinks(GetMetadataFieldArgs args,
        ScrapperManager scrapperManager)
    {
        if (Game.Links == null) return null;

        foreach (var gameLink in Game.Links)
        {
            if (gameLink == null) continue;
            var res = await scrapperManager.ScrapGamePage(gameLink.Url, args.CancelToken);
            if (res != null) return res;
        }

        return null;
    }

    public class ScrapperManager
    {
        private readonly IEnumerable<IScrapper> _scrappers;

        public ScrapperManager(IEnumerable<IScrapper> scrappers)
        {
            _scrappers = scrappers;
        }

        public async Task<List<SearchResult>> ScrapSearchPage(string searchName, CancellationToken cancellationToken)
        {
            foreach (var scrapper in _scrappers)
            {
                var res = await scrapper.ScrapSearchPage(searchName, cancellationToken);
                if (res.Any()) return res;
            }

            return new List<SearchResult>();
        }


        public async Task<ScrapperResult?> ScrapGamePage(SearchResult searchResult, CancellationToken cancellationToken)
        {
            foreach (var scrapper in _scrappers)
            {
                var res = await scrapper.ScrapGamePage(searchResult, cancellationToken);
                if (res != null) return res;
            }

            return null;
        }

        public async Task<ScrapperResult?> ScrapGamePage(string link, CancellationToken cancellationToken)
        {
            var result = Uri.TryCreate(link, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            if (!result) return null;


            foreach (var scrapper in _scrappers)
            {
                var res = await scrapper.ScrapGamePage(link, cancellationToken);
                if (res != null) return res;
            }

            return null;
        }
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        return GetResult(args)?.Title ?? base.GetName(args);
    }

    public override MetadataFile GetIcon(GetMetadataFieldArgs args)
    {
        var iconUrl = GetResult(args)?.IconUrl;
        return iconUrl is null ? base.GetIcon(args) : new MetadataFile(iconUrl);
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        var circle = GetResult(args)?.Circle;
        if (circle is null) return base.GetDevelopers(args);

        var company = _playniteAPI.Database.Companies.Where(x => x.Name is not null)
            .FirstOrDefault(x => x.Name.Equals(circle, StringComparison.OrdinalIgnoreCase));
        if (company is not null) return new[] { new MetadataIdProperty(company.Id) };
        return new[] { new MetadataNameProperty(circle) };
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        var link = GetResult(args)?.Link;
        if (link is null) yield break;
        yield return new Link("Fanza", link);
    }

    public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
    {
        var series = GetResult(args)?.Series;
        if (series is null) return base.GetSeries(args);

        var item = _playniteAPI.Database.Series.Where(x => x.Name is not null)
            .FirstOrDefault(x => x.Name.Equals(series, StringComparison.OrdinalIgnoreCase));
        if (item is not null) return new[] { new MetadataIdProperty(item.Id) };
        return new[] { new MetadataNameProperty(series) };
    }

    public override string GetDescription(GetMetadataFieldArgs args)
    {
        var result = GetResult(args);
        if (result is null) return base.GetDescription(args);
        return result.Description ?? "";
    }

    public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
    {
        var res = GetResult(args);
        var adult = res?.Adult;
        if (adult == null)
        {
            return base.GetAgeRatings(args);
        }

        var ageRating = adult.Value ? "18禁" : "一般向";
        return new[] { new MetadataNameProperty(ageRating) };
    }

    public override IEnumerable<MetadataProperty> GetRegions(GetMetadataFieldArgs args)
    {
        return new[] { new MetadataNameProperty("Japan") };
    }

    private MetadataFile? SelectImage(GetMetadataFieldArgs args, string caption)
    {
        var images = GetResult(args)?.PreviewImages;
        if (images is null || !images.Any()) return null;

        if (IsBackgroundDownload)
        {
            return new MetadataFile(images.First());
        }

        var imageFileOption =
            _playniteAPI.Dialogs.ChooseImageFile(images.Select(image => new ImageFileOption(image)).ToList(), caption);
        return imageFileOption == null ? null : new MetadataFile(imageFileOption.Path);
    }

    public override MetadataFile? GetCoverImage(GetMetadataFieldArgs args)
    {
        return SelectImage(args, "Select Cover Image");
    }

    public override MetadataFile? GetBackgroundImage(GetMetadataFieldArgs args)
    {
        return SelectImage(args, "Select Background Image");
    }

    public override int? GetCommunityScore(GetMetadataFieldArgs args)
    {
        var rating = GetResult(args)?.Rating;
        return rating switch
        {
            null => base.GetCommunityScore(args),
            double.NaN => base.GetCommunityScore(args),
            _ => (int)(rating.Value / 5 * 100)
        };
    }

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        var result = GetResult(args);
        if (result is null) return base.GetReleaseDate(args);

        var releaseDate = result.ReleaseDate;
        return releaseDate.Equals(DateTime.MinValue) ? base.GetReleaseDate(args) : new ReleaseDate(releaseDate);
    }

    private IEnumerable<MetadataProperty>? GetProperties(GetMetadataFieldArgs args, PlayniteProperty currentProperty)
    {
        // Genres
        var genreProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
            _playniteAPI,
            _settings.GenreProperty,
            currentProperty,
            () => GetResult(args)?.Genres);

        // Game Genre/Theme
        var gameGenreProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
            _playniteAPI,
            _settings.GameGenreProperty,
            currentProperty,
            () =>
            {
                var gameGenre = GetResult(args)?.GameGenre;
                if (gameGenre is null) return null;
                return new[] { gameGenre };
            });

        return PlaynitePropertyHelper.MultiConcat(genreProperties, gameGenreProperties);
    }

    public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
    {
        return GetProperties(args, PlayniteProperty.Tags) ?? base.GetTags(args);
    }

    public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
    {
        return GetProperties(args, PlayniteProperty.Features) ?? base.GetFeatures(args);
    }

    public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
    {
        return GetProperties(args, PlayniteProperty.Genres) ?? base.GetGenres(args);
    }
}
