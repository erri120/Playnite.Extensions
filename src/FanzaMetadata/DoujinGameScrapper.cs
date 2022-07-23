using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.Extensions.Logging;

namespace FanzaMetadata;

public class DoujinGameScrapper : IScrapper
{
    private readonly ILogger<DoujinGameScrapper> _logger;
    private readonly IConfiguration _configuration;

    public const string GameBaseUrl = "https://www.dmm.co.jp/dc/doujin/-/detail/=/cid=d_";

    public const string IconUrlFormat = "https://doujin-assets.dmm.co.jp/digital/game/d_{0}/d_{0}pt.jpg";

    //game category only
    private const string SearchBaseUrl =
        "https://www.dmm.co.jp/search/=/searchstr={0}/limit=30/n1=FgRCTw9VBA4GAV5NWV8I/n2=Aw1fVhQKX1ZRAlhMUlo5Uw4QXF9e/n3=AgReSwMKX1VZCFQCloTHi8SF";

    public DoujinGameScrapper(ILogger<DoujinGameScrapper> logger, HttpMessageHandler messageHandler)
    {
        _logger = logger;
        _configuration = Configuration.Default
            .WithRequesters(messageHandler)
            .WithDefaultLoader();
    }


    public DoujinGameScrapper(ILogger<DoujinGameScrapper> logger)
    {
        _logger = logger;
        var clientHandler = new HttpClientHandler();
        clientHandler.Properties.Add("User-Agent", "Playnite.Extensions");
        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Cookie("age_check_done", "1", "/", ".dmm.co.jp")
        {
            Expires = DateTime.Now + TimeSpan.FromDays(30),
            HttpOnly = true
        });

        clientHandler.CookieContainer = cookieContainer;
        _configuration = Configuration.Default.WithRequesters(clientHandler).WithDefaultLoader();
        clientHandler.UseCookies = true;
    }

    public async Task<ScrapperResult?> ScrapGamePage(SearchResult searchResult,
        CancellationToken cancellationToken = default)
    {
        return await ScrapGamePage(searchResult.Href, cancellationToken);
    }

    public async Task<ScrapperResult?> ScrapGamePage(string link, CancellationToken cancellationToken)
    {
        var id = GetGameIdFromLinks(new List<string> { link });
        if (string.IsNullOrEmpty(id)) return null;

        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(link, cancellationToken);
        if (document.StatusCode == HttpStatusCode.NotFound) return null;

        var result = new ScrapperResult
        {
            Link = link
        };

        var productTitleElement = document
            .GetElementsByClassName("productTitle__txt")
            .FirstOrDefault(elem => elem.TagName.Equals(TagNames.H1, StringComparison.OrdinalIgnoreCase));

        if (productTitleElement is not null)
        {
            var productTitleText = productTitleElement.Text();

            var prefixElements = productTitleElement.GetElementsByClassName("productTitle__txt--campaign");
            if (prefixElements.Any())
            {
                var prefixElement = prefixElements.Last();
                var prefixElementText = prefixElement.Text();
                var index = productTitleText.IndexOf(prefixElementText, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    productTitleText = productTitleText.Substring(index + prefixElementText.Length + 1);
                }
            }

            result.Title = productTitleText.Trim();
        }

        var circleNameElement = document.GetElementsByClassName("circleName__txt").FirstOrDefault();
        if (circleNameElement is not null)
        {
            result.Circle = circleNameElement.Text().Trim();
        }

        var productPreviewElement = document.GetElementsByClassName("productPreview").FirstOrDefault();
        if (productPreviewElement is not null)
        {
            var previewImages = productPreviewElement.GetElementsByClassName("productPreview__item")
                .Where(elem => elem.TagName.Equals(TagNames.Li, StringComparison.OrdinalIgnoreCase))
                .Select(elem => elem.GetElementsByClassName("fn-colorbox").FirstOrDefault())
                .Where(elem => elem is not null)
                .Where(elem => elem!.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                .Cast<IHtmlAnchorElement>()
                .Select(anchor => anchor.Href)
                .Where(href => !string.IsNullOrWhiteSpace(href))
                .ToList();

            result.PreviewImages = previewImages.Any() ? previewImages : null;
        }

        // result.PreviewImages = document.GetElementsByClassName("previewList__item")
        //     .Select(elem => elem.Children.FirstOrDefault(x => x.TagName.Equals(TagNames.Img, StringComparison.OrdinalIgnoreCase)))
        //     .Where(elem => elem is not null)
        //     .Cast<IHtmlImageElement>()
        //     .Select(img => img.Source)
        //     .Where(src => !string.IsNullOrWhiteSpace(src))
        //     .Select(src => src!)
        //     .ToList();

        var userReviewElement = document.GetElementsByClassName("userReview__item").FirstOrDefault();
        if (userReviewElement is not null)
        {
            var reviewElement = userReviewElement.GetElementsByTagName(TagNames.A)
                .Select(elem => elem.Children.FirstOrDefault(x =>
                    x.ClassName is not null && x.ClassName.StartsWith("u-common__ico--review")))
                .FirstOrDefault();

            if (reviewElement is not null)
            {
                var rating = reviewElement.ClassName! switch
                {
                    "u-common__ico--review50" => 5.0,
                    "u-common__ico--review45" => 4.5,
                    "u-common__ico--review40" => 4.0,
                    "u-common__ico--review35" => 3.5,
                    "u-common__ico--review30" => 3.0,
                    "u-common__ico--review25" => 2.5,
                    "u-common__ico--review20" => 2.0,
                    "u-common__ico--review15" => 1.5,
                    "u-common__ico--review10" => 1.0,
                    "u-common__ico--review05" => 0.5,
                    "u-common__ico--review00" => 0.0,
                    _ => double.NaN
                };

                result.Rating = rating;
            }
        }

        var informationListElements = document.GetElementsByClassName("informationList");
        if (informationListElements.Any())
        {
            foreach (var informationListElement in informationListElements)
            {
                var ttlElement = informationListElement.GetElementsByClassName("informationList__ttl").FirstOrDefault();
                if (ttlElement is null) continue;

                var ttlText = ttlElement.Text().Trim();

                var txtElement = informationListElement.GetElementsByClassName("informationList__txt").FirstOrDefault();
                var txt = txtElement?.Text().Trim();

                if (ttlText.Equals("配信開始日", StringComparison.OrdinalIgnoreCase))
                {
                    // release date
                    if (txt is null) continue;

                    // "2021/12/25 00:00"
                    var index = txt.IndexOf(' ');
                    if (index == -1) continue;

                    // "2021/12/25"
                    txt = txt.Substring(0, index);

                    if (DateTime.TryParseExact(txt, "yyyy/MM/dd", null, DateTimeStyles.None, out var releaseDate))
                    {
                        result.ReleaseDate = releaseDate;
                    }
                }
                else if (ttlText.Equals("ゲームジャンル", StringComparison.OrdinalIgnoreCase))
                {
                    // game genres, not the same as genres (this is more like a theme, eg "RPG")
                    if (txt is null) continue;

                    result.GameGenre = txt;
                }
                else if (ttlText.Equals("シリーズ", StringComparison.OrdinalIgnoreCase))
                {
                    // series
                    if (txt is null) continue;
                    if (txt.Equals("----")) continue;

                    result.Series = txt;
                }
                else if (ttlText.Equals("ジャンル", StringComparison.OrdinalIgnoreCase))
                {
                    // genres, not the same as game genre (this is more like tags)
                    var genreTagTextElements = informationListElement.GetElementsByClassName("genreTag__txt");

                    result.Genres = genreTagTextElements
                        .Select(elem => elem.Text().Trim())
                        .ToList();
                }
            }
        }

        result.IconUrl = string.Format(IconUrlFormat, id);
        return result;
    }

    public string? GetGameIdFromLinks(IEnumerable<string> links)
    {
        return links.Where(link => link.StartsWith(GameBaseUrl, StringComparison.OrdinalIgnoreCase))
            .Select(ParseLinkId)
            .FirstOrDefault(x => !string.IsNullOrEmpty(x));
    }

    public static string? ParseLinkId(string link)
    {
        if (!link.StartsWith(GameBaseUrl, StringComparison.OrdinalIgnoreCase)) return null;

        var afterIdSlashIdx = link.IndexOf("/", GameBaseUrl.Length, StringComparison.Ordinal);
        var cidStr = "cid=d_";
        var cidEndIdx = link.LastIndexOf(cidStr, StringComparison.Ordinal) + cidStr.Length;
        return link.Substring(GameBaseUrl.Length, afterIdSlashIdx - cidEndIdx);
    }


    public async Task<List<SearchResult>> ScrapSearchPage(string searchName,
        CancellationToken cancellationToken = default)
    {
        var url = string.Format(SearchBaseUrl, searchName);
        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(url, cancellationToken);

        var anchorElements = document.GetElementsByClassName("tileListImg__tmb")
            .Where(ele =>
            {
                var aEle = ele.GetElementsByTagName("a").Cast<IHtmlAnchorElement>().First();
                var title = aEle.FirstElementChild?.GetAttribute("alt");
                if (string.IsNullOrEmpty(title)) return false;

                var id = GetGameIdFromLinks(new List<string> { aEle.Href });
                return !string.IsNullOrEmpty(id);
            })
            .Select(ele =>
            {
                var aEle = ele.GetElementsByTagName("a").Cast<IHtmlAnchorElement>().First();
                var title = aEle.FirstElementChild?.GetAttribute("alt");
                var id = GetGameIdFromLinks(new List<string> { aEle.Href });
                return new SearchResult(Convert.ToString(title), Convert.ToString(id), aEle.Href);
            }).ToList();
        return anchorElements;
    }
}
