namespace war.Responses;

public class PlayerMatchResponse: MatchResponse
{
    public string YourPlayerId { get; set; }
    public int CardsLeft { get; set; }
    public int CurrentCard { get; set; }
}

public enum MatchStatus
{
    Started,
    InProgress,
    Ended
}