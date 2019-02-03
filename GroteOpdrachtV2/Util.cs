﻿using System;
using System.Collections.Generic;
using System.IO;

namespace GroteOpdrachtV2 {
    class Util {
        // Property for getting a random value between 0 and 1
        public static double Rnd { get { return Program.random.NextDouble(); } }
        // Property for getting a random boolean
        public static bool RndBool { get { return Program.random.Next(0, 2) == 1; } }
        // Function for saving a Solution class as a .txt file
        public static void SaveSolution(Solution s, string path = "../../Solutions/BestSolution.txt") {
            StreamWriter sw = new StreamWriter(path) { AutoFlush = true };
            int counter = 0;
            for (int d = 1; d <= 5; d++) {
                for (int t = 1; t <= 2; t++) {
                    int max = s.cycles[t - 1, d - 1].Count;
                    for (int c = 0; c < max; c++) {
                        OrderPosition current = s.cycles[t - 1, d - 1][c].first;
                        bool active = false;
                        while (current != null) {
                            if (!current.Active) {
                                current = current.Next;
                                continue;
                            }
                            active = true;
                            sw.Write(t); // Vehicle
                            sw.Write(';');
                            sw.Write(d); // Day
                            sw.Write(';');
                            sw.Write(counter + 1); // Sequence number
                            sw.Write(';');
                            sw.Write(current.order.ID); // Order ID
                            sw.WriteLine();
                            counter++;
                            current = current.Next;
                        }
                        if (active) {
                            sw.Write(t); // Vehicle
                            sw.Write(';');
                            sw.Write(d); // Day
                            sw.Write(';');
                            sw.Write(counter + 1); // Sequence number
                            sw.Write(';');
                            sw.Write(0); // Order ID
                            sw.WriteLine();
                            counter++;
                        }
                    }
                    counter = 0;
                }
            }
            sw.Close();
        }
        // Function for getting a random NeighbourSpace from the list of chances initialised in Program.Main()
        public static NeighbourSpace NeighbourTypeFromRNG() {
            double counter = 0;
            double random = Rnd;
            foreach (ValuePerNeighbour vpn in Program.neighbourOptions) {
                counter += vpn.value;
                if (counter > random)
                    return vpn.type;
            }
            throw new Exception("Shit's broken: total chance of neighbourType < 1");
        }
        // Function for getting the time needed to get from the Order at location 'from' to the Order at location 'to'
        public static int PathValue(short from, short to) {
            if (from == to) return 0;
            return Program.paths[from, to];
        }
        // Function for testing whether the values used in computing the score of the current Solution are still correct (only for debugging purposes)
        public static bool Test(Solution s, string message = "", bool print = true) {
            if (print) Console.WriteLine(message);
            bool different = false;
            double tv = 0, dv = 0, tp = 0, wp = 0;
            double[,] locTim = new double[2, 5];
            Dictionary<Cycle, int> cycWe = new Dictionary<Cycle, int>();
            foreach (OrderPosition op in Program.allPositions) {
                if (op.Active) {
                    locTim[op.truck, op.Day] += PathValue(op.order.Location, s.NextActive(op).Location);
                    locTim[op.truck, op.Day] += op.order.Time;
                    if (!cycWe.ContainsKey(op.cycle)) cycWe.Add(op.cycle, 0);
                    cycWe[op.cycle] += op.order.ContainerVolume;
                    if (!s.allCycles.Contains(op.cycle)) {
                        Console.WriteLine("OhNo");
                    }
                }
                else {
                    dv += op.order.Time * 3;
                }
            }
            foreach (Cycle c in s.allCycles) {
                Order act;
                if (c.first.Active) act = c.first.order;
                else {
                    act = s.NextActive(c.first);
                }
                locTim[c.truck, c.day] += PathValue(Program.Home, act.Location);
                if (act != Program.HomeOrder) locTim[c.truck, c.day] += Program.DisposalTime;

                if (cycWe.ContainsKey(c)) wp += Program.overWeightPenalty * Math.Max(cycWe[c] - Program.MaxCarry, 0);
            }
            for (int t = 0; t < 2; t++) {
                for (int d = 0; d < 5; d++) {
                    tv += locTim[t, d];
                    tp += Program.overTimePenalty * Math.Max(locTim[t, d] - Program.MaxTime, 0);
                }
            }
            if (!TheSameISwear(tv, s.timeValue)) { Console.WriteLine("TimeValue should be " + tv + " but it is " + s.timeValue + "; difference: " + -(tv - s.timeValue)); different = true; }
            if (!TheSameISwear(dv, s.declineValue)) { Console.WriteLine("DeclineValue should be " + dv + " but it is " + s.declineValue + "; difference: " + -(dv - s.declineValue)); different = true; }
            if (!TheSameISwear(tp, s.timePen)) { Console.WriteLine("TimePenalty should be " + tp + " but it is " + s.timePen + "; difference: " + -(tp - s.timePen)); different = true; }
            if (!TheSameISwear(wp, s.weightPen)) { Console.WriteLine("WeightPenalty should be " + wp + " but it is " + s.weightPen + "; difference: " + -(wp - s.weightPen)); different = true; }
            // WRONG DAY PENALTIES AND WRONG FREQUENCY PENALTIES NOT IN HERE YET
            if (different) {
                Console.WriteLine("One or more values were inequal; check console.");
            }
            return different;
        }
        // Function for checking whether two values are equal, independent of rounding errors
        public static bool TheSameISwear(double one, double theOther) {
            return one < theOther + .5 && one > theOther - .5;
        }
        // Function for finding the increase in FrequencyPenaltyAmounts of an Order
        public static int IncreaseFreqPenAmount(Order o) {
            int last = o.LastFreqPenAmount;
            int newFPA = FreqPenAmount(o.ActiveFreq, o.Frequency);
            o.LastFreqPenAmount = newFPA;
            return newFPA - last;
        }
        // Function for finding 'how wrong' the current frequency of an Order is
        public static int FreqPenAmount(int currentFreq, int desiredFreq) {
            return Math.Min(currentFreq, desiredFreq - currentFreq);
        }
        // Function for finding the increase in InvalidDayPlanning of an Order
        public static int IncreaseInvalidDayPlanning(Order o) {
            int last = o.LastInvalidPlan;
            int[] plannedDays = new int[o.ActiveFreq];
            int d = 0;
            foreach (OrderPosition pos in o.Positions) {
                if (pos.Active) {
                    plannedDays[d] = pos.Day + 1;
                    d++;
                }
            }
            int newVDP;
            if (InvalidDayPlanning(o, plannedDays)) newVDP = 1;
            else newVDP = 0;
            o.LastInvalidPlan = newVDP;
            return newVDP - last;
        }
        // Function for finding whether the current planning of an Order is not allowed
        public static bool InvalidDayPlanning(Order o, int[] planning) {
            if (planning.Length == 0) return false;
            byte freq = o.Frequency;
            if (freq == 1) return false;
            if (freq == 2) if (planning.Length == 1) return planning[0] == 3; else return Math.Abs(planning[0] - planning[1]) != 3;
            if (freq == 3) {
                bool[] test3 = new bool[3];
                foreach (int b in planning) {
                    int dv = b / 2;
                    if (b % 2 != 1 || test3[dv]) return true;
                    test3[dv] = true;
                }
                return false;
            }
            bool[] test4 = new bool[5];
            foreach (int i in planning) {
                if (test4[i - 1]) return true;
                test4[i - 1] = true;
            }
            return false;
        }
        // Function for determining the path value from op's previous, to op, to op's next
        public static int ShadowPath(OrderPosition op) {
            Order nxt = Program.HomeOrder;
            Order prv = Program.HomeOrder;
            if (op.Previous != null) prv = op.Previous.order;
            if (op.Next != null) nxt = op.Next.order;
            return PathValue(prv.Location, op.order.Location) + PathValue(op.order.Location, nxt.Location);
        }
        // Function for determining whether op can be planned on the day it's currently inactively planned
        public static int ShadowDay(OrderPosition op) {
            int[] day = new int[1];
            day[0] = op.Day + 1;
            if (InvalidDayPlanning(op.order, day)) {
                return (int)Program.wrongDayPentalty;
            }
            return 0;
        }
        // Function to reset OrderPositions before re-generating a Solution
        public static void ResetOps() {
            List<OrderPosition> opList = new List<OrderPosition>();
            foreach (Order o in Program.allOrders) {
                for (int i = 0; i < o.Frequency; i++) {
                    o.ActiveFreq = 0;
                    o.LastFreqPenAmount = 0;
                    o.LastInvalidPlan = 0;
                    OrderPosition op = new OrderPosition(o, 0, 0, null, false);
                    opList.Add(op);
                    o.Positions[i] = op;
                }
            }
            Program.allPositions = opList.ToArray();
        }
    }
}
