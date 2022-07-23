using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK.Plugins;

namespace FanzaMetadata;

public interface IScrapper
{
    Task<List<SearchResult>> ScrapSearchPage(string searchName, CancellationToken cancellationToken);

    Task<ScrapperResult?> ScrapGamePage(SearchResult result, CancellationToken cancellationToken);

    Task<ScrapperResult?> ScrapGamePage(string link, CancellationToken cancellationToken);

    string? GetGameIdFromLinks(IEnumerable<string> links);
}
