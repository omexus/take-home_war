using Microsoft.AspNetCore.Mvc;
using war.services;

namespace war.Controllers;

[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly IMatchService _matchService;

    // GET
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlayer(string id)
    {
        var r = await _matchService.GetPlayerWithResponse(id);
        return Ok(r);
    }

    public PlayerController(IMatchService service)
    {
        _matchService = service;
    }
}