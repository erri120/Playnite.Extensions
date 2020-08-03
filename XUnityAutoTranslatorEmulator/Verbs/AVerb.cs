using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using XUnityAutoTranslatorEmulator.Frameworks;

namespace XUnityAutoTranslatorEmulator.Verbs
{
    public abstract class AVerb
    {
        [Option('i', "input", Required = true, HelpText = "Absolute path to the game folder")]
        public string? GameFolder { get; set; }

        protected virtual bool ValidateOptions()
        {
            if (!Directory.Exists(GameFolder))
            {
                Utils.Log($"Game Folder at {GameFolder} does not exist!");
                return false;
            }

            return true;
        }

        public bool Execute()
        {
            return ValidateOptions() && Run();
        }

        protected abstract bool Run();
    }

    public abstract class AFrameworkVerb : AVerb
    {
        [Option("reipatcher", SetName = "framework", HelpText = "Will use ReiPatcher")]
        public bool UseReiPatcher { get; set; }

        [Option("bepinex", SetName = "framework", HelpText = "Will use BepInEx")]
        public bool UseBepInEx { get; set; }

        [Option("ipa", SetName = "framework", HelpText = "Will use IPA")]
        public bool UseIPA { get; set; }

        [Option("unityinjector", SetName = "framework", HelpText = "Will use UnityInjector")]
        public bool UseUnityInjector { get; set; }

        protected AFramework GetFramework()
        {
            if (UseReiPatcher)
                return new ReiPatcher(GameFolder!);

            if (UseBepInEx)
                throw new NotImplementedException();

            if (UseIPA)
                throw new NotImplementedException();

            if (UseUnityInjector)
                throw new NotImplementedException();

            throw new Exception("Should not be reached!");
        }

        protected override bool ValidateOptions()
        {
            if (!Directory.Exists(GameFolder))
            {
                Utils.Log($"Game Folder at {GameFolder} does not exist!");
                return false;
            }

            var uses = new List<bool> { UseReiPatcher, UseBepInEx, UseIPA, UseUnityInjector };
            var selectedUses = uses.Count(x => x);
            if (selectedUses > 1)
            {
                Utils.Log("You selected more than one framework!");
                return false;
            }

            if (selectedUses == 0)
            {
                Utils.Log($"You need to select at least one: {nameof(UseReiPatcher)}, {nameof(UseBepInEx)}, {nameof(UseIPA)} or {nameof(UseUnityInjector)}");
                return false;
            }

            return true;
        }
    }
}
