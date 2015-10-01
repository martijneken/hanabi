using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public class Move
    {
        public HeldCard Play { get; private set; }
        public HeldCard Discard { get; private set; }
        public Hint Hint { get; private set; }

        // Factory methods.
        private Move() {}
        public static Move DoPlay(HeldCard c) { return new Move{ Play = c }; }
        public static Move DoDiscard(HeldCard c) { return new Move { Discard = c }; }
        public static Move DoHint(Hint h) { return new Move { Hint = h }; }
    }
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

        public Game Game { get; private set; }
        public int Index { get; private set; }
        public List<HeldCard> Cards { get; private set; }

        // Public-facing hand of cards.
        public List<Card> Hand() { return this.Cards.Select(c => c.Actual).ToList(); }

        // Next player in the order.
        public Player Next() { return this.Game.Players[(this.Index + 1) % this.Game.Players.Count]; }

        public void Go()
        {
            Move m = this.Strategy.Play(this);
            if (m.Play != null)
            {
                this.Cards.Remove(m.Play);
                this.AddCard(this.Game.Play(m.Play.Actual));
            }
            else if (m.Discard != null)
            {
                this.Cards.Remove(m.Discard);
                this.AddCard(this.Game.Discard(m.Discard.Actual));
            }
            else
            {
                this.Game.Hint(m.Hint);
            }
        }

        public void GetHint(Hint h)
        {
            // TODO: store a history of received hints ... do we need a full move history?
            for (int i = 0; i < this.Cards.Count; i++)
            {
                HeldCard card = this.Cards[i];
                bool match = h.Positions.Contains(i);
                int value = h.Number.HasValue ? h.Number.Value : h.Suit.Value;
                Func<Card, int> get = c => h.Number.HasValue ? c.Number : c.Suit;
                // If the hint matches this card, remove all other values.
                // If the hint does not match this card, remove the hinted value.
                card.Possible.RemoveAll(c => match ? get(c) != value : get(c) == value);
            }
        }

        public void AddCard(Card c)
        {
            if (c == null) return;
            this.Cards.Add(new HeldCard(c));
        }
    }

    interface IStrategy
    {
        // Do something when it's your turn.
        Move Play(Player me);
    }

    public class AlwaysPlay : IStrategy
    {
        public Move Play(Player me) { return Move.DoPlay(me.Cards[0]); }
    }
    public class AlwaysDiscard : IStrategy
    {
        public Move Play(Player me) { return Move.DoDiscard(me.Cards[0]); }
    }
    public class Steller : IStrategy
    {
        public Move Play(Player me)
        {
            // TODO: narrow down card possibilities by process of elimination
            // TODO: include incomplete knowledge of own hand (known numbers, known suits).

            // Re-evaluate all cards based on new information in hand, board, and opponents' hands.
            foreach (HeldCard c in me.Cards)
            {
                c.Label = c.State(me);
            }

            // TODO: override states based on last hint (requires history)
            // TODO: immediately PLAY on recent hints according to position?
            // Keep any queued cards that have a known number or suit (likely received a hint).
            foreach (HeldCard c in me.Cards.Where(c =>
                c.Label == Intent.QUEUE && (c.Number().HasValue || c.Suit().HasValue)))
            {
                c.Label = Intent.KEEP;
            }

            Player next = me.Next();

            // Send critical hints, if possible.
            if (me.Game.Hints > 0)
            {
                // For playable cards, or cards wrongly marked playable, hint number, then suit (top of queue first).
                foreach (HeldCard c in next.Cards)
                {
                    bool canPlay = me.Game.Deck.AllowPlay(c.Actual);
                    if ((canPlay && c.Label != Intent.PLAY) ||
                        (!canPlay && c.Label == Intent.PLAY))
                    {
                        if (!c.Number().HasValue) return Move.DoHint(new Hint(next, null, c.Actual.Number));
                        if (!c.Suit().HasValue) return Move.DoHint(new Hint(next, c.Actual.Suit, null));
                    }
                }
                // For pole cards that shouldn't be discarded, hint suit. This should move them into state KEEP.
                var pole = next.Cards.FirstOrDefault(c => c.Label == Intent.QUEUE);
                if (pole != null && pole.Actual.LastChance(next, me))
                {
                    //if (!pole.Number().HasValue) return Move.DoHint(new Hint(next, null, pole.Actual.Number));
                    if (!pole.Suit().HasValue) return Move.DoHint(new Hint(next, pole.Actual.Suit, null));
                }
            }

            // Play cards that are ready (lowest number first).
            HeldCard play = me.Cards.Where(c => c.Label == Intent.PLAY)
                .OrderBy(c => c.Number().GetValueOrDefault(int.MaxValue))
                .FirstOrDefault();
            if (play != null) return Move.DoPlay(play);

            // Discard when we have cards ready to go (any order).
            HeldCard disc = me.Cards.FirstOrDefault(c => c.Label == Intent.DISCARD);
            if (disc != null) return Move.DoDiscard(disc);

            // TODO: send non-critical hints, like suit, especially when many hints are available
            // TODO: special hints for 5s?

            // Otherwise discard the first card from queue.
            HeldCard discq = me.Cards.FirstOrDefault(c => c.Label == Intent.QUEUE);
            if (discq != null) return Move.DoDiscard(discq);

            // TODO: Non-critical hint?

            // If we really have to, discard any card from the keep stack.
            // TODO: discard the _safest_ card (highest, or least played).
            return Move.DoPlay(me.Cards[0]);
        }
    }
}
