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
using Extensions.Common;
using Microsoft.Extensions.Logging;

namespace DLSiteMetadata;

public class Scrapper
{
    public const string DefaultLanguage = "en_US";

    public const string SiteBaseUrl = "https://www.dlsite.com/";
    public const string ProductBaseUrl = SiteBaseUrl + "maniax/work/=/product_id/";
    public const string SearchFormatUrl = SiteBaseUrl + "maniax/fsr/=/language/jp/sex_category%5B0%5D/male/keyword/{0}/order%5B0%5D/trend/per_page/{1}/from/fs.header/?locale={2}";

    private readonly ILogger<Scrapper> _logger;
    private readonly IConfiguration _configuration;

    public Scrapper(ILogger<Scrapper> logger, HttpMessageHandler messageHandler)
    {
        _logger = logger;

        _configuration = Configuration.Default
            .WithRequesters(messageHandler)
            .WithDefaultLoader();
    }

    public async Task<ScrapperResult> ScrapGamePage(string url, CancellationToken cancellationToken = default, string language = DefaultLanguage)
    {
        if (!url.Contains("/?locale="))
        {
            url += url.EndsWith("/") ? $"?locale={language}" : $"/?locale={language}";
        }

        var context = BrowsingContext.New(_configuration);
        var document = await context.OpenAsync(url, cancellationToken);

        var res = new ScrapperResult
        {
            Link = url
        };

        // Title
        var itemThingElement = document.GetElementsByClassName("topicpath_item").LastOrDefault();
        if (itemThingElement is not null)
        {
            res.Title = itemThingElement.Children.FirstOrDefault()?.Children.FirstOrDefault()?.Text().Trim();
        }

        // Images
        var productSliderDataElement = document.GetElementsByClassName("product-slider-data").FirstOrDefault();
        if (productSliderDataElement is not null)
        {
            res.ProductImages = productSliderDataElement.Children
                .Where(x => x.TagName.Equals(TagNames.Div, StringComparison.OrdinalIgnoreCase))
                .Where(x => x.HasAttribute("data-src"))
                // .Select(x => x.GetAttribute("data-src")!.Trim().Replace(".jpg", ".webp"))
                .Select(x => x.GetAttribute("data-src")!.Trim())
                .Select(x => !x.StartsWith("//") ? x : $"https:{x}")
                .ToList();
        }

        // Maker
        var makerNameElement = document.GetElementsByClassName("maker_name").FirstOrDefault();
        var makerNameAnchorElement = (IHtmlAnchorElement?)makerNameElement?.Children.FirstOrDefault(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase));
        if (makerNameAnchorElement is not null)
        {
            res.Maker = makerNameAnchorElement.Text().Trim();
        }

        var addFollowElement = document.GetElementsByClassName("add_follow").FirstOrDefault();
        if (addFollowElement is not null)
        {
            res.Maker = addFollowElement.GetAttribute("data-follow-name");
        }

        var workOutlineTable = document.QuerySelector("#work_outline");
        if (workOutlineTable is not null)
        {
            var tableRows = workOutlineTable.Children.FirstOrDefault()?.Children;
            if (tableRows is not null && tableRows.Any())
            {
                foreach (var tableRow in tableRows)
                {
                    var headerName = tableRow.Children.FirstOrDefault(x => x.TagName.Equals(TagNames.Th, StringComparison.OrdinalIgnoreCase))?.Text().Trim();
                    var dataElement = tableRow.Children.FirstOrDefault(x => x.TagName.Equals(TagNames.Td, StringComparison.OrdinalIgnoreCase));
                    if (headerName is not null && dataElement is not null)
                    {
                        if (headerName.Equals("Release date", StringComparison.OrdinalIgnoreCase))
                        {
                            var sDate = dataElement.Text().CustomTrim();
                            if (DateTime.TryParseExact(sDate, "MMM/dd/yyyy", null, DateTimeStyles.None, out var dateReleased))
                            {
                                res.DateReleased = dateReleased;
                            }
                        }
                        else if (headerName.Equals("販売日", StringComparison.OrdinalIgnoreCase))
                        {
                            var sDate = dataElement.Text().CustomTrim();
                            if (DateTime.TryParseExact(sDate, "yyyy年MM月dd日", null, DateTimeStyles.None, out var dateReleased))
                            {
                                res.DateReleased = dateReleased;
                            }
                        }
                        else if (headerName.Equals("Update information", StringComparison.OrdinalIgnoreCase))
                        {
                            var sDate = dataElement.Text().CustomTrim().Substring(0, 11);
                            if (DateTime.TryParseExact(sDate, "MMM/dd/yyyy", null, DateTimeStyles.None, out var dateUpdated))
                            {
                                res.DateUpdated = dateUpdated;
                            }
                        }
                        else if (headerName.Equals("更新情報", StringComparison.OrdinalIgnoreCase))
                        {
                            var sDate = dataElement.Text().CustomTrim().Substring(0, 11);
                            if (DateTime.TryParseExact(sDate, "yyyy年MM月dd日", null, DateTimeStyles.None, out var dateUpdated))
                            {
                                res.DateUpdated = dateUpdated;
                            }
                        } else if (headerName.Equals("Scenario", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("シナリオ", StringComparison.OrdinalIgnoreCase))
                        {
                            res.ScenarioWriters = dataElement.Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();
                        } else if (headerName.Equals("Illustration", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("イラスト", StringComparison.OrdinalIgnoreCase))
                        {
                            res.Illustrators = dataElement.Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();
                        } else if (headerName.Equals("Voice Actor", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("声優", StringComparison.OrdinalIgnoreCase))
                        {
                            res.VoiceActors = dataElement.Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();
                        } else if (headerName.Equals("Music", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("音楽", StringComparison.OrdinalIgnoreCase))
                        {
                            res.MusicCreators = dataElement.Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();
                        } else if (headerName.Equals("Age", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("年齢指定", StringComparison.OrdinalIgnoreCase))
                        {
                            // var sAge = dataElement.Text().CustomTrim();
                            // res.Age = sAge switch
                            // {
                            //     "18+" or "18禁" => DLSiteAge.Adult,
                            //     "R-15" => DLSiteAge.RatedR,
                            //     "All ages" or "全年齢" => DLSiteAge.AllAges,
                            //     _ => DLSiteAge.Unknown
                            // };
                        } else if (headerName.Equals("Product format", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("作品形式", StringComparison.OrdinalIgnoreCase))
                        {
                            res.Categories = dataElement.Children.First().Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();

                            var additionalInfoElement = dataElement.Children.First().Children.FirstOrDefault(x => x.ClassList.Contains("additional_info"));
                            if (additionalInfoElement is not null)
                            {
                                res.Categories.Add(additionalInfoElement.Text().Replace('/', ' ').CustomTrim());
                            }
                        } else if (headerName.Equals("File format", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("ファイル形式", StringComparison.OrdinalIgnoreCase))
                        {
                            // TODO:
                        } else if (headerName.Equals("Supported languages", StringComparison.OrdinalIgnoreCase))
                        {
                            // TODO:
                        } else if (headerName.Equals("Genre", StringComparison.OrdinalIgnoreCase) ||
                                   headerName.Equals("ジャンル", StringComparison.OrdinalIgnoreCase))
                        {
                            res.Genres = dataElement.Children.First().Children
                                .Where(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase))
                                .Select(x => x.Text().CustomTrim())
                                .ToList();
                        } else if (headerName.Equals("File size", StringComparison.OrdinalIgnoreCase))
                        {
                            //"Total 4.82GB"
                            // res.FileSize = dataElement.Text().CustomTrim().Substring(6);
                        } else if (headerName.Equals("ファイル容量", StringComparison.OrdinalIgnoreCase))
                        {
                            //"総計 4.82GB"
                            // res.FileSize = dataElement.Text().CustomTrim().Substring(3);
                        }
                        else
                        {
                            _logger.LogWarning("Unknown header: \"{HeaderName}\"", headerName);
                        }
                    }
                }
            }
        }

        // https://img.dlsite.jp/modpub/images2/work/doujin/RJ247000/RJ246037_img_main.jpg
        // https://img.dlsite.jp/modpub/images2/work/doujin/RJ247000/RJ246037_img_sam_mini.jpg

        if (res.ProductImages is not null && res.ProductImages.Any())
        {
            var mainImage = res.ProductImages.FirstOrDefault(x => x.Contains("_img_main."));
            if (mainImage is not null)
            {
                var iconImage = mainImage.Replace("_img_main.", "_img_sam_mini.");
                res.Icon = iconImage;
            }
        }

        return res;
    }

    public async Task<List<SearchResult>> ScrapSearchPage(string term, CancellationToken cancellationToken = default, int maxResults = 50, string language = DefaultLanguage)
    {
        var context = BrowsingContext.New(_configuration);

        var url = string.Format(SearchFormatUrl, term, maxResults, language);
        var document = await context.OpenAsync(url, cancellationToken);

        var results = new List<SearchResult>();

        var resultListElement = document.GetElementsByClassName("n_worklist").FirstOrDefault();
        if (resultListElement is not null)
        {
            var enumerable = resultListElement.Children
                .Where(x => x.TagName.Equals(TagNames.Li, StringComparison.OrdinalIgnoreCase));

            foreach (var elem in enumerable)
            {
                var divElement = elem.GetElementsByClassName("multiline_truncate").FirstOrDefault();
                if (divElement is null) continue;

                var anchorElement = (IHtmlAnchorElement?)divElement.Children.FirstOrDefault(x => x.TagName.Equals(TagNames.A, StringComparison.OrdinalIgnoreCase));
                if (anchorElement is null) continue;

                var title = anchorElement.Title;
                var href = anchorElement.Href;

                if (title is null) continue;
                results.Add(new SearchResult(title, href));
            }
        }

        return results;
    }
}
