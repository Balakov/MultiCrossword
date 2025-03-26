using Microsoft.AspNetCore.Mvc;
using Crossword.Models;

namespace Crossword.Controllers;

public class HomeController : Controller
{
    public HomeController()
    {
    }

    public class IndexViewModel
    {
        public CrosswordDownloader.AvailableCrosswords RSSData { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new IndexViewModel()
        {
            RSSData = await CrosswordDownloader.DownloadCrosswordListsAsync(new[] { "quick", "speedy", "cryptic" })
        });
    }

    [HttpPost]
    public IActionResult CustomCrossword(string CrosswordType, string CrosswordNumber)
    {
        return RedirectToAction("Index", "Crossword", new { url = CrosswordDownloader.ToURL(CrosswordType, CrosswordNumber) });
    }
}
