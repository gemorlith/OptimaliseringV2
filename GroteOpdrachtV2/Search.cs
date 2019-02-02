﻿using System;
using System.Windows.Input;

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

    public class BruteForce : SearchType {
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

    public abstract class LocalSearch : SearchType {
        protected long counter = 0;
        public override void Search() {
            SearchFrom(Program.Generator.Generate());
        }
        public virtual void SearchFrom(Solution s) {
            Compare(s);
            while (counter < Program.maxIterations) {
                //Util.Test(s, "Voor TryNeighbour", false);
                //if (Keyboard.IsKeyDown(Key.Escape)) // Maybe add a safe quit functionality?
                TryNeighbour(s);
                Compare(s);
                counter++;
                if (counter % Program.printFreq == 0) {
                    if (counter % (Program.printFreq * Program.saveFreq) == 0) {
                        Util.Test(s, "occasionalTest", true);
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
        public abstract void TryNeighbour(Solution s);
    }

    public class SimulatedAnnealingMK1 : LocalSearch {
        protected double QLeft = 1;
        protected float T = Program.annealingStartT;

        public override void TryNeighbour(Solution s) {
            NeighbourSpace ns = Util.NeighbourTypeFromRNG();
            if (ns.IsEmpty(s)) return;
            UpdateQ(s);
            Neighbour n = ns.RndNeighbour(s);
            if (n == null) return;
            double oldValue = s.Value;
            double opv = s.penaltyValue;
            double odv = s.declineValue;
            double otv = s.timeValue;
            n.Apply();
            double gain = s.Value - oldValue;
            if (gain >= 0f && !ApplyNegativeAnyways(gain)) {
                if (gain != 0f || n.ShadowGain() > 0) {
                    double newValue = s.Value;
                    n.Reverse().Apply();
                }
                //if (s.Value != oldValue) Util.Test(s, "In TryNeighbour", false);
            }
            else {
                int kaas = 103765;
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
                Program.overWeightPenalty *= Program.weightPenMult;
                Program.overTimePenalty *= Program.timePenMult;
                Program.wrongFreqPenalty *= Program.freqPenMult;
                Program.wrongDayPentalty *= Program.dayPenMult;
                s.freqPen *= Program.freqPenMult;
                s.timePen *= Program.timePenMult;
                s.weightPen *= Program.weightPenMult;
                s.wrongDayPen *= Program.dayPenMult;
            }
        }
        private bool ApplyNegativeAnyways(double gain) {
            double rnd = Util.Rnd;
            return (rnd < Math.Exp(-gain / T));
        }

    }
}
