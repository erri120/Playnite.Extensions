using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Common;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using Microsoft.Extensions.Logging;
using Playnite.SDK.Plugins;

namespace FanzaMetadata;

public class GameScrapper : IScrapper
{
    private const string BaseSearchUrl = "https://dlsoft.dmm.co.jp/search?service=pcgame&searchstr=";
    private const string BaseGamePageUrl = "https://dlsoft.dmm.co.jp/detail/";
    private const string PosterUrlPattern = "https://pics.dmm.co.jp/digital/pcgame/{0}/{0}pl.jpg";
    private readonly ILogger<GameScrapper> _logger;
    private readonly IConfiguration _configuration;

    public GameScrapper(ILogger<GameScrapper> logger)
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

    public GameScrapper(ILogger<GameScrapper> logger, HttpClientHandler messageHandler)
    {
        _logger = logger;
        _configuration = Configuration.Default.WithRequesters(messageHandler).WithDefaultLoader();
        messageHandler.UseCookies = true;
    }


    public async Task<List<SearchResult>> ScrapSearchPage(string searchName,
        CancellationToken cancellationToken = default)
    {
        var url = BaseSearchUrl + Uri.EscapeUriString(searchName);
        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(url, cancellationToken);

        await Console.Out.WriteAsync(document.Title);

        return document.GetElementsByClassName("component-legacy-productTile")
            .Where(ele =>
            {
                var s = ele.GetElementsByClassName("component-legacy-productTile__detailLink")
                    .Cast<IHtmlAnchorElement>().First().Href;
                return !string.IsNullOrEmpty(s);
            })
            .Select(element =>
                {
                    var title = element.GetElementsByClassName("component-legacy-productTile__title").First().Text();
                    var href = element.GetElementsByClassName("component-legacy-productTile__detailLink")
                        .Cast<IHtmlAnchorElement>().First().Href;
                    var id = new Uri(href).Segments.Last().Replace("/", "");
                    return new SearchResult(title, id, href);
                }
            ).ToList();
    }


    public async Task<ScrapperResult?> ScrapGamePage(SearchResult searchResult,
        CancellationToken cancellationToken = default)
    {
        return await ScrapGamePage(searchResult.Href, cancellationToken);
    }

    public async Task<ScrapperResult?> ScrapGamePage(string link, CancellationToken cancellationToken)
    {
        var id = GetGameIdFromLinks(new List<string> { link });

        _logger.LogInformation("id" + id);
        if (string.IsNullOrEmpty(id)) return null;

        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(link, cancellationToken);
        if (document.StatusCode == HttpStatusCode.NotFound) return null;

        var result = new ScrapperResult
        {
            Link = link
        };
        result.Title = document.GetElementById("title")?.Text().Trim();
        result.Circle = document.GetElementsByClassName("brand").Children(".content").First().Text().Trim();

        result.PreviewImages = document.GetElementsByClassName("image-slider").Children("li").Children("img")
            .Cast<IHtmlImageElement>().Select(x => x.Source ?? "").Where(x => !string.IsNullOrEmpty(x)).ToList();

        const string ratingPrefix = "d-rating-";
        result.Rating = document.QuerySelector(".review div")!.ClassList
            .Where(className => className.StartsWith(ratingPrefix))
            .Select(className => className.Replace(ratingPrefix, ""))
            .Select(rating => double.Parse(rating) / 10D).First();

        // <tr>
        //  <td class="type-left">ダウンロード版対応OS</td>
        //  <td class="type-center">：</td>
        //  <td class="type-right"></td>
        // </tr>
        // key is type-left class text, value is type-right class element
        var productDetailDict =
            document.QuerySelectorAll(".main-area-center .container02 table tbody tr .type-left")
                .ToDictionary(ele => ele.Text().Trim(),
                    v => v.ParentElement?.GetElementsByClassName("type-right").First());

        var dateStr = productDetailDict["配信開始日"]?.Text().Trim();
        if (DateTime.TryParseExact(dateStr, "yyyy/MM/dd", null, DateTimeStyles.None, out var releaseDate))
        {
            result.ReleaseDate = releaseDate;
        }

        const string noneVal = "----";
        var gameGenre = productDetailDict["ゲームジャンル"]?.Text().Trim();
        if (!noneVal.Equals(gameGenre))
        {
            result.GameGenre = gameGenre;
        }

        var series = productDetailDict["シリーズ"]?.Text().Trim();
        if (!noneVal.Equals(series))
        {
            result.Series = series;
        }

        var tags = productDetailDict["ジャンル"]?.GetElementsByTagName("a").Select(x => x.Text().Trim()).ToList();
        result.Genres = tags;

        result.IconUrl = string.Format(PosterUrlPattern, id);
        return result;
    }

    public static string? ParseLinkId(string? link)
    {
        if (link == null) return null;
        return new Uri(link).Segments.Last().Replace("/", "");
    }

    public string? GetGameIdFromLinks(IEnumerable<string> links)
    {
        return links.Where(link =>
                link.StartsWith(BaseGamePageUrl, StringComparison.OrdinalIgnoreCase))
            .Select(ParseLinkId).Where(x => !string.IsNullOrEmpty(x)).FirstOrDefault();
    }
}
