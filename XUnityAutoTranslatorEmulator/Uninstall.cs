using CommandLine;

namespace XUnityAutoTranslatorEmulator.Verbs
{
    [Verb("uninstall", HelpText = "Uninstall a framework")]
    public class Uninstall : AFrameworkVerb
    {
        [Option("restore", Default = true, HelpText = "Restore a backup")]
        public bool RestoreBackup { get; set; }

        [Option("remove-backup", Default = true, HelpText = "Remove backup after restoring it")]
        public bool RemoveBackup { get; set; }

        protected override bool Run()
        {
            var framework = GetFramework();
            Utils.Log($"Removing {framework.FrameworkName} in {GameFolder!}");

            if (!framework.IsAlreadyInstalled())
            {
                Utils.Log($"Framework {framework.FrameworkName} is not installed in {GameFolder!}");
                return true;
            }

            var result = framework.Uninstall();
            if (!result)
            {
                Utils.Log($"Failed to uninstall {framework.FrameworkName}");
                return false;
            }

            if (RestoreBackup)
            {
                Utils.Log("Restoring backup.");
                framework.RestoreBackup(RemoveBackup);
            }
            else
            {
                Utils.Log("Restore backup option is set to false! It is recommended to restore the backup after removing a framework!");
            }

            var check = !framework.IsAlreadyInstalled();
            if (check)
            {
                Utils.Log($"Successfully uninstalled {framework.FrameworkName} in {GameFolder!}");
                return true;
            }

            Utils.Log($"Failed to uninstall {framework.FrameworkName} in {GameFolder!}");
            return false;
        }
    }
}
