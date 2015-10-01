using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public class Hint
    {
        private Hint() {}
        public Hint(Player p, int? suit, int? num)
        {
            this.For = p;
            // Check hint content;
            if (suit.HasValue && num.HasValue) throw new ArgumentException("both dimensions");
            this.Suit = suit;
            this.Number = num;
            // Find matching card positions.
            this.Positions = new List<int>();
            var cards = p.Hand();
            for (int i = 0; i < cards.Count; i++)
            {
                bool match = num.HasValue ? num.Value == cards[i].Number : suit.Value == cards[i].Suit;
                if (match) this.Positions.Add(i);
            }
            if (Positions.Count <= 0) throw new ArgumentException("no matching cards");
        }

        public Player For { get; private set; }
        public int? Suit { get; private set; }
        public int? Number { get; private set; }
        public List<int> Positions { get; private set; }

        public override string ToString()
        {
            string data = Suit.HasValue ? "suit " + Suit.Value : "number " + Number.Value;
            string pos = String.Join(",", Positions.Select(p => p+1));
            return String.Format("'cards {0} = {1}'", pos, data);
        }
    }

    public class Game
    {
        const int FAILS = 3;
        const int HINTS = 8;

        public Game(int players, System.IO.TextWriter log)
        {
            this.Log = log;
            this.Deck = new Deck();
            this.Players = Enumerable.Range(0, players).Select(i => new Player(this, i)).ToList();
            this.Fails = FAILS;
            this.Hints = HINTS;
            this.FinalTurns = players;
        }
        private readonly System.IO.TextWriter Log;
        public Deck Deck { get; private set; }
        public List<Player> Players { get; private set; }
        public int Fails { get; private set; }
        public int Hints { get; private set; }
        public int FinalTurns { get; private set; }

        private bool Done() {
            if (this.Deck.Score() >= 25) return true;
            if (this.Fails <= 0) return true;
            if (this.FinalTurns <= 0) return true;
            return false;
        }

        public int Run()
        {
            // Deal initial cards.
            Log.WriteLine("Drawing initial cards... ");
            for (int i = 0; i < 5; i++)
            {
                foreach (Player p in this.Players)
                {
                    p.AddCard(this.Draw());
                }
            }

            // Let players play.
            int turn = 1;
            Player player = this.Players[0];
            while (!this.Done())
            {
                Log.Write("Turn {0}: player {1} ", turn++, player.Index + 1);
                player.Go();
                player = player.Next();
            }

            int score = this.Deck.Score();
            Log.WriteLine("SCORE: {0}", score);
            Log.WriteLine("");
            return score;
        }

        private Card Draw()
        {
            Card draw = this.Deck.Draw();
            if (draw == null)
            {
                Log.WriteLine("Deck empty...");
                this.FinalTurns--;
                return null;
            }
            else
            {
                draw.In = Card.Holder.PLAYER;
                return draw;
            }
        }

        public Card Play(Card c)
        {
            Log.Write("plays {0}... ", c.ToString());
            if (this.Deck.AllowPlay(c))
            {
                Log.WriteLine("OK!");
                c.In = Card.Holder.BOARD;
                if (c.Number == Card.NUMBERS && this.Hints < HINTS) this.Hints++;
            }
            else
            {
                Log.WriteLine("FAIL!");
                c.In = Card.Holder.DISCARD;
                this.Fails--;
            }
            return Draw();
        }

        public Card Discard(Card c) {
            Log.WriteLine("discards {0}", c.ToString());
            c.In = Card.Holder.DISCARD;
            if (this.Hints < HINTS) this.Hints++;
            return Draw();
        }

        public void Hint(Hint h)
        {
            if (this.Hints <= 0) throw new InvalidOperationException("no hints available");
            Log.WriteLine("hints to player {0}: {1}", h.For.Index + 1, h.ToString());
            h.For.GetHint(h);
            this.Hints--;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int games = 100;
            int total = 0;
            for (int i = 0; i < games; i++)
            {
                Game g = new Game(2, Console.Out);  // System.IO.TextWriter.Null
                total += g.Run();
            }
            Console.Out.WriteLine("MEAN SCORE: {0}", 1.0 * total / games);
        }
    }
}
