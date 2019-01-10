using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOpdrachtV2 {
    public abstract class SearchType {
        public virtual void Search() { }
        public void Compare(Solution s) {
            if (s.Value < Program.minValue && s.penaltyValue == 0) {
                Program.minValue = s.Value;
                Console.WriteLine("Better solution found: " + s.Value);
                Util.SaveSolution(s);
            }
            if (s.Value < Program.unviableMinValue && s.penaltyValue >= 0) {
                Program.unviableMinValue = s.Value;
                if (s.penaltyValue > 0)
                    Console.WriteLine("unv. sol.: ±" + (int)s.Value);
                Util.SaveSolution(s, "../../Solutions/UnviableBest.txt");
            }
        }
    }

    public class Bruteforce : SearchType {
        public override void Search() {
            for (int i = 0; i < 10000; i++) {
                Solution s = Program.Generator.Generate();
                if (s.Value < Program.minValue) {
                    Program.minValue = s.Value;
                    Util.SaveSolution(s);
                }
            }
        }
    }

    public abstract class Localsearch : SearchType {
        protected int counter = 0;
        public override void Search() {
            SearchFrom(Program.Generator.Generate());
        }
        public virtual void SearchFrom(Solution s) {
            Compare(s);
            while (counter < Program.maxIterations) {
                TryNeighbour(s);
                Compare(s);
                counter++;
                if (counter % Program.pasteFreq == 0) {
                    if (counter % (Program.pasteFreq * Program.saveFreq) == 0) {
                        Util.SaveSolution(s, "../../Solutions/Temp.txt");
                        Console.WriteLine("Saved in Temp.txt");
                    }
                    Console.WriteLine(StatusString(s));
                }
            }
            Reset();
            counter = 0;
        }
        protected virtual string StatusString(Solution s) {
            return (counter / Program.pasteFreq) + "/" + Program.maxIterations / Program.pasteFreq + " cr:" + (int)s.Value;
        }
        protected virtual void Reset() { }
        public abstract void TryNeighbour(Solution s);
    }

    public class SimulatedAnnealingMK1 : Localsearch {
        protected double QLeft = 1;
        protected float T = Program.annealingStartT;

        public override void TryNeighbour(Solution s) {
            NeighbourSpace ns = Util.NeighbourTypeFromRNG();
            if (ns.IsEmpty(s)) return;
            UpdateQ();
            Neighbour n = ns.RndNeighbour(s);
            if (n == null) return;
            double oldValue = s.Value;
            n.Apply();
            double gain = oldValue - s.Value;
            if (gain > 0f && !ApplyNegativeAnyways(gain)) {
                n.Reverse().Apply();
                if (s.Value != oldValue) throw new Exception("values aren't equal, incorrect reversion? or rounding error?");
            }
        }
        protected override void Reset() {
            QLeft = 1;
            T = Program.annealingStartT;
        }
        protected override string StatusString(Solution s) {
            return base.StatusString(s) + " T: " + T;
        }
        private void UpdateQ() {
            QLeft -= 1 / (double)(Program.complexityEstimate * Program.annealingQPerNSSize);
            if (QLeft < 0) {
                QLeft = 1;
                T *= Program.alpha;
            }
        }
        private bool ApplyNegativeAnyways(double gain) {
            double rnd = Util.Rnd;
            return (rnd < Math.Exp(-gain / T));
        }

    }
}
