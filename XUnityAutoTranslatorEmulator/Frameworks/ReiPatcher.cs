using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XUnityAutoTranslatorEmulator.Frameworks
{
    public class ReiPatcher : AFramework
    {
        public ReiPatcher(string gameFolder) : base(gameFolder)
        {
        }

        public override string FrameworkName => "ReiPatcher";
        protected override string ReleasePrefix => "XUnity.AutoTranslator-ReiPatcher";
        protected override string SetupFileName => "SetupReiPatcherAndAutoTranslator.exe";

        private string ReiPatcherDirectory => Path.Combine(GameFolder, "ReiPatcher");

        private static readonly List<string> ReiPatcherFiles = new List<string>
        {
            "ReiPatcher.exe",
            "Mono.Cecil.Rocks.dll",
            "Mono.Cecil.Pdb.dll",
            "Mono.Cecil.Mdb.dll",
            "Mono.Cecil.Inject.dll",
            "Mono.Cecil.dll",
            "ExIni.dll",
            "Patches\\XUnity.AutoTranslator.Patcher.dll"
        };

        private static readonly List<string> ReiPatcherManagedFiles = new List<string>
        {
            "0Harmony.dll",
            "ExIni.dll",
            "Mono.Cecil.dll",
            "MonoMod.RuntimeDetour.dll",
            "MonoMod.Utils.dll",
            "ReiPatcher.exe",
            "XUnity.AutoTranslator.Plugin.Core.dll",
            "XUnity.AutoTranslator.Plugin.ExtProtocol.dll",
            "XUnity.Common.dll",
            "XUnity.ResourceRedirector.dll"
        };

        public override bool IsAlreadyInstalled()
        {
            if (!Directory.Exists(ReiPatcherDirectory))
                return false;

            if (!ReiPatcherFiles.DoFilesExist(ReiPatcherDirectory))
                return false;

            if (!ReiPatcherManagedFiles.DoFilesExist(Path.Combine(GameDataFolder, "Managed")))
                return false;

            return true;
        }

        public override bool Uninstall()
        {
            Directory.Delete(ReiPatcherDirectory, true);

            ReiPatcherManagedFiles
                .Select(x => Path.Combine(GameDataFolder, "Managed", x))
                .Do(File.Delete);

            var translatorsFolder = Path.Combine(GameDataFolder, "Managed", "Translators");
           
            Directory.Delete(translatorsFolder, true);

            File.Delete(Path.Combine(GameFolder, SetupFileName));

            return !IsAlreadyInstalled();
        }

        public override void RunGame()
        {
            //ReiPatcher\ReiPatcher.exe" -c "game.ini"

            var reiPatcherExe = Path.Combine(ReiPatcherDirectory, "ReiPatcher.exe");
            var gameExe = Utils.GetGameExe(GameFolder);
            if(gameExe == null)
                throw new Exception("Found no game executable!");

            var gameName = Path.GetFileNameWithoutExtension(gameExe);

            var process = new ProcessHelper(reiPatcherExe)
            {
                Args = $"-c \"{gameName}.ini\""
            };

            //var observer = process.Output.Subscribe(tuple => { Utils.Log(tuple.Line); }, () => Utils.Log("Finished extraction"));
            var result = -1;
            try
            {
                result = process.Start(ReiPatcherDirectory, false, false).Result;
            }
            catch (Exception e)
            {
                Utils.Log(e, $"Exception while running process {reiPatcherExe}");
               /* if (Debugger.IsAttached) 
                    throw;*/
            }
            /*finally
            {
                observer.Dispose();
            }*/

            Utils.Log($"Process exited with code {result}");
        }
    }
}
