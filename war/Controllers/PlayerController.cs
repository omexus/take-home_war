using Microsoft.AspNetCore.Mvc;
using war.services;

namespace war.Controllers;

[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly DbService _dbService;

    // GET
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPlayer(string id)
    {
        var r = await _dbService.GetPlayerWithResponse(id);
        return Ok(r);
    }

    public PlayerController(DbService service)
    {
        _dbService = service;
    }
}