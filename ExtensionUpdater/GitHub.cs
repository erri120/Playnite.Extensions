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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Extensions.Common;

namespace ExtensionUpdater
{
    [Serializable]
    public class GitHubRelease
    {
        public string name;
        public string tag_name;
        public string html_url;
        public List<GitHubReleaseAsset> assets;
    }

    [Serializable]
    public class GitHubReleaseAsset
    {
        public string browser_download_url;
        public string name;
        public long size;
    }

    public class GitHubClient
    {
        private readonly HttpClient _client;
        private readonly HttpClientHandler _handler;

        public GitHubClient()
        {
            _handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
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
            msg.Headers.Add("user-agent", "erri120.ExtensionUpdaterPlugin");

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
                throw new Exception($"URL for asset {asset.name} is null!");

            var result = await Client.GetAsync(asset.browser_download_url, HttpCompletionOption.ResponseContentRead);
            long? contentLength = result.Content.Headers.ContentLength;
            if(contentLength == null)
                throw new Exception($"Content length for {asset.browser_download_url} ({asset.name}) is null!");

            using (var fs = File.OpenWrite(output))
            {
                await result.Content.CopyToAsync(fs);
                result.Content.Dispose();
            }
        }
    }
}