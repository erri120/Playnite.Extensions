using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

    public FanzaMetadataProvider(IPlayniteAPI playniteAPI, Settings settings, MetadataRequestOptions options)
    {
        _playniteAPI = playniteAPI;
        _settings = settings;
        _options = options;

        _logger = CustomLogger.GetLogger<FanzaMetadataProvider>(nameof(FanzaMetadataProvider));
    }

    private ScrapperResult? _result;
    private bool _didRun;

    public static string? GetIdFromLink(string link)
    {
        if (!link.StartsWith(Scrapper.GameBaseUrl, StringComparison.OrdinalIgnoreCase)) return null;

        var id = link.Substring(Scrapper.GameBaseUrl.Length);
        var index = id.IndexOf('/');
        return index == -1 ? id : id.Substring(0, index);
    }

    public static string? GetIdFromGame(Game game)
    {
        if (game.Name is not null)
        {
            var id = GetIdFromLink(game.Name);
            if (id is not null) return id;
        }

        var link = game.Links?.FirstOrDefault(link => link.Name.Equals("Fanza", StringComparison.OrdinalIgnoreCase));
        if (link is not null && !string.IsNullOrWhiteSpace(link.Url))
        {
            return GetIdFromLink(link.Url);
        }

        return null;
    }

    public static Scrapper SetupScrapper()
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

        var scrapper = new Scrapper(CustomLogger.GetLogger<Scrapper>(nameof(Scrapper)), clientHandler);
        return scrapper;
    }

    private ScrapperResult? GetResult(GetMetadataFieldArgs args)
    {
        if (_didRun) return _result;

        var scrapper = SetupScrapper();

        var id = GetIdFromGame(Game);
        if (id is null)
        {
            if (string.IsNullOrWhiteSpace(Game.Name))
            {
                _logger.LogError("Unable to get Id from Game and Name is null or whitespace!");
                _didRun = true;
                return null;
            }

            if (IsBackgroundDownload)
            {
                // background download so we just choose the first item

                var searchTask = scrapper.ScrapSearchPage(Game.Name, args.CancelToken);
                searchTask.Wait(args.CancelToken);

                var searchResult = searchTask.Result;
                if (searchResult is null || !searchResult.Any())
                {
                    _logger.LogError("Search return nothing for {Name}, make sure you are logged in!", Game.Name);
                    _didRun = true;
                    return null;
                }

                id = searchResult.First().Id;
            }
            else
            {
                var item = _playniteAPI.Dialogs.ChooseItemWithSearch(
                    new List<GenericItemOption>(),
                    searchString =>
                    {
                        var searchTask = scrapper.ScrapSearchPage(searchString, args.CancelToken);
                        searchTask.Wait(args.CancelToken);

                        var searchResult = searchTask.Result;
                        if (searchResult is null || !searchResult.Any())
                        {
                            _logger.LogError("Search return nothing, make sure you are logged in!");
                            _didRun = true;
                            return null;
                        }

                        var items = searchResult
                            .Select(x => new GenericItemOption(x.Name, $"{Scrapper.GameBaseUrl}{x.Id}"))
                            .ToList();

                        return items;
                    }, Game.Name, "Search Fanza");

                if (item is null)
                {
                    _didRun = true;
                    return null;
                }

                var link = item.Description;
                id = GetIdFromLink(link ?? string.Empty);

                if (id is null)
                {
                    throw new NotImplementedException();
                }
            }
        }

        var task = scrapper.ScrapGamePage(id, args.CancelToken);
        task.Wait(args.CancelToken);
        _result = task.Result;
        _didRun = true;

        return _result;
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

        var company = _playniteAPI.Database.Companies.Where(x => x.Name is not null).FirstOrDefault(x => x.Name.Equals(circle, StringComparison.OrdinalIgnoreCase));
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

        var item = _playniteAPI.Database.Series.Where(x => x.Name is not null).FirstOrDefault(x => x.Name.Equals(series, StringComparison.OrdinalIgnoreCase));
        if (item is not null) return new[] { new MetadataIdProperty(item.Id) };
        return new[] { new MetadataNameProperty(series) };
    }

    private MetadataFile? SelectImage(GetMetadataFieldArgs args, string caption)
    {
        var images = GetResult(args)?.PreviewImages;
        if (images is null || !images.Any()) return null;

        if (IsBackgroundDownload)
        {
            return new MetadataFile(images.First());
        }

        var imageFileOption = _playniteAPI.Dialogs.ChooseImageFile(images.Select(image => new ImageFileOption(image)).ToList(), caption);
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
