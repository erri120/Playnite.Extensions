using System;
using System.Text.RegularExpressions;
using Extensions.Common;

namespace F95ZoneMetadata
{
    public class F95ZoneMetadataProvider : AMetadataProvider<F95ZoneGame>
    {
        private static readonly Regex URLRegex = new Regex(@"https:\/\/f95zone.to\/threads\/(?<id>[\w-]*.\d*)\/?");

        protected override string GetID()
        {
            var playniteGame = Options.GameData;

            var name = playniteGame.Name;

            if (name.IsEmpty())
            {
                if (!playniteGame.TryGetLink("F95Zone", out var link))
                    throw new Exception("Name must not be empty!");
                name = link.Url;
            }

            var match = URLRegex.Match(name);
            if (!match.Success)
            {
                if (!playniteGame.TryGetLink("F95Zone", out var link))
                    throw new Exception("Name must not be empty!");
                name = link.Url;

                match = URLRegex.Match(name);
                if(!match.Success)
                    throw new Exception($"{name} is not a valid link!");
            }

            if(!match.Groups["id"].Success)
                throw new Exception($"Could not group match id for {name}");

            var id = match.Groups["id"].Value;
            if(id.IsEmpty())
                throw new Exception($"ID for {name} is empty!");

            return id;
        }
    }
}