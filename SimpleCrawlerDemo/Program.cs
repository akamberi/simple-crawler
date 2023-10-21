using SimpleCrawler;
using HtmlAgilityPack;
using System;

class Program
{
    static string dirToStoreStatic = "C:\\projects\\SimpleCrawler\\Files";
    static string startUrl = "https://crawler-test.com/";
    static async Task Main(string[] args)
    {
        var crawler = new Crawler(startUrl);
        crawler.OnUrlCrawled += (url, document) =>
        {
            Console.WriteLine("Visited: " + url);
        };
        crawler.OnUrlCrawled += async (url, document) =>
        {
            await SaveStaticFile(url, document);
        };

        await crawler.StartCrawlingAsync();
        Console.WriteLine("Done crawling, press any key to stop the app");
        Console.ReadLine();
    }
    static string GetRelativePath(string currentUrl)
    {
        var relativeUri = new Uri(startUrl).MakeRelativeUri(new Uri(currentUrl));
        return Uri.UnescapeDataString(relativeUri.ToString());
    }
    private async static Task SaveStaticFile(string url, HtmlDocument document)
    {
        try
        {
            var path = GetRelativePath(url);
            if (path == "")
                path = "index.html";
            else
                path += ".html";
            path = Path.Combine(dirToStoreStatic, path);

            var fileInfo = new FileInfo(path);
            if (!Directory.Exists(fileInfo.DirectoryName))
            {
                Directory.CreateDirectory(fileInfo.DirectoryName);
            }
            if (!File.Exists(path))
                File.WriteAllText(path, document.DocumentNode.OuterHtml);
        }
        catch (Exception ex)
        {
        }
    }
}
