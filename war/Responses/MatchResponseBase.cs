namespace war.Responses;

public class MatchResponseBase
{
    public string? MatchId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public MatchStatus Status { get; set; }
    public string WinnerPlayerId { get; set; }
    public int CardsOnPile { get; set; }
}