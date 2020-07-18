using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Extensions.Common;

namespace VNDBMetadata
{
    public static class VNDBTags
    {
        private static List<BasicTag> _tags;

        static VNDBTags()
        {
            _tags = new List<BasicTag>();
        }

        public static BasicTag GetTagByID(int id)
        {
            return _tags.Find(x => x.id == id);
        }

        public static BasicTag GetTagByName(string name)
        {
            return _tags.Find(x => x.name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static int ReadTags(string dataDir)
        {
            var file = Path.Combine(dataDir, "tags.json");
            if (!File.Exists(file))
                return 0;

            _tags = File.ReadAllText(file).FromJson<List<BasicTag>>().OrderByDescending(x => x.vns).ToList();
            return _tags.Count;
        }

        public static async Task<bool> GetLatestDumb(string dataDir)
        {
            var filePath = Path.Combine(dataDir, Consts.LatestTagsDumbFile);
            var outputPath = Path.Combine(dataDir, "tags.json");

            if (File.Exists(filePath) && File.Exists(outputPath))
            {
                var fsi = new FileInfo(filePath);
                if ((DateTime.UtcNow - fsi.CreationTimeUtc).TotalDays > StaticSettings.TagsCacheLivetime)
                {
                    File.Delete(filePath);
                    File.Delete(outputPath);
                }
                else
                    return true;
            }

            using (var client = new WebClient())
            {
                await client.DownloadFileTaskAsync(Consts.LatestTagsDumb, filePath);
            }

            return File.Exists(filePath) && DecompressTagsArchive(filePath, outputPath);
        }

        public static bool DecompressTagsArchive(string input, string output)
        {
            if (!File.Exists(input))
                throw new ArgumentException();

            if (File.Exists(output))
                File.Delete(output);

            using (var inputStream = File.OpenRead(input))
            using (var outputStream = File.Create(output))
            {
                using (var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(outputStream);
                }
            }

            return true;
        }
    }
}
