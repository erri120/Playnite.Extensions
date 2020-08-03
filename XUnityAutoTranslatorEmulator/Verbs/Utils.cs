using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;

namespace XUnityAutoTranslatorEmulator
{
    public static class Utils
    {
        public static readonly Subject<(string msg, bool newLine)> LogSubject = new Subject<(string msg, bool newLine)>();

        static Utils()
        {
            LogSubject.Subscribe(x =>
            {
                var (msg, newLine) = x;
                if (newLine)
                    Console.WriteLine(msg);
                else
                    Console.Write(msg);
            });
        }

        public static void Log(string msg, bool newLine = true)
        {
            LogSubject.OnNext((msg, newLine));
        }

        public static void Log(Exception e, string msg)
        {
            Log($"{msg}\n{e}");
        }

        public static void Do<T>(this IEnumerable<T> col, Action<T> a)
        {
            foreach (var i in col) a(i);
        }

        public static string GetTempFolder()
        {
            var tmp = Path.GetTempPath();
            var folder = Path.Combine(tmp, "erri120.XUnityAutoTranslatorEmulator");
            return folder;
        }

        public static string? GetGameDataFolder(string gameFolder)
        {
            var exe = GetGameExe(gameFolder);
            if (exe == null)
                return null;

            var name = Path.GetFileNameWithoutExtension(exe);
            var dataFolder = Path.Combine(gameFolder, $"{name}_Data");

            return Directory.Exists(dataFolder) ? dataFolder : null;
        }

        public static string? GetGameExe(string gameFolder)
        {
            string? result = null;
            Directory.EnumerateFiles(gameFolder, "*.exe", SearchOption.TopDirectoryOnly).Do(exe =>
            {
                var name = Path.GetFileNameWithoutExtension(exe);
                if (name == null)
                    return;

                var dataFolder = Path.Combine(gameFolder, $"{name}_Data");
                if (Directory.Exists(dataFolder))
                    result = exe;
            });

            return result;
        }

        private static JsonSerializerSettings GenericJsonSettings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        public static bool DoFilesExist(this IEnumerable<string> col, string folder)
        {
            return col.All(x => File.Exists(Path.Combine(folder, x)));
        }

        public static string ToJson<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, GenericJsonSettings);
        }

        public static T FromJson<T>(this string data)
        {
            return JsonConvert.DeserializeObject<T>(data, GenericJsonSettings)!;
        }

        public static int ExtractArchive(string input, string output)
        {
            var sevenZip = Path.Combine("7z", "7z.exe");
            if(!File.Exists(sevenZip))
                throw new Exception($"7z.exe is not present in {sevenZip}!");

            var process = new ProcessHelper(sevenZip){Args = $"x -bsp1 -y -o\"{output}\" {input} -mmt=off" };
            Log($"Starting extraction of {input} to {output}");
            /*var observer = process.Output.Subscribe(x =>
            {
                if (x.Type == ProcessHelper.StreamType.Output)
                {
                    var (_, line) = x;
                    if (line == null)
                        return;

                    Log($"Extracting: {line}", false);
                }
            }, () =>
            {
                Log($"Finished extracting {input}");
            });*/
            var observer = process.Output.Subscribe(tuple => { }, () => Log("Finished extraction"));

            var result = process.Start().Result;
            observer.Dispose();
            return result;
        }
    }
}
