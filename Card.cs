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

        public static void RestrictCards(this Player me)
        {
            // Find the cards visible to the player: down or in others' hands.
            var others = me.Game.Players.Where(p => p != me).Select(p => p.Hand());
            var visible = me.Game.Deck.Down().Concat(others.SelectMany(c => c));
            // Derive any 'closed' cards (those definitely not in own hand).
            var counts = visible.GroupBy(c => c.ToString(), (key, g) => new { Card = g.First(), Count = g.Count() });
            var closed = new HashSet<Card>(counts.Where(g => g.Card.Copies() == g.Count).Select(g => g.Card), Compare);
            if (closed.Count == 0) return;

            // Narrow down the options for each 
            foreach (HeldCard held in me.Cards)
            {
                held.Possible.RemoveAll(c => closed.Contains(c));
            }

            // TODO: include incomplete knowledge of own hand (known numbers, known suits).
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
            foreach (Card c in card.Possible)
            {
                if (!next && !played && !stuck) break; // optimization

                int nxt = me.Game.Deck.NextPlay(c.Suit);
                int? stuckat = me.Game.Deck.StuckAt(c.Suit);

                if (nxt != c.Number) next = false;
                if (nxt <= c.Number) played = false;
                if (!stuckat.HasValue || stuckat.Value > c.Number) stuck = false;
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
            // If it's already been played, it's not needed at all.
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
        public static readonly int SUITS = 5;
        public static readonly int NUMBERS = 5;
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
            this.Possible = new List<Card>();
            for (int s = 1; s <= Card.SUITS; s++)
            {
                for (int n = 1; n <= Card.NUMBERS; n++)
                {
                    this.Possible.Add(new Card { Suit = s, Number = n });
                }
            }
            this.Label = Intent.QUEUE;
        }

        public Card Actual { get; private set; }
        public List<Card> Possible { get; private set; }
        public Intent Label { get; set; }
        // TODO: if strategies get savvier, we may need internal and external Possible, to avoid leaking info

        public bool Known()
        {
            return Possible.Count <= 1;
        }
        public int? Suit()
        {
            var suits = Possible.Select(c => c.Suit).Distinct();
            return suits.Count() > 1 ? (int?)null : suits.First();
        }
        public int? Number()
        {
            var nums = Possible.Select(c => c.Number).Distinct();
            return nums.Count() > 1 ? (int?)null : nums.First();
        }
    }
}
