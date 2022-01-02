using System;
using System.Collections.Generic;
using System.Linq;
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

    public bool NeedsUpdate(TimeSpan minDistance)
    {
        return DateTime.UtcNow - LastChecked > minDistance;
    }
}
