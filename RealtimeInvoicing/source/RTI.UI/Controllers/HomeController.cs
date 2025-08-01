using System.Diagnostics;

using EventStore.StreamConnectors;

using Microsoft.AspNetCore.Mvc;

using RTI.UI;
using RTI.UI.Models;

public class HomeController : Controller {
    private readonly CurrentBackplaneVm _backplaneVm;
    private readonly ILogger<HomeController> _logger;

    public HomeController(CurrentBackplaneVm backplaneVm, ILogger<HomeController> logger) {
        _backplaneVm = backplaneVm;
        _logger = logger;
    }

    public IActionResult Index() {
        _logger.LogDebug("Index loading...");
        return View();
    }

    public IActionResult Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpPost]
    public async Task<IActionResult> SwitchBackplane([FromQuery] Backplanes backplane) {
        if(_backplaneVm.CurrentBackplane != backplane) {
            await _backplaneVm.ChangeBackplaneAsync(backplane);
        }

        return RedirectToAction("Index", "Home");
    }
}
