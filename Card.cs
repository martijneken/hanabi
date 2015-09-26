using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    /// <summary>
    /// An actual card in the game, and its state of play.
    /// </summary>
    public class Card
    {
        public enum Holder
        {
            DECK,
            PLAYER,
            BOARD,
            DISCARD,
        }

        public int Suit { get; set; }
        public int Number { get; set; }
        public Holder In { get; set; }

        public override string ToString()
        {
            return String.Format("{0}:{1}", this.Suit, this.Number);
        }
    }

    /// <summary>
    /// A player's public signal of intent regarding a HeldCard.
    /// </summary>
    public enum Intent
    {
        PLAY,
        DISCARD,
        KEEP,
        QUEUE,
    }

    /// <summary>
    /// A card held by a player, and the player's knowledge about that card.
    /// </summary>
    public class HeldCard
    {
        public HeldCard(Card a)
        {
            this.Actual = a;
            this.Suits = new List<int>() { 1, 2, 3, 4, 5 };
            this.Numbers = new List<int>() { 1, 2, 3, 4, 5 };
            this.Label = Intent.QUEUE;
        }

        public Card Actual { get; private set; }
        public List<int> Suits { get; private set; }
        public List<int> Numbers { get; private set; }
        public Intent Label { get; set; }

        public int? Suit() { return Suits.Count > 1 ? (int?)null : Suits[0]; }
        public int? Num() { return Numbers.Count > 1 ? (int?)null : Numbers[0]; }
    }
}
