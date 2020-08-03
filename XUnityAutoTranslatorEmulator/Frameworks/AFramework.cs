using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XUnityAutoTranslatorEmulator.Frameworks
{
    public abstract class AFramework
    {
        protected readonly string GameFolder;
        protected string GameDataFolder => Utils.GetGameDataFolder(GameFolder) ?? throw new Exception($"Game Data Folder for {GameFolder} is null!");
        private string BackupFolder => Path.Combine(GameFolder, "backup", FrameworkName);

        public abstract string FrameworkName { get; }
        protected abstract string ReleasePrefix { get; }
        protected abstract string SetupFileName { get; }

        protected AFramework(string gameFolder)
        {
            GameFolder = gameFolder;
        }

        public abstract bool IsAlreadyInstalled();
        public abstract bool Uninstall();
        public abstract void RunGame();

        public virtual IEnumerable<string> FilesToBackup { get; } = new List<string>
        {
            "Managed\\UnityEngine.dll"
        };

        public void BackupGameFolder()
        {
            if (!Directory.Exists(BackupFolder))
                Directory.CreateDirectory(BackupFolder);

            FilesToBackup
                .Select(x => (backup: Path.Combine(BackupFolder, x), original: Path.Combine(GameDataFolder, x)))
                .Where(x => File.Exists(x.original))
                .Do(x =>
                {
                    var (backup, original) = x;
                    Utils.Log($"Creating backup of {original} in {backup}");
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(original, backup, true);
                });
        }

        public void RestoreBackup(bool deleteBackup)
        {
            if (!Directory.Exists(BackupFolder))
            {
                Utils.Log($"Restore Backup got called but {BackupFolder} does not exist!");
                return;
            }

            FilesToBackup
                .Select(x => (backup: Path.Combine(BackupFolder, x), original: Path.Combine(GameDataFolder, x)))
                .Where(x => File.Exists(x.original))
                .Do(x =>
                {
                    var (backup, original) = x;
                    Utils.Log($"Restoring backup of {original} from {backup}");
                    File.Copy(backup, original, true);

                    if(deleteBackup)
                        File.Delete(backup);
                });
        }

        public bool Install()
        {
            var file = DownloadRelease(ReleasePrefix);
            var code = Utils.ExtractArchive(file, GameFolder);
            if (code != 0)
                throw new Exception($"Exit code from 7z is {code}!");

            var setupFile = Path.Combine(GameFolder, SetupFileName);
            if (!File.Exists(setupFile))
                throw new Exception($"Setup file {SetupFileName} does not exist in {GameFolder}!");

            var process = new ProcessHelper(setupFile);
            var observer = process.Output.Subscribe(x =>
            {
                Utils.Log(x.Line);
            });
            var result = process.Start(GameFolder).Result;
            Utils.Log("You might saw a System.InvalidOperationException about Console.ReadKey, this is to be expected. " +
                      "The exit code below will probably be -532462766, " +
                      "you can ignore those messages as only \"Setup completed. Press any key to exit.\" is important.");
            Utils.Log($"Installed finished with exit code {result}");

            observer.Dispose();
            return true;
        }

        private string DownloadRelease(string prefix)
        {
            var releases = GitHub.GetGitHubReleases("bbepis/XUnity.AutoTranslator").Result;
            var latest = releases.First();

            if(latest.assets == null || latest.assets.Count == 0)
                throw new Exception($"Found no assets for {latest.name!}");

            var asset = latest.assets.First(x =>
                x.name != null && x.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            var cacheFolder = Path.Combine(Utils.GetTempFolder(), FrameworkName);
            var cachedFile = Path.Combine(cacheFolder, asset.name!);

            if (Directory.Exists(cacheFolder))
            {
                if (File.Exists(cachedFile))
                    return cachedFile;
            }
            else
            {
                Directory.CreateDirectory(cacheFolder);
            }

            //remove old versions
            Directory.EnumerateFiles(cacheFolder, "*", SearchOption.TopDirectoryOnly).Do(f =>
            {
                if(f.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    File.Delete(f);
            });

            GitHub.DownloadGitHubReleaseAsset(asset, cachedFile).Wait();
            return cachedFile;
        }
    }
}
