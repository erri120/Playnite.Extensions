using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DLSiteMetadata;

public class ScrapperResult
{
    public string? Link { get; set; }
    public string? Title { get; set; }
    public List<string>? ProductImages { get; set; }
    public string? Maker { get; set; }
    public DateTime DateReleased { get; set; } = DateTime.MinValue;
    public DateTime DateUpdated { get; set; } = DateTime.MinValue;
    public string? SeriesNames { get; set; }
    public List<string>? ScenarioWriters { get; set; }
    public List<string>? Illustrators { get; set; }
    public List<string>? VoiceActors { get; set; }
    public List<string>? MusicCreators { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? Genres { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? AgeRating { get; set; }
    public int? Score { get; set; }
    public string? Icon { get; set; }
}

[DebuggerDisplay("{Title} ({Href})")]
public class SearchResult
{
    public readonly string Title;
    public readonly string Href;

    public SearchResult(string title, string href)
    {
        Title = title;
        Href = href;
    }
}
