using System;
using Extensions.Common;

namespace DLSiteMetadata
{
    public class DLSiteMetadataProvider : AMetadataProvider<DLSiteGame>
    {
        protected override string GetID()
        {
            var playniteGame = Options.GameData;

            var name = playniteGame.Name;

            if (name.IsEmpty())
            {
                if (!playniteGame.TryGetLink("DLSite", out var dlSiteLink))
                    throw new Exception("Name must not be empty!");
                name = dlSiteLink.Url;
            }

            var id = name;

            IDCheck:
            if (id.StartsWith("RJ", StringComparison.OrdinalIgnoreCase) ||
                id.StartsWith("RE", StringComparison.OrdinalIgnoreCase)) return id;

            if (!id.StartsWith(Consts.RootENG) && !id.StartsWith(Consts.RootJPN))
                throw new Exception($"{id} does not start with RJ/RE!");

            //https://www.dlsite.com/ecchi-eng/work/=/product_id/RE234198.html
            var root = id.StartsWith(Consts.RootENG) ? Consts.RootENG : Consts.RootJPN;
            id = id.Replace(root, "");
            //work/=/product_id/{id}.html
            id = id.Replace(".html", "").Replace("work/=/product_id/", "");
            goto IDCheck;
        }
    }
}