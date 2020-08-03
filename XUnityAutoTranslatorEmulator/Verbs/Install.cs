using CommandLine;

namespace XUnityAutoTranslatorEmulator.Verbs
{
    [Verb("install", HelpText = "Install a Framework")]
    public class Install : AFrameworkVerb
    {
        [Option("backup", Default = true, HelpText = "Backup core files before installation")]
        public bool Backup { get; set; }

        protected override bool Run()
        {
            var framework = GetFramework();
            Utils.Log($"Starting installation of {framework.FrameworkName} in {GameFolder!}");

            if (framework.IsAlreadyInstalled())
            {
                Utils.Log($"Framework {framework.FrameworkName} is already installed in {GameFolder!}");
                return true;
            }

            if (Backup)
            {
                Utils.Log($"Creating backup for {GameFolder!}");
                framework.BackupGameFolder();
            }
            else
            {
                Utils.Log("Backup was set to false! The Framework installer might permanently modify your game files!");
            }

            var result = framework.Install();
            if (!result)
            {
                Utils.Log($"Failed to installed {framework.FrameworkName}");
                return false;
            }

            var check = framework.IsAlreadyInstalled();
            if (check)
            {
                Utils.Log($"Successfully installed {framework.FrameworkName} in {GameFolder!}");
                return true;
            }

            Utils.Log($"Failed to install {framework.FrameworkName} in {GameFolder!}");
            return false;
        }
    }
}
