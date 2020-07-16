namespace DLSiteMetadata
{
    internal static class Consts
    {
        internal static string RootJPN => "https://www.dlsite.com/maniax/";
        internal static string RootENG => "https://www.dlsite.com/ecchi-eng/";

        internal static string GetWorkURL(string id, bool english = true)
        {
            return english
                ? $"{RootENG}work/=/product_id/{id}.html"
                : $"{RootJPN}work/=/product_id/{id}.html";
        }

        internal static string GetAnnounceURL(string id, bool english = true)
        {
            return english
                ? $"{RootENG}announce/=/product_id/{id}.html"
                : $"{RootJPN}announce/=/product_id/{id}.html";
        }

        internal static string GetReleaseTranslation(bool english)
        {
            return english ? "Release" : "販売日";
        }

        internal static string GetLastModifiedTranslation(bool english)
        {
            return english ? "Last Modified" : "最終更新日";
        }

        internal static string GetAgeRatingsTranslation(bool english)
        {
            return english ? "Age Ratings" : "年齢指定";
        }

        internal static string GetWorkFormatTranslation(bool english)
        {
            return english ? "Work Format" : "作品形式";
        }

        internal static string GetFileFormatTranslation(bool english)
        {
            return english ? "File Format" : "ファイル形式";
        }

        internal static string GetGenreTranslation(bool english)
        {
            return english ? "Genre" : "ジャンル";
        }

        internal static string GetFileSizeTranslation(bool english)
        {
            return english ? "File Size" : "ファイル容量";
        }
    }
}
