using GameStore.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers;

[Route("home")]
public class HomeController : Controller
{
    private GameService gameService;

    public HomeController(GameService _gameService)
    {
        gameService = _gameService;
    }


    [Route("~/")]
    [Route("index")]
    [Route("")]
    public IActionResult Index()
    {
        ViewBag.games = gameService.findAll();
        return View();
    }
}