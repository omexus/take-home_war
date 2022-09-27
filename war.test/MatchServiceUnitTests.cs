using MongoDB.Driver;
using Moq;
using war.Exceptions;
using war.models;
using war.Requests;
using war.services;
using war.Store;
using Match = war.models.Match;

namespace war.test;

public class MatchServiceUnitTests
{
    private readonly IMatchService _service;
    private Mock<IMongoContext> _dbContext;
    private Mock<IMongoCollection<Player>> _playersCollectionMock;
    private Mock<IMongoCollection<Match>> _matchesCollectionMock;

    private List<Player> _players = new()
    {
        new Player()
        {
            Id="633305ab560339f10c42a44c"
        }
    };
    private List<Match> _matches = new()
    {   
        new Match()
        {
            Id="633305ab560339f10c42a44c", CreatedTime = DateTime.UtcNow, PlayerOne = new PlayerMatch()
            {
                PlayerId = "633305ab560339f10c42a44c"
            }
        }
    };

    public MatchServiceUnitTests()
    {
        _dbContext = new Mock<IMongoContext>();
        _playersCollectionMock = GetMockCollection<Player>(_players);
        _dbContext.Setup(s => s.Players).Returns(() => _playersCollectionMock.Object);
        
        _matchesCollectionMock = GetMockCollection<Match>(_matches);
        _dbContext.Setup(s => s.Matches).Returns(() => _matchesCollectionMock.Object);
        
        _service = new MatchService(_dbContext.Object);

    }

    private Mock<IMongoCollection<T>> GetMockCollection<T>(List<T> list)
    {
        var matchCursor = new Mock<IAsyncCursor<T>>();
        matchCursor.Setup(c => c.Current).Returns(list);
        matchCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(true)
            .Returns(false);
        matchCursor
            .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true))
            .Returns(Task.FromResult(false));

        var collectionMock = new Mock<IMongoCollection<T>>();

        collectionMock
            .Setup(x => x.FindAsync<T>(
                It.IsAny<ExpressionFilterDefinition<T>>(),
                null,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(matchCursor.Object);

        return collectionMock;
    }

    [Fact]
    
    public async Task MatchService_NewGame_RunsFineWithOrWOPlayerId()
    {
        var response = await _service.CreateMatch(null);
        Assert.True(response != null);
        
        response = await _service.CreateMatch(new PlayerRequest(){Id = "633305ab560339f10c42a44c"});
        Assert.True(response != null);
    }
    
    [Fact]
    public async Task MatchService_JoinGame_ThrowsNotFoundWhenMatchDoesNotExist()
    {
        _matchesCollectionMock = GetMockCollection<Match>(new List<Match>());   // <<- no matches are returned here
        await  Assert.ThrowsAsync<NotFoundException>(async () => await  _service.JoinMatch("a", null));
    }
    
    [Fact]
    public async Task MatchService_JoinGame_BothSpotsAreTaken()
    {
        _matchesCollectionMock = GetMockCollection<Match>(new List<Match>()
        {
            new Match()
            {
                PlayerTwo = new PlayerMatch()
                {
                    PlayerId = "633305ab560339f10c42a44c"
                }
            }
        });   // <<- no matches are returned here
        await  Assert.ThrowsAsync<ArgumentException>(async () => await  _service.JoinMatch("633305ab560339f10c42a440", null));
    }
    
    [Fact]
    public async Task MatchService_StartMatch_ThrowsNotFoundWhenMatchDoesNotExist()
    {
        _matchesCollectionMock = GetMockCollection<Match>(new List<Match>());   // <<- no matches are returned here
        await  Assert.ThrowsAsync<NotFoundException>(async () => await  _service.StartMatch("a", null));
    }
    
    [Fact]
    public async Task MatchService_DrawCard_ThrowsBadArgumentWhenMatchHasNotStarted()
    {
        _matchesCollectionMock = GetMockCollection<Match>(new List<Match>()
        {
            new Match()
            {
                StartTime = null,
                PlayerTwo = new PlayerMatch()
                {
                    PlayerId = "633305ab560339f10c42a44c"
                }
            }
        }); 
        await  Assert.ThrowsAsync<ArgumentException>(async () => await  _service.DrawCard("a", null));
    }
}