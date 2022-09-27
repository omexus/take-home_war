using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using war.Exceptions;
using war.models;
using war.Requests;
using war.Responses;
using war.Store;
using Match = war.models.Match;

namespace war.services;


public interface IMatchService
{
    Task<PlayerMatchResponse?> CreateMatch(PlayerRequest playerRequest);
    Task<PlayerMatchResponse> JoinMatch(string matchId, PlayerRequest playerRequest);
    Task<PlayerResponse?> GetPlayerWithResponse(string playerId);
    Task<MatchResponse?> GetMatchWithResponse(string matchId);
    Task<List<OpenMatchResponse>> GetOpenedMatches();
    Task<PlayerMatchResponse> StartMatch(string matchId, string playerId);
    Task<PlayerMatchResponse> DrawCard(string matchId, string userId);
}

public class MatchService: IMatchService
{
    private readonly IMongoContext _dbContext;
    private readonly ICardService _cardService;

    public MatchService(IMongoContext dbContext, ICardService cardService)
    {
        _dbContext = dbContext;
        _cardService = cardService;
    }

    /// <summary>
    /// A player decides to start (create) a new match (rather than joining an existing one)
    /// </summary>
    /// <param name="playerRequest"></param>
    /// <returns>matchId - newly created match</returns>
    public async Task<PlayerMatchResponse?> CreateMatch(PlayerRequest playerRequest)
    {
        Player player;
        //if player is new, add it to the collection
        if (playerRequest?.Id == null)
        {
            player = new Player();
            await _dbContext.Players.InsertOneAsync(player);
            playerRequest ??= new PlayerRequest();
            playerRequest.Id = player.Id;
        }
        else
        {
            //get player
            player = await GetPlayer(playerRequest.Id);
            if (player == null)
            {
                throw new NotFoundException("Player does not exist. Try sending 'null' to create a new one");
            }
        }

        var playerMatch = new PlayerMatch() {PlayerId = player.Id};
        //create a match
        var match = new Match()
        {
            PlayerOne = playerMatch,
            CardsToPlay = 1,
            CreatedTime = DateTime.UtcNow
        };

        await _dbContext.Matches.InsertOneAsync(match);

        return GetResponse(match, playerMatch);
    }

    private async Task<Player> GetPlayer(string id)
    {
        var matchCursor = await _dbContext.Players.FindAsync(m => m.Id == id);
        return matchCursor.FirstOrDefault();
    }
    
    /// <summary>
    /// This is used by a second player who wants to join a match
    /// </summary>
    /// <param name="matchId">match id to join</param>
    /// <param name="playerRequest">player joining in</param>
    /// <returns>PlayerTwo Id - if player is new</returns>
    /// <exception cref="Exception"></exception>
    public async Task<PlayerMatchResponse?> JoinMatch(string matchId, PlayerRequest playerRequest)
    {
        if (playerRequest == null)
        {
            playerRequest = new PlayerRequest();
        }
        
        var match = await this.GetMatch(matchId);

        if (match == null)
        {
            throw new NotFoundException("Could not find match");
        }

        if (match.PlayerTwo != null)
        {
            if (match.EndTime != null)
                throw new ArgumentException("Match has already finished");
            
            throw new ArgumentException("Match is already in progress, start or join a different one");
        }
        
        Player player;
        //if player is new, add it to the collection
        if (playerRequest?.Id == null)
        {
            //add player to collection
            player = new Player();
            await _dbContext.Players.InsertOneAsync(player);
        }
        else
        {
            player = await GetPlayer(playerRequest.Id);
            if (player == null)
            {
                throw new NotFoundException("Player does not exist");
            }
        }

        if (match.PlayerOne.PlayerId == player.Id)
        {
            throw new ArgumentException("For this version you cannot play against yourself (tip: you can always play against yourself and 'trick' the system by creating a new player (playerId == null)");
        }

        match.PlayerTwo = new PlayerMatch(){PlayerId = player.Id};

        var filter = Builders<Match>.Filter.Eq("Id", matchId);

        await _dbContext.Matches.ReplaceOneAsync(filter, match);

        return GetResponse(match, match.PlayerTwo);
    }

    private PlayerMatchResponse GetResponse(Match match, PlayerMatch player)
    {
        return new PlayerMatchResponse()
        {
            MatchId = match?.Id,
            YourPlayerId = player.PlayerId,
            CreatedTime = match.CreatedTime,
            EndTime = match.EndTime,
            StartTime = match.StartTime,
            Status = match.PlayerTwo == null
                ? MatchStatus.Started
                : (match.EndTime == null ? MatchStatus.InProgress : MatchStatus.Ended),
            CardsLeft = player.Cards?.Count() ?? 0,
            CurrentCard = player.CurrentCard,
            CardsOnPile = match.CardsOnPile?.Count ?? 0,
            WinnerPlayerId = match.Winner,
            PlayerOne = new PlayerMatchStateResponse()
            {
                CardsLeft = match.PlayerOne?.Cards?.Count ?? 0,
                CurrentCard = match.PlayerOne?.CurrentCard ?? 0,
                PlayerId = match.PlayerOne?.PlayerId
            },
            PlayerTwo = new PlayerMatchStateResponse()
            {
                CardsLeft = match.PlayerTwo?.Cards?.Count ?? 0,
                CurrentCard = match.PlayerTwo?.CurrentCard ?? 0,
                PlayerId = match.PlayerTwo?.PlayerId
            }
        };
    }

    /// <summary>
    /// This is used to get a match (for status of a game that the consumer need to constantly poll - if game is in progress)
    /// </summary>
    /// <param name="matchId">match Id to lookup</param>
    /// <returns>Match</returns>
    public async Task<PlayerMatchResponse?> GetMatch(string matchId, string userId)
    {
        var matchCursor = await _dbContext.Matches.FindAsync(m => m.Id == matchId);
        var match = matchCursor.FirstOrDefault();

        if (match == null)
        {
            throw new NotFoundException("Could not find match");
        }

        return GetResponse(match, match.PlayerOne.PlayerId == userId ? match.PlayerOne : match.PlayerTwo);
    }

    private async Task<Match?> GetMatch(string matchId)
    {
        var matchCursor = await _dbContext.Matches.FindAsync(m => m.Id == matchId);
        return matchCursor.FirstOrDefault();

    }
    
    public async Task<PlayerResponse?> GetPlayerWithResponse(string playerId)
    {
        var player = await this.GetPlayer(playerId);

        if (player == null)
        {
            throw new NotFoundException($"Could not find player with id:{playerId}");
        }

        return new PlayerResponse()
        {
            Id = playerId, Wins = player.Wins
        };
    }
    
    public async Task<MatchResponse?> GetMatchWithResponse(string matchId)
    {
        var match = await this.GetMatch(matchId);

        if (match == null)
        {
            throw new NotFoundException($"Could not find a match with provided id: {matchId}");
        }
        
        return new MatchResponse()
        {
            MatchId = match?.Id,
            CreatedTime = match.CreatedTime,
            EndTime = match.EndTime,
            StartTime = match.StartTime,
            Status = match.PlayerTwo == null
                ? MatchStatus.Started
                : (match.EndTime == null ? MatchStatus.InProgress : MatchStatus.Ended),
            CardsOnPile = match.CardsOnPile?.Count ?? 0,
            WinnerPlayerId = match.Winner,
            PlayerOne = new PlayerMatchStateResponse()
            {
                PlayerId = match.PlayerOne?.PlayerId,
                CardsLeft = match.PlayerOne?.Cards?.Count ?? 0,
                CurrentCard = match.PlayerOne?.CurrentCard ?? 0
            },
            PlayerTwo = new PlayerMatchStateResponse()
            {
                PlayerId = match.PlayerOne?.PlayerId,
                CardsLeft = match.PlayerOne?.Cards?.Count ?? 0,
                CurrentCard = match.PlayerOne?.CurrentCard ?? 0
            }
        };

    }

    /// <summary>
    /// This gets a list of open matches so that the player knows what matches are open to join
    /// note: Player can always create a new match
    /// </summary>
    /// <returns>List of open matches</returns>
    public async Task<List<OpenMatchResponse>> GetOpenedMatches()
    {
        var filter = Builders<Match>.Filter.Where(m => m.PlayerTwo == null);
        var matches = await _dbContext.Matches.Find(filter).ToListAsync();

        return matches.Select(m => new OpenMatchResponse()
        {
            MatchId = m.Id,
            CreatedTime = m.CreatedTime,
            OponentPlayerId = m.PlayerOne.PlayerId
        }).ToList();
    }
    
    /// <summary>
    /// This assumes that a player wants to start a match that is already created
    /// </summary>
    /// <param name="matchId"></param>
    /// <param name="playerId"></param>
    /// <exception cref="Exception"></exception>
    public async Task<PlayerMatchResponse> StartMatch(string matchId, string playerId)
    {
        var matchCursor = await _dbContext.Matches.FindAsync(m=> m.Id == matchId);

        var match = matchCursor.FirstOrDefault();

        if (match == null)
        {
            throw new NotFoundException($"Match {matchId} does not exist, create or join one first");
        }

        if (match.PlayerTwo == null)
        {
            throw new ArgumentException($"Need another player");
        }
        
        if (match.PlayerOne.PlayerId != playerId && match.PlayerTwo.PlayerId != playerId)
            throw new ArgumentException($"Not allowed to join match, spots are already taken");

        if (match.StartTime != null)
        {
            throw new ArgumentException($"Match is already in progress, try calling 'draw' endpoint if both have played their hand");
        }

        if (match.EndTime != null)
        {
            throw new ArgumentException($"Match has already finished!");
        }

        //shuffle cards
        var shuffledCards = _cardService.ShuffleCards();
        
        //divide cards (one by one)
        var player1Cards = new List<int>();
        var player2Cards = new List<int>();

        _cardService.DivideCardsToPlayers(shuffledCards, player1Cards, player2Cards);
            
        match.PlayerOne.Cards = player1Cards;
        match.PlayerTwo.Cards = player2Cards;
        
        //save the cards
        var filter = Builders<Match>.Filter.Eq("Id", matchId);

        var update = Builders<Match>.Update
            .Set("PlayerOne", match.PlayerOne)
            .Set("PlayerTwo", match.PlayerTwo)
            .Set("StartTime", DateTime.UtcNow)
            ;
        
        await _dbContext.Matches.UpdateOneAsync(filter, update);
        
        return GetResponse(match, playerId == match.PlayerOne?.PlayerId ? match.PlayerOne : match.PlayerTwo);
    }

    /// <summary>
    /// Allow player to play his hand and return current match with updated state
    /// </summary>
    /// <param name="matchId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<PlayerMatchResponse> DrawCard(string matchId, string userId)
    {
        //grab match
        var r = await _dbContext.Matches.FindAsync(m=> m.Id == matchId && m.PlayerOne.PlayerId == userId || m.PlayerTwo.PlayerId == userId);

        var match = r.FirstOrDefault();

        if (match == null)
        {
            throw new NotFoundException("Match does not exist or you are not a player of the provided match");
        }

        if (match.StartTime == null)
        {
            throw new ArgumentException($"Match needs to be started first!");
        }

        var currentPlayer = match.PlayerOne.PlayerId == userId ? match.PlayerOne : match.PlayerTwo; 
        var oppositePlayer = match.PlayerOne.PlayerId == userId ? match.PlayerTwo : match.PlayerOne;

        if (currentPlayer.CurrentCard > 0)
            throw new ArgumentException("You've played your card already");

        //play the card (and have the last for comparison further below if needed)
        currentPlayer.CurrentCard = PutCardsOnPile(match, currentPlayer);

        UpdateDefinition<Match> update;
        //we just save the draw and wait for the other player to draw
        var filter = Builders<Match>.Filter.Eq("Id", matchId);
        
        var cardsToPlay = match.CardsToPlay;
        //resolve the game if the other player has already shown his card
        if (oppositePlayer.CurrentCard > 0)
        {
            PlayerMatch matchWinner = null;
            //check who wins
            if (currentPlayer.CurrentCard > oppositePlayer.CurrentCard)
            {
                matchWinner = PlayerWins(match, currentPlayer, oppositePlayer);
                cardsToPlay = 1; //reset
            }else if (currentPlayer.CurrentCard < oppositePlayer.CurrentCard)
            {
                matchWinner = PlayerWins(match, oppositePlayer, currentPlayer);
                cardsToPlay = 1; //reset
            }
            else
            {
                //we just update the match for next play (draw 2)
                cardsToPlay = 2; 
                //reset current card
                match.PlayerTwo.CurrentCard = 0;
                match.PlayerOne.CurrentCard = 0;
            }

            //see if there is a match winner
            if (matchWinner != null)
            {
                //update everything
                update = Builders<Match>.Update
                    .Set("Winner", matchWinner.PlayerId)
                    .Set("EndTime", DateTime.UtcNow)
                    .Set("PlayerOne", match.PlayerOne)
                    .Set("PlayerTwo", match.PlayerTwo)
                    .Set("CardsOnPile", 0);
                
                //increment winner's all-time wins
                var updatePlayer = Builders<Player>.Update
                    .Inc("Wins", 1);

                _ = await  _dbContext.Players.FindOneAndUpdateAsync(p => p.Id == matchWinner.PlayerId, updatePlayer); 
            }
            else
            {
                update = UpdatePlay(match, cardsToPlay);
            }
        
        }
        else
        {
            update = UpdatePlay(match, cardsToPlay);
        }
        
        await _dbContext.Matches.UpdateOneAsync(filter, update);
        return GetResponse(match, currentPlayer);
        
    }

    private int PutCardsOnPile(Match match, PlayerMatch currentPlayer)
    {
        var cardsToPlay = match.CardsToPlay;
        var cardsOnPlayOne = currentPlayer.Cards.Take(cardsToPlay).ToImmutableList();

        if (match.CardsOnPile == null)
        {
            match.CardsOnPile = new List<int>();
        }
        match.CardsOnPile.AddRange(cardsOnPlayOne);
        currentPlayer.Cards.RemoveRange(0, cardsToPlay);

        return cardsOnPlayOne.Last();
    }

    private static UpdateDefinition<Match> UpdatePlay(Match match, int cardsToPlay)
    {
        var update = Builders<Match>.Update
            .Set("PlayerOne", match.PlayerOne)
            .Set("PlayerTwo", match.PlayerTwo)
            .Set("CardsToPlay", cardsToPlay)
            .Set("CardsOnPile", match.CardsOnPile);

        return update;
    }
    
    private static PlayerMatch PlayerWins(Match match, PlayerMatch playWinner, PlayerMatch playLoser)
    {
        //current player wins and adds the other player cards to his hand
        //but first, add his own to his own deck
        var winnerCards = match.CardsOnPile;
        playWinner.Cards.AddRange(winnerCards);
        match.CardsOnPile = new List<int>();

        //resets for next play
        playLoser.CurrentCard = 0;
        playWinner.CurrentCard = 0;
        
        //check if winner has won the match
        if (playLoser.Cards.Count == 0)
        {
            return playWinner;
        }

        return null;
    }

}