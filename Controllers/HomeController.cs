using Microsoft.AspNetCore.Mvc;
using Crossword.Models;

namespace Crossword.Controllers;

public class HomeController : Controller
{
    private Storage.BoardStorageService m_boardStorageService;

    public HomeController(Storage.BoardStorageService boardStorageService)
    {
        m_boardStorageService = boardStorageService;
    }

    public class IndexViewModel
    {
        public bool Debug { get; set; }
        public CrosswordDownloader.AvailableCrosswords RSSData { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool debug = false)
    {
        var RSSData = await CrosswordDownloader.DownloadCrosswordListsAsync(debug ? ["cryptic", "speedy"] 
                                                                                  : ["quick", "speedy"]);
        if (RSSData != null)
        {
            Dictionary<string, CrosswordDownloader.CrosswordLink> links = new();

            foreach (var crosswordType in RSSData.CrosswordTypes)
            {
                foreach (var crossword in crosswordType.Crosswords)
                {
                    string gameId = Storage.BoardStorage.GameIdKey(crosswordType.Name, crossword.CrosswordNumber);
                    links.Add(gameId, crossword);
                }
            }

            foreach (var completionInfoPair in await m_boardStorageService.GetCompletionTimes(links.Keys))
            {
                if (links.TryGetValue(completionInfoPair.Key, out var link))
                {
                    link.CompletionTimeInSeconds = completionInfoPair.Value.Seconds;
                    link.CompletionDate = completionInfoPair.Value.Date;
                }
            }
        }

        return View(new IndexViewModel()
        {
            RSSData = RSSData,
            Debug = debug
        });
    }

    [HttpPost]
    public IActionResult CustomCrossword(string CrosswordType, string CrosswordNumber)
    {
        return RedirectToAction("Index", "Crossword", new { url = CrosswordDownloader.ToURL(CrosswordType, CrosswordNumber) });
    }
}
