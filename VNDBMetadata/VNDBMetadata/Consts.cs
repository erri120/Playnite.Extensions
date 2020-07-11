namespace VNDBMetadata
{
    internal static class Consts
    {
        internal static string ClientName => "erri120-Playnite-Extensions-VNDBMetadata";
        internal static string ClientVersion => "1.0.0";

        internal static string Host => "api.vndb.org";
        internal static int TCPPort => 19534;
        internal static int TLSPort => 19535;

        internal static byte EndOfTransmissionChar => 0x04;
        internal static string OK => "ok";
        internal static int MaxIterations => 10;
    }
}
