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
using System.Linq;
using System.Threading.Tasks;
using Extensions.Common;
using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace JastusaMetadata
{
    public class JastusaGame : AGame
    {
        public override string Name { get; set; }
        public override string Description { get; set; }
        public override string Link { get; set; }
        
        public List<string> Images { get; private set; }
        public string CoverImage { get; private set; }
        
        public Link Studio { get; private set; }
        public Link Publisher { get; private set; }
        public List<Link> AdditionalLinks { get; private set; }
        
        public DateTime ReleaseDate { get; private set; }
        
        public bool HasAdultRating { get; private set; }
        
        public override async Task<AGame> LoadGame()
        {
            var id = ID;

            var url = $"{Consts.Root}{id}";
            Link = url;
            
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            if(document == null)
                throw new Exception($"Could not load from {url}");

            var node = document.DocumentNode;

            #region Name
            
            var nameNode =
                node.SelectSingleNode(
                    "//div[@class='product-name mdl-cell mdl-cell--order-0 mdl-cell--12-col']");
            if (!IsNull(nameNode, "Name"))
            {
                if (TryGetInnerText(nameNode, "h1[@class='h1']", "Name", out var sName))
                {
                    Name = sName;
                    AvailableFields.Add(MetadataField.Name);
                    LogFound("Name", sName);
                }
            }
            else
            {
                return null;
            }

            #endregion

            #region Product Media

            var productMediaNode =
                node.SelectSingleNode(
                    "//div[@class='product-img-box mdl-cell mdl-cell--order-2 mdl-cell--order-1-tablet mdl-cell--order-1-desktop mdl-cell--12-col mdl-cell--7-col-tablet mdl-cell--8-col-desktop']");
            if (!IsNull(productMediaNode, "Product Media"))
            {
                #region Images

                var imageMediaNode =
                    productMediaNode.SelectSingleNode(productMediaNode.XPath +
                                                      "/div[@class='slick-wrapper']/div[@class='slick-media']");
                if (!IsNull(imageMediaNode, "Image Media"))
                {
                    var imageNodes =
                        imageMediaNode.SelectNodes(imageMediaNode.XPath + "/div[@class='item product-image']/img");
                    if (!IsNullOrEmpty(imageNodes, "Image Nodes"))
                    {
                        var temp = imageNodes.Select(x =>
                        {
                            var dataLazy = x.GetValue("data-lazy");
                            return dataLazy;
                        }).NotNull().ToList();

                        if (temp.Count > 0)
                        {
                            Images = temp;
                            AvailableFields.Add(MetadataField.BackgroundImage);
                            LogFound("Images", Images);
                        }
                    }
                }

                #endregion

                #region Description

                var descriptionNode = productMediaNode.SelectSingleNode(
                    productMediaNode.XPath +
                    "/div[@class='product-collateral mdl-grid']/div[@class='mdl-cell mdl-cell--12-col']/div[@class='mdl-tabs mdl-js-tabs']/div[@id='tab-description']");
                if (!IsNull(descriptionNode, "Description"))
                {
                    //TODO: clean this up, inner HTML is kinda funky
                    var innerHtml = descriptionNode.InnerHtml.DecodeString();
                    if (!innerHtml.IsEmpty())
                    {
                        Description = innerHtml;
                        AvailableFields.Add(MetadataField.Description);
                        LogFound("Description", null);
                    }
                }
                
                #endregion
            }

            #endregion

            #region Product Details

            var productDetailsNode =
                node.SelectSingleNode(
                    "//div[@class='product-shop mdl-cell mdl-cell--order-1 mdl-cell--order-2-tablet mdl-cell--order-2-desktop mdl-cell--12-col mdl-cell--5-col-tablet mdl-cell--4-col-desktop']");
            if (!IsNull(productDetailsNode, "Product Details"))
            {
                #region Cover Image

                var coverImageNode = productDetailsNode.SelectSingleNode(
                    productDetailsNode.XPath +
                    "/div[@class='product-shop-inner']/div[@class='product-package-img']/img[@class='product-image-boxart']");
                if (!IsNull(coverImageNode, "Cover Image"))
                {
                    var src = coverImageNode.GetValue("src");
                    if (!src.IsEmpty())
                    {
                        CoverImage = src;
                        AvailableFields.Add(MetadataField.CoverImage);
                        LogFound("Cover Image", src);
                    }
                }

                #endregion

                #region Game Info

                var gameInfoNode =
                    productDetailsNode.SelectSingleNode(productDetailsNode.XPath +
                                                        "/div[@class='product-shop-inner additional-info']");
                if (!IsNull(gameInfoNode, "Game Info"))
                {
                    #region Studio

                    var studioNode = gameInfoNode.SelectSingleNode(
                        gameInfoNode.XPath + "//div[@class='rTableRow studio']/div[@class='rTableCell data']/a");
                    if (!IsNull(studioNode, "Studio"))
                    {
                        var href = studioNode.GetValue("href");
                        var name = studioNode.InnerText.DecodeString();

                        if (!href.IsEmpty() && !name.IsEmpty())
                        {
                            var link = new Link(name, href);
                            AvailableFields.Add(MetadataField.Developers);
                            Studio = link;
                            LogFound("Developer", $"{name}: {href}");
                        }
                    }

                    #endregion

                    #region Publisher

                    var publisherNode = gameInfoNode.SelectSingleNode(
                        gameInfoNode.XPath + "//div[@class='rTableRow publisher']/div[@class='rTableCell data']/a");
                    if (!IsNull(publisherNode, "Studio"))
                    {
                        var href = publisherNode.GetValue("href");
                        var name = publisherNode.InnerText.DecodeString();

                        if (!href.IsEmpty() && !name.IsEmpty())
                        {
                            var link = new Link(name, href);
                            AvailableFields.Add(MetadataField.Publishers);
                            Publisher = link;
                            LogFound("Publisher", $"{name}: {href}");
                        }
                    }

                    #endregion

                    #region Release Date

                    var releaseDateNode = gameInfoNode.SelectSingleNode(
                        gameInfoNode.XPath + "//div[@class='rTableRow release-date']/div[@class='rTableCell data']");
                    if (!IsNull(releaseDateNode, "Release Date"))
                    {
                        var sDate = releaseDateNode.InnerText.DecodeString();
                        if (!sDate.IsEmpty())
                        {
                            if (DateTime.TryParse(sDate, out var releaseDate))
                            {
                                ReleaseDate = releaseDate;
                                AvailableFields.Add(MetadataField.ReleaseDate);
                                LogFound("Release Date", sDate);
                            }
                        }
                    }

                    var adultContentNode = gameInfoNode.SelectSingleNode(gameInfoNode.XPath + "//div[@class='rTableRow product-content adult']");
                    if (!IsNull(adultContentNode, "Adult Content Rating"))
                    {
                        HasAdultRating = true;
                        AvailableFields.Add(MetadataField.AgeRating);
                        LogFound("Adult Content Rating", null);
                    }

                    #endregion
                }

                #endregion

                #region Game Additional Meta

                var gameMetaNode = productDetailsNode.SelectSingleNode(
                    productDetailsNode.XPath +
                    "/div[@class='product-shop-inner meta mdl-cell--hide-phone']/div[@class='rTable']");
                if (!IsNull(gameMetaNode, "Game Meta"))
                {
                    var linkNodes = gameMetaNode.SelectNodes(
                        gameMetaNode.XPath + "/div[@class='rTableRow links']/div[@class='rTableCell data']/a");
                    if (!IsNullOrEmpty(linkNodes, "Links"))
                    {
                        List<Link> temp = linkNodes.Select(x =>
                        {
                            var href = x.GetValue("href");
                            var name = x.InnerText.DecodeString();

                            if (!href.IsEmpty() && !name.IsEmpty())
                            {
                                return new Link(name, href);
                            }
                            else
                            {
                                return null;
                            }
                        }).NotNull().ToList();

                        if (temp.Count > 0)
                        {
                            AdditionalLinks = temp;
                            LogFound("Additional Links", temp);
                        }
                    }
                }

                #endregion
            }
            
            #endregion
            
            return this;
        }

        public override MetadataFile GetCoverImage(IDialogsFactory dialogsAPI)
        {
            return new MetadataFile(CoverImage);
        }

        public override MetadataFile GetBackgroundImage(IDialogsFactory dialogsAPI)
        {
            List<ImageFileOption> options = Images.Select(x => new ImageFileOption(x)).ToList();
            var option = dialogsAPI.ChooseImageFile(options, "Select Background Image");
            if (option == null)
                return null;
            
            var file = new MetadataFile(option.Path);
            return file;
        }

        public override DateTime GetReleaseDate()
        {
            return ReleaseDate;
        }
        
        public override List<Link> GetLinks()
        {
            var list = new List<Link>
            {
                new Link("Jastusa", Link)
            };
            if (Studio != null)
                list.Add(Studio);
            if (Publisher != null)
                list.Add(Publisher);
            if (AdditionalLinks != null && AdditionalLinks.Count > 0)
                list.AddRange(AdditionalLinks);

            return list;
        }

        public override List<string> GetDevelopers()
        {
            return new List<string> {Studio.Name};
        }
        
        public override List<string> GetPublishers()
        {
            return new List<string> {Publisher.Name};
        }
        
        public override string GetAgeRating()
        {
            return HasAdultRating ? AgeRatingAdult : throw new NotImplementedException();
        }
        
        #region Not Implemented

        public override int GetCriticScore()
        {
            throw new NotImplementedException();
        }
        
        public override List<string> GetGenres()
        {
            throw new NotImplementedException();
        }

        public override int GetCommunityScore()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetFeatures()
        {
            throw new NotImplementedException();
        }

        public override MetadataFile GetIcon()
        {
            throw new NotImplementedException();
        }
        public override List<string> GetTags()
        {
            throw new NotImplementedException();
        }

        public override string GetSeries()
        {
            throw new NotImplementedException();
        }

        public override string GetPlatform()
        {
            throw new NotImplementedException();
        }

        public override string GetRegion()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}