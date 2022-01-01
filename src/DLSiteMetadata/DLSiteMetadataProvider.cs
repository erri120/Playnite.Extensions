using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Extensions.Common;
using Microsoft.Extensions.Logging;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace DLSiteMetadata;

public class DLSiteMetadataProvider : OnDemandMetadataProvider
{
    private readonly IPlayniteAPI _playniteAPI;
    private readonly Settings _settings;
    private readonly ILogger<DLSiteMetadataProvider> _logger;

    private readonly MetadataRequestOptions _options;
    private Game Game => _options.GameData;
    private bool IsBackgroundDownload => _options.IsBackgroundDownload;

    // useless
    public override List<MetadataField> AvailableFields { get; } = new();

    public DLSiteMetadataProvider(IPlayniteAPI playniteAPI, Settings settings, MetadataRequestOptions options)
    {
        _playniteAPI = playniteAPI;
        _settings = settings;
        _options = options;

        _logger = CustomLogger.GetLogger<DLSiteMetadataProvider>(nameof(DLSiteMetadataProvider));
    }

    private ScrapperResult? _result;
    private bool _didRun;

    private static string? GetIdFromLink(string link)
    {
        if (!link.StartsWith(Scrapper.ProductBaseUrl, StringComparison.OrdinalIgnoreCase)) return null;

        // https://www.dlsite.com/maniax/work/=/product_id/RJ246037.html

        var gameId = link.Substring(Scrapper.ProductBaseUrl.Length);

        var dotIndex = gameId.IndexOf('.');
        return dotIndex == -1 ? gameId : gameId.Substring(0, gameId.IndexOf('.'));
    }

    public static string? GetIdFromGame(Game game)
    {
        if (game.Name is not null)
        {
            {
                var gameId = GetIdFromLink(game.Name);
                if (gameId is not null) return gameId;
            }

            if (game.Name.StartsWith("RJ", StringComparison.OrdinalIgnoreCase) || game.Name.StartsWith("RE", StringComparison.OrdinalIgnoreCase))
            {
                return game.Name;
            }
        }

        var dlSiteLink = game.Links?.FirstOrDefault(link => link.Name.Equals("DLsite", StringComparison.OrdinalIgnoreCase));
        if (dlSiteLink is not null && !string.IsNullOrWhiteSpace(dlSiteLink.Url))
        {
            return GetIdFromLink(dlSiteLink.Url);
        }

        return null;
    }

    private ScrapperResult? GetResult(GetMetadataFieldArgs args)
    {
        if (_didRun) return _result;

        var scrapper = new Scrapper(CustomLogger.GetLogger<Scrapper>(nameof(Scrapper)), new HttpClientHandler());

        var id = GetIdFromGame(Game);
        if (id is null)
        {
            if (IsBackgroundDownload)
            {
                // background download so we just choose the first item

                var searchTask = scrapper.ScrapSearchPage(Game.Name, args.CancelToken, _settings.MaxSearchResults, _settings.PreferredLanguage ?? Scrapper.DefaultLanguage);
                searchTask.Wait(args.CancelToken);

                var searchResult = searchTask.Result;
                if (searchResult is null || !searchResult.Any())
                {
                    _logger.LogError("Search return nothing for {Name}", Game.Name);
                    return null;
                }

                id = GetIdFromLink(searchResult.First().Title);
                if (id is null)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                var item = _playniteAPI.Dialogs.ChooseItemWithSearch(
                    new List<GenericItemOption>(),
                    searchString =>
                    {
                        var searchTask = scrapper.ScrapSearchPage(searchString, args.CancelToken, _settings.MaxSearchResults, _settings.PreferredLanguage ?? Scrapper.DefaultLanguage);
                        searchTask.Wait(args.CancelToken);

                        var searchResult = searchTask.Result;
                        if (searchResult is null || !searchResult.Any())
                        {
                            _logger.LogError("Search return nothing for {Name}", searchString);
                            return null;
                        }

                        var items = searchResult
                            .Select(x => new GenericItemOption(x.Title, x.Href))
                            .ToList();

                        return items;
                    }, Game.Name, "Search DLsite");

                var link = item.Description;
                id = GetIdFromLink(link ?? string.Empty);

                if (id is null)
                {
                    throw new NotImplementedException();
                }
            }
        }

        var task = scrapper.ScrapGamePage(id, args.CancelToken, _settings.PreferredLanguage ?? Scrapper.DefaultLanguage);
        task.Wait(args.CancelToken);
        _result = task.Result;
        _didRun = true;

        return _result;
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        return GetResult(args)?.Title ?? base.GetName(args);
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        var result = GetResult(args);
        if (result is null) return base.GetDevelopers(args);

        var staff = new List<string>();
        if (result.Maker is not null)
        {
            staff.Add(result.Maker);
        }

        if (result.Illustrators is not null && _settings.IncludeIllustrators)
        {
            staff.AddRange(result.Illustrators);
        }

        if (result.MusicCreators is not null && _settings.IncludeMusicCreators)
        {
            staff.AddRange(result.MusicCreators);
        }

        if (result.ScenarioWriters is not null && _settings.IncludeScenarioWriters)
        {
            staff.AddRange(result.ScenarioWriters);
        }

        if (result.VoiceActors is not null && _settings.IncludeVoiceActors)
        {
            staff.AddRange(result.VoiceActors);
        }

        var developers = staff
            .Select(name => (name, _playniteAPI.Database.Companies.Where(x => x.Name is not null).FirstOrDefault(company => company.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
            .Select(tuple =>
            {
                var (name, company) = tuple;
                if (company is not null) return (MetadataProperty)new MetadataIdProperty(company.Id);
                return new MetadataNameProperty(name);
            })
            .ToList();

        return developers;
    }

    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        var id = GetResult(args)?.Id;
        if (id is null) yield break;

        yield return new Link("DLsite", $"{Scrapper.ProductBaseUrl}{id}.html");
    }

    private MetadataFile? SelectImage(GetMetadataFieldArgs args, string caption)
    {
        var images = GetResult(args)?.ProductImages;
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

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        var result = GetResult(args);
        if (result is null) return base.GetReleaseDate(args);

        var releaseDate = result.DateReleased;
        return releaseDate.Equals(DateTime.MinValue) ? base.GetReleaseDate(args) : new ReleaseDate(releaseDate);
    }

    private IEnumerable<MetadataProperty>? GetProperties(GetMetadataFieldArgs args, PlayniteProperty currentProperty)
    {
        // Categories
        var categoryProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
            _playniteAPI,
            _settings.CategoryProperty,
            currentProperty,
            () => GetResult(args)?.Categories);

        if (categoryProperties is not null) return categoryProperties;

        // Genres
        var genreProperties = PlaynitePropertyHelper.ConvertValuesIfPossible(
            _playniteAPI,
            _settings.GenreProperty,
            currentProperty,
            () => GetResult(args)?.Genres);

        if (genreProperties is not null) return genreProperties;

        // Default
        return null;
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

    public override MetadataFile GetIcon(GetMetadataFieldArgs args)
    {
        var icon = GetResult(args)?.Icon;
        return icon is null ? base.GetIcon(args) : new MetadataFile(icon);
    }
}
