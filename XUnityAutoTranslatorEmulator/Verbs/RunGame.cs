using CommandLine;

namespace XUnityAutoTranslatorEmulator.Verbs
{
    [Verb("run", HelpText = "Start the game using the specified Framework")]
    public class RunGame : AFrameworkVerb
    {
        protected override bool Run()
        {
            var framework = GetFramework();
            Utils.Log($"Starting {framework.FrameworkName} in {GameFolder!}");

            if (!framework.IsAlreadyInstalled())
            {
                Utils.Log($"{framework.FrameworkName} is not installed in {GameFolder!}, it will be installed.");

                framework.BackupGameFolder();
                var install = framework.Install();
                if(!install)
                {
                    Utils.Log($"Failed to install {framework.FrameworkName}!");
                    return false;
                }
            }

            framework.RunGame();

            return true;
        }
    }
}
