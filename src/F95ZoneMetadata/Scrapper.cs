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

        // Images
        var messageContentElements = document.GetElementsByClassName("message-content");
        if (messageContentElements.Any())
        {
            var mainMessage = messageContentElements.First();

            // images link to the thumbnail and are wrapped in anchor elements that point to the original image
            var images = mainMessage.GetElementsByTagName(TagNames.Img)
                .Where(elem => elem.ParentElement is not null && elem.ParentElement.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                .Select(elem => elem.ParentElement!)
                .Cast<IHtmlAnchorElement>()
                .Where(elem => !string.IsNullOrWhiteSpace(elem.Href))
                .Select(elem => elem.Href)
                .ToList();

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

        var span = title.AsSpan();
        var bracketStartIndex = span.IndexOf('[');
        var bracketEndIndex = span.IndexOf(']');

        if (bracketStartIndex == -1 || bracketEndIndex == -1)
        {
            return (title, null, null);
        }

        // "Corrupted Kingdoms"
        var nameSpan = span.Slice(0, bracketStartIndex - 1);

        // "v0.12.8"
        var versionSpan = span.Slice(bracketStartIndex + 1, bracketEndIndex - bracketStartIndex - 1);

        span = span.Slice(bracketEndIndex + 1);
        bracketStartIndex = span.IndexOf('[');
        bracketEndIndex = span.IndexOf(']');

        if (bracketStartIndex == -1 || bracketEndIndex == -1)
        {
            return (nameSpan.ToString(), versionSpan.ToString(), null);
        }

        // "ArcGames"
        var developerSpan = span.Slice(bracketStartIndex + 1, bracketEndIndex - bracketStartIndex - 1);

        return (nameSpan.ToString(), versionSpan.ToString(), developerSpan.ToString());
    }
}
