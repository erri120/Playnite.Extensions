using System;
using CommandLine;
using XUnityAutoTranslatorEmulator.Verbs;

namespace XUnityAutoTranslatorEmulator
{
    public static class Program
    {
        private static readonly Type[] Verbs = {
            typeof(RunGame),
            //typeof(Backup),
            typeof(Uninstall),
            typeof(Install)
        };

        public static int Main(string[] args)
        {
            var result = false;
            try
            {
                result = Parser.Default.ParseArguments(args, Verbs).MapResult(
                    (RunGame run) => run.Execute(),
                    //(Backup backup) => backup.Execute(),
                    (Uninstall uninstall) => uninstall.Execute(),
                    (Install install) => install.Execute(),
                    err => false);
            }
            catch (Exception e)
            {
                Utils.Log(e, "Exception while executing core program.");
            }

            if (!result)
            {
                Utils.Log("Execution of subprogram failed, check previous messages!");
            }

            return result ? 0 : -1;
        }
    }
}
