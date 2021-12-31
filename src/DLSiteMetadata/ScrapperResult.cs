using System;
using System.Collections.Generic;

namespace DLSiteMetadata;

public class ScrapperResult
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public List<string>? ProductImages { get; set; }
    public string? Maker { get; set; }
    public DateTime DateReleased { get; set; } = DateTime.MinValue;
    public DateTime DateUpdated { get; set; } = DateTime.MinValue;
    public List<string>? ScenarioWriters { get; set; }
    public List<string>? Illustrators { get; set; }
    public List<string>? VoiceActors { get; set; }
    public List<string>? MusicCreators { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? Genres { get; set; }
    public string? Icon { get; set; }
}
