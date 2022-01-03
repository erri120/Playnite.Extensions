using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Common;
using Playnite.SDK.Models;

namespace F95ZoneMetadata;

public class UpdateTracking
{
    public List<GameTracking> Games { get; set; } = new();

    public GameTracking GetOrAdd(Game game)
    {
        var existing = Games.FirstOrDefault(x => x.GameId.Equals(game.Id));
        if (existing is not null) return existing;

        var newTracking = new GameTracking
        {
            GameId = game.Id
        };

        Games.Add(newTracking);
        return newTracking;
    }
}

public class GameTracking
{
    public Guid GameId { get; set; } = Guid.Empty;

    public DateTime LastChecked { get; set; } = DateTime.MinValue;

    public bool NeedsUpdate(Game game, Settings settings)
    {
        if (!settings.CheckForUpdates) return false;
        if (DateTime.UtcNow - LastChecked < settings.UpdateDistance) return false;
        if (settings.UpdateCompletedGames) return true;

        IEnumerable<DatabaseObject> enumerable = settings.LabelProperty switch
        {
            PlayniteProperty.Features => game.Features,
            PlayniteProperty.Genres => game.Genres,
            PlayniteProperty.Tags => game.Tags,
            _ => throw new ArgumentOutOfRangeException()
        };

        return !enumerable.Any(item => item.Name is not null && item.Name.Equals("Completed", StringComparison.OrdinalIgnoreCase));
    }
}
