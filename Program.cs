using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    public class Hint
    {
        public int? Suit;
        public int? Number;
        public List<int> Positions;

        public override string ToString()
        {
            string data = Suit.HasValue ? "suit " + Suit.Value : "number " + Number.Value;
            string pos = String.Join(",", Positions.Select(p => p+1));
            return String.Format("'cards {0} = {1}'", pos, data);
        }
    }

    public class Game
    {
        public Game(int p)
        {
            this.Deck = new Deck();
            this.Players = new List<Player>();
            for (int i = 0; i < p; i++)
            {
                this.Players.Add(new Player(this, i));
            }
            this.Fails = 3;
            this.Hints = 8;
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
            this.Hints++;
            return Draw();
        }

        public void Hint(Hint h, int idx)
        {
            Console.Out.Write("hints to player {0}: {1}", idx + 1, h.ToString());
            this.Players[idx].GetHint(h);
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
