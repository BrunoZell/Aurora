using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;

namespace Aurora.LawDownloader
{
    public static class Program
    {
        public static async Task Main()
        {
            var config = Configuration.Default.WithDefaultLoader().WithXml();
            var address = "http://www.gesetze-im-internet.de/gii-toc.xml";
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(address);

            using var client = new HttpClient();
            foreach (var item in document.QuerySelectorAll("item")) {
                var title = item.QuerySelector("title").TextContent;
                var link = item.QuerySelector("link").TextContent;

                Console.WriteLine($"Downloading '{title}'");

                using var download = await client.GetAsync(link);
                using var content = await download.Content.ReadAsStreamAsync();
                using var archive = new ZipArchive(content);

                foreach (var entry in archive.Entries) {
                    var fileInfo = new FileInfo(ConstructFileName(title, entry));
                    fileInfo.Directory.Create();
                    using var file = fileInfo.OpenWrite();
                    using var payload = entry.Open();
                    await payload.CopyToAsync(file);
                }
            }
        }

        private static string ConstructFileName(string title, ZipArchiveEntry entry)
        {
            var directory = String.Join(String.Empty, title.Split(Path.GetInvalidFileNameChars()));
            directory = String.Join(String.Empty, directory.Split(Path.GetInvalidPathChars()));

            if (directory.Length > 65) {
                directory = directory.Substring(0, 65);
            }

            directory = directory.TrimEnd(' ');
            return Path.Combine("downloads", directory, entry.FullName);
        }
    }
}
