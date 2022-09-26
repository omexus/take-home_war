namespace war.Responses;

public class MatchResponse : MatchResponseBase
{
    public PlayerMatchStateResponse PlayerOne { get; set; }
    public PlayerMatchStateResponse PlayerTwo { get; set; }
}

public class PlayerMatchStateResponse
{
    public string? PlayerId { get; set; }
    public int CardsLeft { get; set; }
    public int CurrentCard { get; set; }
}