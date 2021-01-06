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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Common;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace ExtensionUpdater
{
    [UsedImplicitly]
    public class ExtensionUpdaterPlugin : Plugin
    {
        private readonly IPlayniteAPI _playniteAPI;
        private readonly ILogger _logger;
        private readonly List<string> _extensionsDirectories;
        private readonly CancellationTokenSource _source;
        private readonly CancellationToken _token;

        public ExtensionUpdaterPlugin(IPlayniteAPI playniteAPI) : base(playniteAPI)
        {
            _playniteAPI = playniteAPI;
            _logger = playniteAPI.CreateLogger();

            _extensionsDirectories = new List<string>();
            
            var applicationPath = _playniteAPI.Paths.ApplicationPath;
            var applicationExtensionsPath = Path.Combine(applicationPath, "Extensions");
            _extensionsDirectories.Add(applicationExtensionsPath);

            var configurationPath = _playniteAPI.Paths.ConfigurationPath;
            var configurationExtensionsPath = Path.Combine(configurationPath, "Extensions");
            _extensionsDirectories.Add(configurationExtensionsPath);

            _source = new CancellationTokenSource();
            _token = _source.Token;
        }

        public override Guid Id { get; } = Guid.Parse("f4a74d1b-44c0-4ea5-a935-2b994a638236");

        public override void Dispose()
        {
            _source.Dispose();
            base.Dispose();
        }

        public override void OnApplicationStopped()
        {
            _source.Cancel();
        }

        public override void OnApplicationStarted()
        {
            Task.Run(() =>
            {
                // Clear temporary dowload folder and re-create it
                var downloadPath = Path.Combine(GetPluginUserDataPath(), "Temp");
                if (Directory.Exists(downloadPath))
                    Directory.Delete(downloadPath, true);
                Directory.CreateDirectory(downloadPath);
                if (_extensionsDirectories.Count == 0)
                    return;

                if (_playniteAPI.ApplicationInfo.InOfflineMode)
                {
                    _logger.Info("Application is in offline mode, skipping extension update check");
                    return;
                }

                IEnumerable<IGrouping<string, PlayniteExtensionConfig>> configs = _extensionsDirectories
                    .Where(Directory.Exists)
                    .SelectMany(extensionsDirectory =>
                        Directory.EnumerateFiles(extensionsDirectory, "*.yaml", SearchOption.AllDirectories)
                            .Select(file =>
                            {
                                try
                                {
                                    var config = YamlUtils.FromYaml<PlayniteExtensionConfig>(file);
                                    return config?.UpdaterConfig == null ? null : config;
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(e, $"Exception while trying to deserialize {file}!\n");
                                }

                                return null;
                            }).NotNull().GroupBy(config =>
                            {
                                var repo = $"{config.UpdaterConfig.GitHubUser}/{config.UpdaterConfig.GitHubRepo}";
                                return repo;
                            }));

                configs.Do(async group =>
                {
                    var repo = group.Key;
                    _logger.Info($"Found: {repo}");

                    List<GitHubRelease> releases;

                    try
                    {
                        releases = await GitHub.GetGitHubReleases(repo);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, $"Exception while loading releases for {repo}");
                        return;
                    }
                    
                    if (releases == null || releases.Count == 0)
                    {
                        _logger.Error($"Found no releases for {repo}");
                        return;
                    }

                    var latest = releases[0];
                    var sLatestVersion = latest.tag_name;

                    if (sLatestVersion[0] == 'v')
                    {
                        sLatestVersion = sLatestVersion.Substring(1);
                    }

                    var canParseLatest = Version.TryParse(sLatestVersion, out var latestVersion);
                    List<Tuple<PlayniteExtensionConfig, bool>> shouldUpdate = group.Select(config =>
                    {
                        var canParseCurrent = Version.TryParse(config.Version, out var currentVersion);

                        if (canParseCurrent)
                        {
                            if (canParseLatest)
                            {
                                if (currentVersion < latestVersion)
                                {
                                    _logger.Info($"There is a new version available for {config.Name}: {latestVersion}");
                                    return new Tuple<PlayniteExtensionConfig, bool>(config, true);
                                }

                                if (currentVersion != latestVersion)
                                    return new Tuple<PlayniteExtensionConfig, bool>(config, false);
                                
                                _logger.Info($"{config.Name} is up-to-date");
                                return new Tuple<PlayniteExtensionConfig, bool>(config, false);
                            }

                            if (config.Version.Equals(sLatestVersion, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.Info($"{config.Name} is up-to-date");
                                return new Tuple<PlayniteExtensionConfig, bool>(config, false);
                            }

                            _logger.Info($"There is a new version for {config.Name}: {sLatestVersion}");
                            return new Tuple<PlayniteExtensionConfig, bool>(config, true);
                        }

                        if (config.Version.Equals(sLatestVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.Info($"{config.Name} is up-to-date");
                            return new Tuple<PlayniteExtensionConfig, bool>(config, false);
                        }

                        _logger.Info($"There is a new version for {config.Name}: {sLatestVersion}");
                        return new Tuple<PlayniteExtensionConfig, bool>(config, true);
                    }).ToList();

                    if (shouldUpdate.Any(x => x.Item2))
                    {
                        var extensionsNameString = shouldUpdate
                            .Where(x => x.Item2)
                            .Select(x => x.Item1.Name)
                            .Aggregate((x, y) => $"{x}\n-{y}");
                        var message = $"The following Extension(s) can be updated:\n-{extensionsNameString}\nClick this message to download the release from {repo}.";

                        _playniteAPI.Notifications.Add(new NotificationMessage(repo, message, NotificationType.Info,
                            () =>
                            {
                                Process.Start(latest.html_url);
                                // Check if there are assets associated with the release
                                if (latest.assets != null)
                                {
                                    var playnitePath = Environment.ExpandEnvironmentVariables(PlayniteApi.Paths.ApplicationPath);
                                    playnitePath = Path.Combine(playnitePath, "Playnite.DesktopApp.exe");
                                    var options = new List<GenericItemOption>();
                                    // Add all .pext and .pthm files as options
                                    foreach (var asset in latest.assets)
                                    {
                                        var path = Path.Combine(GetPluginUserDataPath(), "Temp", asset.name);
                                        var ext = Path.GetExtension(asset.name).ToLower();
                                        if (ext == ".pext" || ext == ".pthm")
                                        {
                                            options.Add(new GenericItemOption(asset.name, asset.browser_download_url));
                                        }
                                    }
                                    // Only try to dowload and install if any options were found
                                    if (options.Count > 0)
                                    {
                                        GenericItemOption selected = options[0];
                                        // Let the user select a file to download 
                                        // if multiple extension files have been found
                                        if (options.Count > 1)
                                        {
                                            selected = PlayniteApi.Dialogs.ChooseItemWithSearch(
                                                options, 
                                                query => options.Where(o => query==null || query.Length == 0 || o.Name.ToLower().Contains(query.ToLower())).ToList(),
                                                null,
                                                "Multiple extension files found. Select the one you want to install."
                                            );
                                        }
                                        // Download and install selected option
                                        if (selected != null)
                                        {
                                            var path = Path.Combine(GetPluginUserDataPath(), "Temp", selected.Name);
                                            PlayniteApi.Dialogs.ActivateGlobalProgress(args =>
                                            {
                                                using (var webclient = new System.Net.WebClient())
                                                {
                                                    // Dowload extension file into temporary folder
                                                    webclient.DownloadFile(selected.Description, path);
                                                }
                                            }, new GlobalProgressOptions($"Dowloading {selected.Name}", false));
                                            // Open Playnite with installext argument to trigger 
                                            // extension installation
                                            Process.Start(playnitePath, $"--installext {path}");
                                        }
                                    }
                                }
                            }));
                    }
                    else
                    {
                        _logger.Info($"No Update available for {repo}");
                    }
                });
            }, _token);
        }
    }
}