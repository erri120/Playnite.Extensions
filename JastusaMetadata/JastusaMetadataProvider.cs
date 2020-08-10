// /*
//     Copyright (C) 2020  erri120
// 
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <https://www.gnu.org/licenses/>.
// */

using System;
using Extensions.Common;

namespace JastusaMetadata
{
    public class JastusaMetadataProvider : AMetadataProvider<JastusaGame>
    {
        protected override string GetID()
        {
            var playniteGame = Options.GameData;

            var name = playniteGame.Name;

            if (name.IsEmpty())
            {
                if(!playniteGame.TryGetLink("Jastusa", out var jastusaLink))
                    throw new Exception("Name must not be empty!");
                name = jastusaLink.Url;
            }
            
            //they have weird links where this:
            //https://jastusa.com/games/genre/romance/kozue-s-strange-journey
            //is also https://jastusa.com/kozue-s-strange-journey
            //and https://jastusa.com/games/kozue-s-strange-journey
            
            if(!name.StartsWith(Consts.Root, StringComparison.CurrentCultureIgnoreCase))
                throw new Exception($"Name must start with {Consts.Root}!");

            if (name.EndsWith("/"))
                name = name.Substring(0, name.Length - 1);
            
            var lastSlash = name.LastIndexOf('/');
            var id = name.Substring(lastSlash);

            return id;
        }
    }
}