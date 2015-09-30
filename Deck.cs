using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public static class DeckExtensions
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class Deck
    {
        // Create shuffled deck.
        public Deck()
        {
            all = new List<Card>();
            for (int s = 1; s <= Card.SUITS; s++)
            {
                for (int n = 1; n <= Card.NUMBERS; n++)
                {
                    for (int i = 0; i < Card.Copies(n); i++)
                    {
                        all.Add(new Card { Suit = s, Number = n, In = Card.Holder.DECK });
                    }
                }
            }
            all.Shuffle();
        }
        List<Card> all;

        public int Score() { return all.Count(c => c.In == Card.Holder.BOARD); }
        public int Depth() { return all.Count(c => c.In == Card.Holder.DECK); }
        public Card Draw() { return all.FirstOrDefault(x => x.In == Card.Holder.DECK); }

        public int? StuckAt(int suit)
        {
            for (int n = NextPlay(suit); n <= Card.NUMBERS; n++)
            {
                Card need = new Card { Suit = suit, Number = n };
                if (Down().CountSame(need) == need.Copies()) return n;
            }
            return null; // not stuck
        }
        public int NextPlay(int suit)
        {
            return all.Max(c => c.In == Card.Holder.BOARD && c.Suit == suit ? c.Number : 0) + 1;
        }
        public bool AllowPlay(Card n)
        {
            return NextPlay(n.Suit) == n.Number;
        }
        public IEnumerable<Card> Down()
        {
            return all.Where(c => c.In == Card.Holder.BOARD || c.In == Card.Holder.DISCARD);
        }
        public IEnumerable<Card> Board()
        {
            return all.Where(c => c.In == Card.Holder.BOARD);
        }
    }
}
