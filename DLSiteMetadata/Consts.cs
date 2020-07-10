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
    }
}
