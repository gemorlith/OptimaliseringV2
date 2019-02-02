using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace GroteOpdrachtV2 {
    public abstract class SearchType {
        public virtual void Search(Random rng, int[,] paths, StartSolutionGenerator gen, List<ValuePerNeighbour> neighborOptions, OrderPosition[] allPositions, List<Order> allOrders) { }
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
        public override void Search(Random rng, int[,] paths, StartSolutionGenerator gen, List<ValuePerNeighbour> neighborOptions, OrderPosition[] allPositions, List<Order> allOrders)
        {
            for (int i = 0; i < 10000; i++) {
                Solution s = gen.Generate(rng, paths, allPositions, allOrders);
                if (s.Value < Program.minValue) {
                    Program.minValue = s.Value;
                    Util.SaveSolution(s);
                }
            }
        }
    }

    public abstract class Localsearch : SearchType {
        protected int counter = 0;
        public override void Search(Random rng, int[,] paths, StartSolutionGenerator gen, List<ValuePerNeighbour> neighborOptions, OrderPosition[] allPositions, List<Order> allOrders) {
            SearchFrom(rng, paths, gen.Generate(rng, paths, allPositions, allOrders), allPositions, neighborOptions);
        }
        public virtual void SearchFrom(Random rng, int[,] paths, Solution s, OrderPosition[] allPositions, List<ValuePerNeighbour> neighborOptions) {
            Compare(s);
            while (counter < Program.maxIterations) {
                //Util.Test(s, "Voor TryNeighbour", false);
                //if (Keyboard.IsKeyDown(Key.Escape)) // Maybe add a safe quit functionality?
                TryNeighbour(rng, s, paths, allPositions, neighborOptions);
                Compare(s);
                counter++;
                if (counter % Program.printFreq == 0) {
                    if (counter % (Program.printFreq * Program.saveFreq) == 0) {
                        Util.Test(s, paths, allPositions, "occasionalTest", true);
                        Util.SaveSolution(s, "../../Solutions/Temp.txt");
                        Console.WriteLine("Opgeslagen in Temp.txt");
                    }
                    Console.WriteLine(StatusString(s));
                }
            }
            Reset();
            counter = 0;
        }
        protected virtual string StatusString(Solution s) {
            return (counter / Program.printFreq) + "/" + Program.MaxPrint + " cr:" + (int)s.Value;
        }
        protected virtual void Reset() { }
        public abstract void TryNeighbour(Random rng, Solution s, int[,] paths, OrderPosition[] allPositions, List<ValuePerNeighbour> neighborOptions);
    }

    public class SimulatedAnnealingMK1 : Localsearch {
        protected double QLeft = 1;
        protected float T = Program.annealingStartT;

        public override void TryNeighbour(Random rng, Solution s, int[,] paths, OrderPosition[] allPositions, List<ValuePerNeighbour> neighborOptions) {
            NeighbourSpace ns = Util.NeighbourTypeFromRNG(rng, neighborOptions);
            if (ns.IsEmpty(s)) return;
            UpdateQ(s);
            Neighbour n = ns.RndNeighbour(rng, allPositions, s);
            if (n == null) return;
            double oldValue = s.Value;
            double opv = s.penaltyValue;
            double odv = s.declineValue;
            double otv = s.timeValue;
            n.Apply(paths);
            double gain = s.Value - oldValue;
            if (gain >= 0f && !ApplyNegativeAnyways(rng, gain)) {
                if (gain != 0f || n.ShadowGain() > 0) {
                    double newValue = s.Value;
                    n.Reverse().Apply(paths);
                }
                //if (s.Value != oldValue) Util.Test(s, "In TryNeighbour", false);
            }
        }
        protected override void Reset() {
            QLeft = 1;
            T = Program.annealingStartT;
            Program.overTimePenalty = Program.overTimePenaltyBase;
            Program.overWeightPenalty = Program.overWeightPenaltyBase;
            Program.wrongFreqPenalty = Program.wrongFreqPenaltyBase;
            Program.wrongDayPentalty = Program.wrongDayPentaltyBase;
        }
        protected override string StatusString(Solution s) {
            return base.StatusString(s) + " T: " + T;
        }
        private void UpdateQ(Solution s) {
            QLeft -= 1 / (double)(Program.complexityEstimate * Program.annealingQPerNSSize);
            if (QLeft < 0) {
                QLeft = 1;
                T *= Program.alpha;
                Program.overWeightPenalty *= Program.weightPenInc;
                Program.overTimePenalty *= Program.timePenInc;
                Program.wrongFreqPenalty *= Program.freqPenInc;
                Program.wrongDayPentalty *= Program.dayPenInc;
                s.freqPen *= Program.freqPenInc;
                s.timePen *= Program.timePenInc;
                s.weightPen *= Program.weightPenInc;
                s.wrongDayPen *= Program.dayPenInc;
            }
        }
        private bool ApplyNegativeAnyways(Random r, double gain) {
            double rnd = r.NextDouble();
            return (rnd < Math.Exp(-gain / T));
        }

    }
}
