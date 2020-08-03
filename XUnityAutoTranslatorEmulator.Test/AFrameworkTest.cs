using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using XUnityAutoTranslatorEmulator.Frameworks;

namespace XUnityAutoTranslatorEmulator.Test
{
    public abstract class AFrameworkTest : IDisposable
    {
        protected readonly string GameFolder;
        private readonly IDisposable _observer;

        protected AFrameworkTest(string name, ITestOutputHelper helper)
        {
            _observer = Utils.LogSubject.Subscribe(x =>
            {
                helper.WriteLine(x.msg);
            });

            Directory.CreateDirectory(name);

            GameFolder = name;

            var exe = Path.Combine(GameFolder, $"{name}.exe");
            File.WriteAllText(exe, "lol");

            var dataFolder = Path.Combine(GameFolder, $"{name}_data");
            Directory.CreateDirectory(dataFolder);
        }

        protected void TestFramework(AFramework framework)
        {
            Assert.NotNull(Utils.GetGameExe(GameFolder));
            var dataFolder = Utils.GetGameDataFolder(GameFolder);
            Assert.NotNull(dataFolder);

            framework.FilesToBackup
                .Select(x => Path.Combine(dataFolder, x))
                .Do(x =>
                {
                    if (File.Exists(x))
                        return;

                    Directory.CreateDirectory(Path.GetDirectoryName(x));
                    File.WriteAllText(x, "lel");
                });

            Assert.False(framework.IsAlreadyInstalled());

            framework.BackupGameFolder();
            var backupFolder = Path.Combine(GameFolder, "backup", framework.FrameworkName);
            Assert.True(Directory.Exists(backupFolder));
            Assert.True(framework.FilesToBackup.Select(x => Path.Combine(backupFolder, x)).All(File.Exists));

            Assert.True(framework.Install());
            Assert.True(framework.IsAlreadyInstalled());

            framework.RunGame();

            Assert.True(framework.Uninstall());
            Assert.False(framework.IsAlreadyInstalled());

            framework.RestoreBackup(true);
            Assert.False(framework.FilesToBackup.Select(x => Path.Combine(backupFolder, x)).All(File.Exists));
        }

        public void Dispose()
        {
            _observer.Dispose();
            if (Directory.Exists(GameFolder))
                Directory.Delete(GameFolder, true);
        }
    }
}
