using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    // Compare cards based on values rather than identity.
    public class CardComparer : IEqualityComparer<Card>
    {
        public bool Equals(Card x, Card y)
        {
            return x.Suit == y.Suit && x.Number == y.Number;
        }
        public int GetHashCode(Card obj)
        {
            return obj.GetHashCode();
        }
    }
    
    public static class CardExtensions
    {
        private static CardComparer Compare = new CardComparer();

        public static int CountSame(this IEnumerable<Card> cards, Card card)
        {
            return cards.Count(c => Compare.Equals(c, card));
        }

        /// <summary>
        /// Calculate a card's state based on given information only.
        /// </summary>
        public static Intent State(this HeldCard card, Player me)
        {
            // Check each possible variety of the card for playability, discardability.
            var down = me.Game.Deck.Down();
            bool next = true;
            bool played = true;
            bool stuck = true;
            foreach (int s in card.Suits)
            {
                if (!next && !played && !stuck) break; // optimization

                int nxt = me.Game.Deck.NextPlay(s);
                int? stuckat = me.Game.Deck.StuckAt(s);
                foreach (int n in card.Numbers)
                {
                    if (nxt != n) next = false;
                    if (nxt <= n) played = false;
                    if (!stuckat.HasValue || stuckat.Value > n) stuck = false;
                }
            }
            // If all states match playability or discardability, change the state.
            if (next) return Intent.PLAY;
            if (played) return Intent.DISCARD;
            if (stuck) return Intent.DISCARD;
            // Otherwise keep existing label.
            return card.Label;
        }

        /// <summary>
        /// Calculate whether a card in another player's hand is unique to play (and should not be discarded).
        /// </summary>
        public static bool LastChance(this Card card, Player p, Player me)
        {
            // If player has multiple instances in hand, it's not a last chance.
            int held = p.Hand().CountSame(card);
            if (held > 1) return false;
            // If it's already on the board, it's not needed at all.
            int board = p.Game.Deck.Board().CountSame(card);
            if (board > 0) return false;
            // Last chance if all copies of this card have been seen.
            int down = p.Game.Deck.Down().CountSame(card);
            int mine = me.Cards.Count(c => c.Known() && Compare.Equals(c.Actual, card));
            return down + held + mine == card.Copies();

            // TODO: include incomplete knowledge of own hand (known numbers, known suits).
        }
    }

    /// <summary>
    /// An actual card in the game, and its state of play.
    /// </summary>
    public class Card
    {
        public static int Suits() { return 5; }
        public static int Numbers() { return 5; }
        public static int Copies(int number)
        {
            switch (number)
            {
                case 1: return 3;
                case 5: return 1;
                default: return 2;
            }
        }

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

        public int Copies() { return Card.Copies(this.Number); }

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
        // TODO: should represent this as a suits x numbers matrix!
        public List<int> Suits { get; private set; }
        public List<int> Numbers { get; private set; }
        public Intent Label { get; set; }

        public bool Known() { return Suits.Count <= 1 && Numbers.Count <= 1; }
        public int? Suit() { return Suits.Count > 1 ? (int?)null : Suits[0]; }
        public int? Number() { return Numbers.Count > 1 ? (int?)null : Numbers[0]; }
    }
}
