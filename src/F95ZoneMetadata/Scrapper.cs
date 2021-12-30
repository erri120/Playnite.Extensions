using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;

namespace F95ZoneMetadata;

public class Scrapper
{
    private const string CoverLinkPrefix = "https://f95zone.to/data/covers";
    private const string ImageLinkPrefix = "https://attachments.f95zone.to/";

    public const string DefaultBaseUrl = "https://f95zone.to/threads/";
    private readonly string _baseUrl;

    private readonly ILogger<Scrapper> _logger;
    private readonly IConfiguration _configuration;

    public Scrapper(ILogger<Scrapper> logger, HttpMessageHandler messageHandler, string baseUrl = DefaultBaseUrl)
    {
        _logger = logger;
        _baseUrl = baseUrl;

        _configuration = Configuration.Default
            .WithRequesters(messageHandler)
            .WithDefaultLoader();
    }

    public async Task<ScrapperResult?> ScrapPage(string id, CancellationToken cancellationToken = default)
    {
        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(_baseUrl + id, cancellationToken);

        var pageContentElement = document.GetElementsByClassName("pageContent").FirstOrDefault();
        if (pageContentElement is null)
        {
            _logger.LogError("Unable to find Element with class \"pageContent\"");
            return null;
        }

        var result = new ScrapperResult
        {
            Id = id
        };

        // Title
        var titleElement = document.GetElementsByClassName("p-title-value").FirstOrDefault();
        if (titleElement is not null)
        {
            var labels = titleElement
                .GetElementsByClassName("labelLink")
                .Select(elem => elem.Text().Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text =>
                {
                    var bracketStartIndex = text.IndexOf('[');
                    var bracketEndIndex = text.IndexOf(']');

                    return text.Substring(bracketStartIndex + 1, bracketEndIndex - bracketStartIndex - 1);
                })
                .ToList();

            var title = titleElement.Text().Trim();
            if (labels.Any())
            {
                var lastLabel = labels.Last();
                var labelIndex = title.IndexOf(lastLabel, StringComparison.OrdinalIgnoreCase);

                if (labelIndex != -1)
                {
                    title = title.Substring(labelIndex + lastLabel.Length + 1).Trim();
                }
            }

            var (name, version, developer) = TitleBreakdown(title);
            result.Name = name;
            result.Version = version;
            result.Developer = developer;

            result.Labels = labels.Any() ? labels : null;
        }
        else
        {
            _logger.LogWarning("Unable to find Element with class \"p-title-value\"");
        }

        // Tags
        var tagItemElements = document.GetElementsByClassName("tagItem")
            .Where(elem => elem.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (tagItemElements.Any())
        {
            var tags = tagItemElements
                .Select(elem => elem.Text())
                .Where(t => t is not null && !string.IsNullOrWhiteSpace(t))
                .ToList();
            result.Tags = tags.Any() ? tags : null;
        }
        else
        {
            _logger.LogWarning("Unable to find Elements with class \"tagItem\"");
        }

        // Rating
        var selectRatingElement = (IHtmlSelectElement?)document.GetElementsByName("rating").FirstOrDefault(elem => elem.TagName.Equals(TagNames.Select, StringComparison.OrdinalIgnoreCase));
        if (selectRatingElement is not null)
        {
            if (selectRatingElement.Dataset.Any(x => x.Key.Equals("initial-rating", StringComparison.OrdinalIgnoreCase)))
            {
                var kv = selectRatingElement.Dataset.FirstOrDefault(x => x.Key.Equals("initial-rating", StringComparison.OrdinalIgnoreCase));
                if (double.TryParse(kv.Value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, out var rating))
                {
                    result.Rating = rating;
                }
                else
                {
                    _logger.LogWarning("Unable parse \"{Value}\" as double", kv.Value);
                }
            }
            else
            {
                _logger.LogWarning("Element with name \"rating\" does not have a data value with the name \"initial-rating\"");
            }
        }
        else
        {
            _logger.LogWarning("Unable to find Element with name \"rating\" using fallback, make sure you are logged in");

            var ratingElement = document.GetElementsByClassName("bratr-rating").FirstOrDefault();
            if (ratingElement is not null)
            {
                var titleAttribute = ratingElement.GetAttribute("title");
                if (titleAttribute is not null)
                {
                    if (!GetRating(titleAttribute, out var rating))
                    {
                        _logger.LogWarning("Unable to get convert \"{RatingText}\" to a rating", titleAttribute);
                    }
                    else
                    {
                        result.Rating = rating;
                    }
                }
                else
                {
                    _logger.LogWarning("Rating Element does not have a \"title\" Attribute!");
                }
            }
            else
            {
                _logger.LogWarning("Unable to find Element with class \"bratr-rating\"");
            }
        }

        // Images
        var messageBodyElements = document.GetElementsByClassName("message-body");
        if (messageBodyElements.Any())
        {
            var mainMessage = messageBodyElements.First();

            var images = new List<string>();
            var imageElements = mainMessage.GetElementsByTagName(TagNames.Img);
            foreach (var elem in imageElements)
            {
                var imageElement = (IHtmlImageElement)elem;
                if (imageElement.Source is null) continue;
                if (!imageElement.Source.StartsWith(ImageLinkPrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var anchorElement = (IHtmlAnchorElement?)(
                    elem.ParentElement!.TagName.Equals(TagNames.NoScript, StringComparison.OrdinalIgnoreCase)
                        ? elem.ParentElement!.ParentElement!.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase)
                            ? elem.ParentElement.ParentElement
                            : null
                        : elem.ParentElement.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase)
                            ? elem.ParentElement
                            : null);

                if (anchorElement is not null)
                {
                    images.Add(anchorElement.Href.StartsWith(ImageLinkPrefix) ? anchorElement.Href : imageElement.Source);
                }
                else
                {
                    images.Add(imageElement.Source);
                }
            }

            result.Images = images.Any() ? images : null;
        }
        else
        {
            _logger.LogWarning("Unable to find Elements with class \"message-content\"");
        }

        // cover image
        var openGraphImageElement = document.Head?.GetElementsByTagName(TagNames.Meta)
            .Cast<IHtmlMetaElement>()
            .FirstOrDefault(elem => elem.GetAttribute("property") == "og:image");

        if (openGraphImageElement is not null)
        {
            var content = openGraphImageElement.Content;
            if (content is not null && !string.IsNullOrWhiteSpace(content))
            {
                if (content.StartsWith(CoverLinkPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    result.Images ??= new List<string>();
                    result.Images.Insert(0, content);
                }
            }
        }

        return result;
    }

    public static bool GetRating(string text, out double rating)
    {
        rating = double.NaN;

        var spaceIndex = text.IndexOf(' ');
        if (spaceIndex == -1) return false;

        var sDouble = text.Substring(0, spaceIndex);
        return double.TryParse(sDouble, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowDecimalPoint, null, out rating);
    }

    public static (string? Name, string? Version, string? Developer) TitleBreakdown(string title)
    {
        if (title.Equals(string.Empty)) return default;

        // "Corrupted Kingdoms [v0.12.8] [ArcGames]"

        var span = title.AsSpan().Trim();
        var bracketStartIndex = span.IndexOf('[');
        var bracketEndIndex = span.IndexOf(']');

        if (bracketStartIndex == -1 || bracketEndIndex == -1)
        {
            return (title, null, null);
        }

        // "Corrupted Kingdoms"
        var nameSpan = span.Slice(0, bracketStartIndex - 1).Trim();

        // "v0.12.8"
        var versionSpan = span.Slice(bracketStartIndex + 1, bracketEndIndex - bracketStartIndex - 1).Trim();

        span = span.Slice(bracketEndIndex + 1);
        bracketStartIndex = span.IndexOf('[');
        bracketEndIndex = span.IndexOf(']');

        if (bracketStartIndex == -1 || bracketEndIndex == -1)
        {
            return (nameSpan.ToString(), versionSpan.ToString(), null);
        }

        // "ArcGames"
        var developerSpan = span.Slice(bracketStartIndex + 1, bracketEndIndex - bracketStartIndex - 1).Trim();

        return (nameSpan.ToString(), versionSpan.ToString(), developerSpan.ToString());
    }
}
