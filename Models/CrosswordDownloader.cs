using System.Net;
using System.Xml.Linq;

namespace Crossword.Models
{
    public static class CrosswordDownloader
    {
        private static HttpClient m_client = new HttpClient();

        public static string ToURL(string type, string id) => $"https://www.theguardian.com/crosswords/{type}/{id}";

        public static async Task<string> DownloadAsync(string type, string id) => await DownloadAsync(ToURL(type, id));
        
        public static async Task<string> DownloadAsync(string url)
        {
            //string url = "https://www.theguardian.com/crosswords/{type}/{id}";
            //string url = "http://localhost:8088/static/cryptic_29648.html";

            try
            {
                // Send a GET request to the specified URL
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.UserAgent.ParseAdd("BingoBeans");
                var response = await m_client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string htmlContent = await response.Content.ReadAsStringAsync();

                int start = htmlContent.IndexOf("<gu-island name=\"CrosswordComponent\"");
                if (start != -1)
                {
                    int end = htmlContent.IndexOf("><", start + 1);
                    string content = htmlContent.Substring(start, (end - start));
                    content = content.Replace(">", "&lt;");

                    string docContent = "<root>" + content + "></gu-island></root>";

                    XDocument doc = XDocument.Load(new StringReader(docContent));

                    string jsonEncodedString = doc?.Root?.Element("gu-island")?.Attribute("props")?.Value ?? string.Empty;

                    return WebUtility.HtmlDecode(jsonEncodedString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }

            return null;
        }

        public class AvailableCrosswords
        {
            public IEnumerable<CrosswordType> CrosswordTypes { get; set; }
        }

        public class CrosswordType
        {
            public string Name { get; set; }
            public string Error { get; set; }
            public IEnumerable<CrosswordLink> Crosswords { get; set; }
        }

        public class CrosswordLink
        {
            public string Title { get; set; }
            public string Link { get; set; }
            public string Date { get; set; }
        }

        public static async Task<AvailableCrosswords> DownloadCrosswordListsAsync(string[] crosswordTypeNames)
        {
            List<CrosswordType> crosswordTypes = new();
            AvailableCrosswords result = new AvailableCrosswords()
            {
                CrosswordTypes = crosswordTypes
            };

            foreach (string typeName in crosswordTypeNames)
            {
                List<CrosswordLink> crosswords = new();
                CrosswordType typeContainer = new()
                {
                    Name = typeName,
                    Crosswords = crosswords
                };

                crosswordTypes.Add(typeContainer);

                string url = $"https://www.theguardian.com/crosswords/series/{typeName}/rss";
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.UserAgent.ParseAdd("BingoBeans");
                    var response = await m_client.SendAsync(request);

                    response.EnsureSuccessStatusCode();

                    string xmlContent = await response.Content.ReadAsStringAsync();

                    XDocument doc = XDocument.Load(new StringReader(xmlContent));

                    foreach (XElement item in doc.Root.Element("channel").Elements("item"))
                    {
                        string title = item.Element("title")?.Value;
                        string link = item.Element("link")?.Value;
                        string category = item.Element("category")?.Value;
                        string pubDate = item.Element("pubDate")?.Value;

                        if (!string.IsNullOrEmpty(title) &&
                            !string.IsNullOrEmpty(link) &&
                            string.Equals(category, "crosswords", StringComparison.InvariantCultureIgnoreCase))
                        {
                            crosswords.Add(new CrosswordLink()
                            {
                                Title = title,
                                Link = link,
                                Date = pubDate
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    typeContainer.Error = e.Message;
                }
            }

            return result;
        }
    }
}
