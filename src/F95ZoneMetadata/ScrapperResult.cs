using System.Collections.Generic;

namespace F95ZoneMetadata;

public class ScrapperResult
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public List<string>? Labels { get; set; }
    public string? Version { get; set; }
    public string? Developer { get; set; }
    public List<string>? Tags { get; set; }
    public double Rating { get; set; }
    public List<string>? Images { get; set; }
}

public class ScrapperSearchResult
{
    public string? Link { get; set; }
    public string? Name { get; set; }
}
