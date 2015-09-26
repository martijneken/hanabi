using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public class Player
    {
        public Player(Game g, int idx)
        {
            this.Strategy = new Steller();
            this.Game = g;
            this.Index = idx;
            this.Cards = new List<HeldCard>();
        }
        private IStrategy Strategy;
        private Game Game;

        public int Index { get; private set; }
        public List<HeldCard> Cards { get; private set; }

        // Public-facing hand of cards.
        public List<Card> Hand() { return this.Cards.Select(c => c.Actual).ToList(); }

        public void Go()
        {
            this.Strategy.Play(this);
        }

        public void GetHint(Hint h)
        {
            foreach (int idx in h.Positions)
            {
                HeldCard c = this.Cards[idx];
                if (h.Number.HasValue)
                {
                    c.Numbers.Clear();
                    c.Numbers.Add(h.Number.Value);
                }
                else
                {
                    c.Suits.Clear();
                    c.Suits.Add(h.Number.Value);
                }
            }
        }

        public void AddCard(Card c)
        {
            if (c == null) return;
            this.Cards.Add(new HeldCard(c));
            this.Strategy.OnAdd(this);
        }

        public void Play(HeldCard c)
        {
            this.Cards.Remove(c);
            this.AddCard(this.Game.Play(c.Actual));
        }
        public void Discard(HeldCard c)
        {
            this.Cards.Remove(c);
            this.AddCard(this.Game.Discard(c.Actual));
        }
    }

    interface IStrategy
    {
        // Do something with a card added at last position. Perhaps reorder.
        void OnAdd(Player p);
        // Do something when it's your turn.
        void Play(Player p);
    }

    public class AlwaysPlay : IStrategy
    {
        public void OnAdd(Player p) {}
        public void Play(Player p) { p.Play(p.Cards[0]); }
    }
    public class AlwaysDiscard : IStrategy
    {
        public void OnAdd(Player p) {}
        public void Play(Player p) { p.Discard(p.Cards[0]); }
    }
    public class Steller : IStrategy
    {
        public void OnAdd(Player p)
        {
            // put card at the rightmost non-keep, non-play, non-discard spot
        }
        public void Play(Player p)
        {
            // re-evaluate HeldCard labels
            // hint when the opponent needs it (compute some score)
            // play when we have cards ready to go
            // discard when we have cards ready to go
            // otherwise discard from queue
            p.Play(p.Cards[0]);
        }
    }
}
