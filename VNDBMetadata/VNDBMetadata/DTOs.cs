using System;
using System.Collections.Generic;
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable NotAccessedField.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CommentTypo

namespace VNDBMetadata
{
    //API Reference: https://vndb.org/d11

    public class Login
    {
        /// <summary>
        /// Must be 1
        /// </summary>
        public int protocol;
        /// <summary>
        /// A string identifying the client application. Between the 3 and 50 characters, must contain only alphanumeric ASCII characters, space, underscore and hyphens. When writing a client, think of a funny (unique) name and hardcode it into your application. 
        /// </summary>
        public string client;
        /// <summary>
        /// A number or string indicating the software version of the client. 
        /// </summary>
        public string clientver;
        /// <summary>
        /// (optional) String containing the username of the person using the client. 
        /// </summary>
        public string username;
        /// <summary>
        /// (optional) String, password of that user in plain text. 
        /// </summary>
        public string password;
    }

    public class Result<T>
    {
        public List<T> items;
        public bool more;
        public int num;
    }

    public class GetVN
    {
        /// <summary>
        /// Visual novel ID
        /// </summary>
        public int id;
        /// <summary>
        /// Main title
        /// </summary>
        public string title;
        /// <summary>
        /// Original/official title.
        /// </summary>
        public string original;
        /// <summary>
        /// Date of the first release.
        /// </summary>
        public string released;
        /// <summary>
        /// Can be an empty array when nothing has been released yet.
        /// </summary>
        public List<string> languages;
        /// <summary>
        /// Language(s) of the first release. Can be an empty array.
        /// </summary>
        public List<string> orig_lang;
        /// <summary>
        /// Can be an empty array when unknown or nothing has been released yet.
        /// </summary>
        public List<string> platforms;
        /// <summary>
        /// Aliases, separated by newlines.
        /// </summary>
        public string aliases;
        /// <summary>
        /// Length of the game, 1-5
        /// </summary>
        public int length;
        /// <summary>
        /// Description of the VN. Can include formatting codes as described in https://vndb.org/d9#3.
        /// </summary>
        public string description;
        /// <summary>
        /// Links
        /// </summary>
        public Links links;
        /// <summary>
        /// HTTP link to the VN image.
        /// </summary>
        public string image;
        /// <summary>
        /// (deprecated) Whether the VN image is flagged as NSFW or not.
        /// </summary>
        public bool image_nsfw;
        /// <summary>
        /// Image flagging summary of the main VN image
        /// </summary>
        public ImageFlagging image_flagging;
        /// <summary>
        /// (Possibly empty) list of anime related to the VN
        /// </summary>
        public List<Anime> anime;
        /// <summary>
        /// (Possibly empty) list of related visual novels
        /// </summary>
        public List<Relation> relations;
        /// <summary>
        /// <para>(Possibly empty) list of tags linked to this VN. Each tag is represented as an array with three elements:</para>
        /// <para>tag id(integer),</para>
        /// <para>score(number between 0 and 3),</para>
        /// <para>spoiler level(integer, 0=none, 1=minor, 2=major)</para>
        /// <para>Only tags with a positive score are included.Note that this list may be relatively large - more than 50 tags for a VN is quite possible.</para>
        /// <para>General information for each tag is available in the tags dump (https://vndb.org/d14#2).Keep in mind that it is possible that a tag has only recently been added and is not available in the dump yet, though this doesn't happen often.</para>
        /// </summary>
        public List<List<double>> tags;
        /// <summary>
        /// Between 0 (unpopular) and 100 (most popular).
        /// </summary>
        public double popularity;
        /// <summary>
        /// Bayesian rating, between 1 and 10.
        /// </summary>
        public double rating;
        /// <summary>
        /// Number of votes.
        /// </summary>
        public int votecount;
        /// <summary>
        /// (Possibly empty) list of screenshots
        /// </summary>
        public List<Screen> screens;
        /// <summary>
        /// (Possibly empty) list of staff related to the VN
        /// </summary>
        public List<Staff> staff;
    }

    public class ImageFlagging
    {
        /// <summary>
        /// Number of flagging votes
        /// </summary>
        public int votecount;
        /// <summary>
        /// Sexual score between 0 (safe) and 2 (explicit)
        /// </summary>
        public double sexual_avg;
        /// <summary>
        /// violence score between 0 (tame) and 2 (brutal).
        /// </summary>
        public double violence_avg;
    }

    public class Links
    {
        /// <summary>
        /// name of the related article on the English Wikipedia (deprecated, use wikidata instead)
        /// </summary>
        [Obsolete("Use wikidata instead")]
        public string wikipedia;
        /// <summary>
        /// the name part of the url on renai.us
        /// </summary>
        public string renai;
        /// <summary>
        /// the URL-encoded tag used on encubed (novelnews.net)
        /// </summary>
        [Obsolete]
        public string encubed;
        /// <summary>
        /// Wikidata identifier
        /// </summary>
        public string wikidata;
    }

    public class Screen
    {
        [Obsolete]
        public bool nsfw;
        /// <summary>
        /// URL of the full-size screenshot
        /// </summary>
        public string image;
        /// <summary>
        /// height of the full-size screenshot
        /// </summary>
        public int height;
        /// <summary>
        /// width of the full-size screenshot
        /// </summary>
        public int width;
        /// <summary>
        /// release ID
        /// </summary>
        public int rid;
        public ImageFlagging flagging;
    }

    public class Relation
    {
        /// <summary>
        /// relation to the VN
        /// </summary>
        public string relation;
        /// <summary>
        /// original/official title, can be null
        /// </summary>
        public string original;
        public bool official;
        public int id;
        /// <summary>
        /// (romaji) title
        /// </summary>
        public string title;
    }

    public class Staff
    {
        /// <summary>
        /// possibly null
        /// </summary>
        public string original;
        /// <summary>
        /// alias ID
        /// </summary>
        public int aid;
        /// <summary>
        /// staff ID
        /// </summary>
        public int sid;
        /// <summary>
        /// possibly null
        /// </summary>
        public string note;
        public string role;
        public string name;
    }

    public class Anime
    {
        /// <summary>
        /// AniDB ID
        /// </summary>
        public int id;
        /// <summary>
        /// AnimeNewsNetwork ID
        /// </summary>
        public int ann_id;
        /// <summary>
        /// AnimeNfo ID
        /// </summary>
        public string nfo_id;
        public string title_romaji;
        public string title_kanji;
        /// <summary>
        /// year in which the anime was aired
        /// </summary>
        public int year;
        public string type;
    }
}
