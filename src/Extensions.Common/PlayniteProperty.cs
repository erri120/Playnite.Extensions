using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace Extensions.Common;

public enum PlayniteProperty
{
    Features = 0,
    Genres = 1,
    Tags = 2
}

public static class PlaynitePropertyHelper
{
    public static IEnumerable<DatabaseObject>? GetDatabaseCollection(IPlayniteAPI playniteAPI, PlayniteProperty property)
    {
        return property switch
        {
            PlayniteProperty.Features => playniteAPI.Database.Features,
            PlayniteProperty.Genres => playniteAPI.Database.Genres,
            PlayniteProperty.Tags => playniteAPI.Database.Tags,
            _ => null
        };
    }

    public static IEnumerable<MetadataProperty>? ConvertValuesToProperties(IPlayniteAPI playniteAPI, IEnumerable<string> values, PlayniteProperty currentProperty)
    {
        var collection = GetDatabaseCollection(playniteAPI, currentProperty);
        if (collection is null) return null;

        var metadataProperties = values
            .Select(value => (value, collection.Where(x => x.Name is not null).FirstOrDefault(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))))
            .Select(tuple =>
            {
                var (value, property) = tuple;
                if (property is not null) return (MetadataProperty)new MetadataIdProperty(property.Id);
                return new MetadataNameProperty(value);
            })
            .ToList();

        return metadataProperties;
    }

    public static IEnumerable<MetadataProperty>? ConvertValuesIfPossible(IPlayniteAPI playniteAPI,
        PlayniteProperty settingsProperty, PlayniteProperty currentProperty,
        Func<IEnumerable<string>?> getValues)
    {
        if (settingsProperty != currentProperty) return null;

        var values = getValues();
        if (values is null) return null;

        var properties = ConvertValuesToProperties(playniteAPI, values, settingsProperty);
        return properties ?? null;
    }
}
