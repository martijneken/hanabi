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

        public Game(int p)
        {
            this.Deck = new Deck();
            this.Players = new List<Player>();
            for (int i = 0; i < p; i++)
            {
                this.Players.Add(new Player(this, i));
            }
            this.Fails = FAILS;
            this.Hints = HINTS;
            this.FinalTurns = p;
        }
        public Deck Deck { get; private set; }
        public List<Player> Players { get; private set; }
        public int Fails { get; private set; }
        public int Hints { get; private set; }
        public int FinalTurns { get; private set; }

        public bool Done() {
            if (this.Deck.Score() >= 25) return true;
            if (this.Fails <= 0) return true;
            if (this.FinalTurns <= 0) return true;
            return false;
        }

        public Card Draw()
        {
            Card draw = this.Deck.Draw();
            if (draw == null)
            {
                Console.Out.WriteLine("Deck empty...");
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
            Console.Out.Write("plays {0}... ", c.ToString());
            if (this.Deck.AllowPlay(c))
            {
                Console.Out.WriteLine("OK!");
                c.In = Card.Holder.BOARD;
            }
            else
            {
                Console.Out.WriteLine("FAIL!");
                c.In = Card.Holder.DISCARD;
                this.Fails--;
            }
            return Draw();
        }

        public Card Discard(Card c) {
            Console.Out.WriteLine("discards {0}", c.ToString());
            c.In = Card.Holder.DISCARD;
            if (this.Hints < HINTS) this.Hints++;
            return Draw();
        }

        public void Hint(Hint h)
        {
            if (this.Hints <= 0) throw new InvalidOperationException("no hints available");
            Console.Out.WriteLine("hints to player {0}: {1}", h.For.Index + 1, h.ToString());
            h.For.GetHint(h);
            this.Hints--;
        }
    }

    class Program
    {
        static int PlayGame()
        {
            Game g = new Game(2);

            // Deal initial cards.
            Console.Out.WriteLine("Drawing initial cards... ");
            for (int i = 0; i < 5; i++)
            {
                foreach (Player p in g.Players)
                {
                    p.AddCard(g.Draw());
                }
            }

            // Let players play.
            int turn = 1;
            int next = 0;
            while (!g.Done())
            {
                Console.Out.Write("Turn {0}: player {1} ", turn++, next + 1);
                Player p = g.Players[next];
                next = (next + 1) % g.Players.Count;

                p.Go();
            }

            int score = g.Deck.Score();
            Console.Out.WriteLine("SCORE: {0}", score);
            Console.Out.WriteLine("");
            return score;
        }

        static void Main(string[] args)
        {
            int games = 1;
            int total = 0;
            for (int i = 0; i < games; i++)
            {
                total += PlayGame();
            }
            Console.Out.WriteLine("MEAN SCORE: {0}", 1.0 * total / games);
        }
    }
}
