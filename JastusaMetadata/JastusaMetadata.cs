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
using System.Collections.Generic;
using Extensions.Common;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace JastusaMetadata
{
    public class JastusaMetadata : AMetadataPlugin<JastusaGame>
    {
        public override Guid Id { get; } = Guid.Parse("21376a20-384c-4036-ad91-51f15e3a1db7");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Links,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.ReleaseDate
        };

        public override string Name => "Jastusa";
        
        public JastusaMetadata(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
        }
        
        protected override AMetadataProvider<JastusaGame> GetProvider()
        {
            return new JastusaMetadataProvider();
        }
    }
}