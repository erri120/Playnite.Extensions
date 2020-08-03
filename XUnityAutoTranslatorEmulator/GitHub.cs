using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace XUnityAutoTranslatorEmulator
{
    public class GitHubRelease
    {
        public string? name;
        public List<GitHubReleaseAsset>? assets;
    }

    public class GitHubReleaseAsset
    {
        public string? browser_download_url;
        public string? name;
        public long size;
    }

    public class GitHubClient
    {
        private readonly HttpClient _client;
        private readonly SocketsHttpHandler _handler;

        public GitHubClient()
        {
            _handler = new SocketsHttpHandler
            {
                AllowAutoRedirect = true,
                ConnectTimeout = TimeSpan.FromMilliseconds(500),
                MaxConnectionsPerServer = 20,
                PooledConnectionLifetime = TimeSpan.FromMilliseconds(100),
                PooledConnectionIdleTimeout = TimeSpan.FromMilliseconds(100)
            };
            _client = new HttpClient(_handler);
        }

        public async Task<string> GetStringAsync(string url)
        {
            var result = await GetAsync(url, HttpCompletionOption.ResponseContentRead);

            if (result.Content == null)
                throw new Exception($"Content for {url} is null!");

            return await result.Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> GetAsync(string url, HttpCompletionOption responseHeadersRead = HttpCompletionOption.ResponseHeadersRead)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Add("user-agent", "erri120.XUnityAutoTranslatorEmulator");

            var result = await _client.SendAsync(msg, responseHeadersRead);
            try
            {
                result.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException requestException)
            {
                throw new Exception($"HttpRequestException trying to access {url}", requestException);
            }

            return result;
        }
    }

    public static class GitHub
    {
        private static readonly GitHubClient Client;

        static GitHub()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            Client = new GitHubClient();
        }

        public static async Task<List<GitHubRelease>> GetGitHubReleases(string repo)
        {
            //https://api.github.com/repos/bbepis/XUnity.AutoTranslator/releases
            var url = $"https://api.github.com/repos/{repo}/releases";

            var result = await Client.GetStringAsync(url);
            List<GitHubRelease> releases;

            try
            {
                releases = result.FromJson<List<GitHubRelease>>();
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to serialize content from {url}\n {result}", e);
            }

            return releases;
        }

        public static async Task DownloadGitHubReleaseAsset(GitHubReleaseAsset asset, string output)
        {
            if(File.Exists(output))
                File.Delete(output);

            if(asset.browser_download_url == null)
                throw new Exception($"URL for asset {asset.name!} is null!");

            var result = await Client.GetAsync(asset.browser_download_url, HttpCompletionOption.ResponseContentRead);
            long? contentLength = result.Content.Headers.ContentLength;
            if(contentLength == null)
                throw new Exception($"Content length for {asset.browser_download_url} ({asset.name!}) is null!");

            await using var fs = File.OpenWrite(output);
            await result.Content.CopyToAsync(fs);
            result.Content.Dispose();
        }
    }
}
