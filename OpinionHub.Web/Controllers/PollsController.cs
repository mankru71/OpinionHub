using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OpinionHub.Web.Hubs;
using OpinionHub.Web.Services;
using OpinionHub.Web.ViewModels;

namespace OpinionHub.Web.Controllers;

[Authorize]
public class PollsController : Controller
{
    private readonly IPollService _pollService;
    private readonly IHubContext<PollHub> _hub;

    public PollsController(IPollService pollService, IHubContext<PollHub> hub)
    {
        _pollService = pollService;
        _hub = hub;
    }

    public IActionResult Create() => View(new CreatePollViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePollViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var poll = await _pollService.CreateDraftAsync(model, userId);
            return RedirectToAction(nameof(Details), new { id = poll.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id)
    {
        var poll = await _pollService.GetPollDetailsAsync(id);
        if (poll is null) return NotFound();
        return View(poll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote(Guid id, List<Guid> optionIds)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _pollService.VoteAsync(id, userId, optionIds);
            await _hub.Clients.Group($"poll-{id}").SendAsync("pollUpdated");
        }
        catch (Exception ex)
        {
            TempData["VoteError"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        await _pollService.PublishAsync(id, userId);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ExportCsv(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bytes = await _pollService.ExportCsvAsync(id, userId);
        return File(bytes, "text/csv", "results.csv");
    }

    public async Task<IActionResult> ExportXlsx(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var bytes = await _pollService.ExportXlsxAsync(id, userId);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "results.xlsx");
    }
}
