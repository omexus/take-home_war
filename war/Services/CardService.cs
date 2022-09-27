namespace war.services;

public interface ICardService
{
    List<int> ShuffleCards();

    /// <summary>
    /// This will divide one-by-one all shuffled cards to both players
    /// </summary>
    /// <param name="shuffledCards"></param>
    /// <param name="playerOneCards"></param>
    /// <param name="playerTwoCards"></param>
    void DivideCardsToPlayers(List<int> shuffledCards, ICollection<int> playerOneCards, ICollection<int> playerTwoCards);
}

public class CardService : ICardService
{
    public List<int> ShuffleCards()
    {
        var allCardsInDict = new Dictionary<int, int>();
        var shuffledCards = new List<int>();
        //populate a dictionary with all possible values
        
        var cardIndex = 1;
        var cardInDeck = 2; //we start at '2' as the lowest value card ('Ace' is 14)
        foreach (var card in Enumerable.Range(1,52))
        {
            allCardsInDict.Add(card, cardInDeck);
            //reset cardIndex every cardType
            if (cardIndex % 13 == 0)
            {
                cardInDeck = 2;
            }
            else
            {
                cardInDeck++;
            }

            cardIndex++;
        }
        
        //at this point we have 'allCards' to contain [1]=2,[2]=3...[10]=11..[13]=14..[52]=14

        //get a random 'position' for each position out
        for (var availableCards = Enumerable.Range(1,52).Count(); availableCards > 0; availableCards--)
        {
            //get a random position from 1 to 'availableCards'
            var randPos = GetRandomCard(1, availableCards);
            
            //get the card at the random position from dictionary
            shuffledCards.Add(allCardsInDict[randPos]);   //O(1) read/addition

            //reduce the available cards
            if (randPos < availableCards)
            {
                //copy the last item in the dictionary (accessing it by its positions) so that we delete it
                //because there will be one less card avail but we still want that last card available so we relocate it 
                allCardsInDict[randPos] = allCardsInDict[availableCards];
            }

            //remove it
            allCardsInDict.Remove(availableCards);   //O(1) removal
        }

        return shuffledCards;
    }
    
    private int GetRandomCard(int start, int end)
    {
        var r = new Random();
        return r.Next(start, end + 1);
    }

    /// <summary>
    /// This will divide one-by-one all shuffled cards to both players
    /// </summary>
    /// <param name="shuffledCards"></param>
    /// <param name="playerOneCards"></param>
    /// <param name="playerTwoCards"></param>
    public void DivideCardsToPlayers(List<int> shuffledCards, ICollection<int> playerOneCards, ICollection<int> playerTwoCards)
    {
        for (var i = 0; i< shuffledCards.Count ; i++)
        {
            if ( (i+1) % 2 == 0)
            {
                playerOneCards.Add(shuffledCards[i]);
            }
            else
            {
                playerTwoCards.Add(shuffledCards[i]);
            }
        }
    }


}

public class CardNode
{
    public int Card;

    public CardNode Next;

    public CardNode(int card, CardNode next)
    {
        Card = card;
        Next = next;
    }
}