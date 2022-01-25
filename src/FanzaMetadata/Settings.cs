using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace FanzaMetadata;

public class Settings : ISettings
{
    private IPlayniteAPI? _playniteAPI;
    private Plugin? _plugin;

    public PlayniteProperty GenreProperty { get; set; } = PlayniteProperty.Genres;
    public PlayniteProperty GameGenreProperty { get; set; } = PlayniteProperty.Features;

    public Settings() { }

    public Settings(Plugin plugin, IPlayniteAPI playniteAPI)
    {
        _plugin = plugin;
        _playniteAPI = playniteAPI;

        var savedSettings = plugin.LoadPluginSettings<Settings>();
        if (savedSettings is not null)
        {
        }
    }

    private Settings? _previousSettings;

    public void BeginEdit()
    {
        _previousSettings = new Settings
        {
            GenreProperty = GenreProperty,
            GameGenreProperty = GameGenreProperty
        };
    }

    public void EndEdit()
    {
        _previousSettings = null;
        _plugin?.SavePluginSettings(this);
    }

    public void CancelEdit()
    {
        if (_previousSettings is null) return;

        GenreProperty = _previousSettings.GenreProperty;
        GameGenreProperty = _previousSettings.GameGenreProperty;
    }

    public bool VerifySettings(out List<string> errors)
    {
        errors = new List<string>();

        if (!Enum.IsDefined(typeof(PlayniteProperty), GenreProperty))
        {
            errors.Add($"Unknown value \"{GenreProperty}\"");
        }

        if (!Enum.IsDefined(typeof(PlayniteProperty), GameGenreProperty))
        {
            errors.Add($"Unknown value \"{GameGenreProperty}\"");
        }

        return !errors.Any();
    }
}
