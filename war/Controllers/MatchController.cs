using Microsoft.AspNetCore.Mvc;
using war.models;
using war.Requests;
using war.services;

namespace war.Controllers;

[Route("[controller]")]
public class MatchController : ControllerBase
{
    private readonly DbService _dbService;
    public MatchController(DbService service)
    {
        _dbService = service;
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var r = await _dbService.GetMatchWithResponse(id);
        return Ok(r);
    }
    
    [HttpPost("new")]
    public async Task<IActionResult> Post([FromBody] PlayerRequest player)
    {
        var matchResponse = await _dbService.CreateMatch(player);
        return Ok(matchResponse); 
    }

    [HttpGet("list")]
    public async Task<IActionResult> Get()
    {
        var r = await _dbService.GetOpenedMatches();
        return Ok(r);
    }
    
    [HttpPut("{matchId}/join")]
    public async Task<IActionResult> Join(string matchId, [FromBody] PlayerRequest player)
    {
        var playerId = await _dbService.JoinMatch(matchId, player);
        return Ok(playerId);
    }
    
    [HttpPut("{matchId}/start/{playerId}")]
    public async Task<IActionResult> Start(string matchId, string playerId)
    {
        var response = await _dbService.StartMatch(matchId, playerId);
        return Ok(response);
    }
    
    [HttpPut("{matchId}/draw/{playerId}")]
    public async Task<IActionResult> Draw(string matchId, string playerId)
    {
        var response = await _dbService.DrawCard(matchId, playerId);
        return Ok(response);
    }
}