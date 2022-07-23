using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FanzaMetadata;

public class ScrapperResult
{
    public string? Link { get; set; }

    public string? Title { get; set; }

    public string? Circle { get; set; }

    public List<string>? PreviewImages { get; set; }

    public double Rating { get; set; } = double.NaN;

    public DateTime ReleaseDate { get; set; } = DateTime.MinValue;

    public string? GameGenre { get; set; }

    public string? Series { get; set; }

    public List<string>? Genres { get; set; }

    public string? IconUrl { get; set; }

    public string? Description { get; set; }

    public bool Adult { get; set; }
}

[DebuggerDisplay("{Name} ({Id})")]
public class SearchResult
{
    public readonly string Name;
    public readonly string Id;
    public readonly string Href;

    public SearchResult(string name, string id, string href)
    {
        Name = name;
        Id = id;
        Href = href;
    }
}
