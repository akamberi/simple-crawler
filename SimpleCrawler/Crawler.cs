using HtmlAgilityPack;
using System.Collections.Concurrent;

namespace SimpleCrawler;

public delegate void OnUrlCrawled(string url, HtmlDocument document);

public class Crawler
{
    private string _domain;
    private List<string> _visitedLinks = new List<string>();
    private ConcurrentQueue<string> _linksToVisit = new ConcurrentQueue<string>();
    //Another option to execute a custom action would be to setup a
    //Func<string, HtmlDocument, Task>
    //(in order to have it async) and then set it up when creating Crawler, or in a later moment
    public event OnUrlCrawled OnUrlCrawled;

    public Crawler(string startUrl)
    {
        this._domain = GetDomainFromUrl(startUrl);
        this._linksToVisit.Enqueue(startUrl);
    }

    public async Task StartCrawlingAsync()
    {
        while (_linksToVisit.TryDequeue(out string url))
        {
            var urlWithoutProtocol = url.Replace("https://", "");
            urlWithoutProtocol = url.Replace("http://", "");
            if (!_visitedLinks.Contains(urlWithoutProtocol))
            {
                _visitedLinks.Add(urlWithoutProtocol);

                string content = await DownloadPageAsync(url);

                if (content is null)
                    continue;
                var html = ConvertPageToHtml(content);
                OnUrlCrawled?.Invoke(url, html);
                var links = ExtractLinksFromPage(url, html);
                foreach (string link in links)
                {
                    _linksToVisit.Enqueue(link);
                }
            }
        }
    }

    private async Task<string> DownloadPageAsync(string url)
    {
        try
        {
            Console.WriteLine($"ToStart: {url}");
            using (HttpClient client = new HttpClient(new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(5),
                ResponseDrainTimeout = TimeSpan.FromSeconds(5)
            }))
            {
                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return null;
    }
    private HtmlDocument ConvertPageToHtml(string content)
    {
        var html = new HtmlDocument();
        html.LoadHtml(content);

        return html;
    }
    private string MakeAbsoluteUrl(string baseUrl, string relativeUrl)
    {
        Uri baseUri = new Uri(baseUrl);
        Uri absoluteUri = new Uri(baseUri, relativeUrl);
        return absoluteUri.ToString();
    }
    private IEnumerable<string> ExtractLinksFromPage(string pageUrl, HtmlDocument document)
    {
        var links = new List<string>();
        try
        {
            var documentLinks = document?.DocumentNode?.SelectNodes("//a[@href]");
            //null when no a element is found
            if (documentLinks != null)
            {
                foreach (var docLink in documentLinks)
                {
                    string href = docLink.GetAttributeValue("href", "");
                    string absoluteUrl = MakeAbsoluteUrl(pageUrl, href);
                    if (IsSameDomain(absoluteUrl))
                    {
                        links.Add(absoluteUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {

        }
        return links;
    }
    private bool IsSameDomain(string url)
    {
        try
        {
            //invalid url throws exception, whatsap port..
            string urlDomain = GetDomainFromUrl(url);
            return urlDomain == _domain;
        }
        catch { return false; }
    }

    private string GetDomainFromUrl(string url)
    {
        Uri uri = new Uri(url);
        return uri.Host;
    }
}