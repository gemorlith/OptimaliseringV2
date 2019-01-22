using System;
using System.Collections.Generic;
using System.IO;

namespace GroteOpdrachtV2 {
    class Util {
        public static double Rnd { get { return Program.random.NextDouble(); } }

        public static void SaveSolution(Solution s, string path = "../../Solutions/BestSolution.txt") {
            StreamWriter sw = new StreamWriter(path) { AutoFlush = true };
            int counter = 0;
            for (int d = 1; d <= 5; d++) {
                for (int t = 1; t <= 2; t++) {
                    int max = s.cycles[t - 1, d - 1].Count;
                    for (int c = 0; c < max; c++) {
                        OrderPosition current = s.cycles[t - 1, d - 1][c].first;
                        while (current != null) {
                            if (!current.active) {
                                current = current.next;
                                continue;
                            }
                            sw.Write(t); // Vehicle
                            sw.Write(';');
                            sw.Write(d); // Day
                            sw.Write(';');
                            sw.Write(counter + 1); // Sequence number
                            sw.Write(';');
                            sw.Write(current.order.ID); // Order ID
                            sw.WriteLine();
                            counter++;
                            current = current.next;
                        }
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
                    counter = 0;
                }
            }
            sw.Close();
        }

        public static NeighbourSpace NeighbourTypeFromRNG() {
            float counter = 0;
            double random = Rnd;
            foreach (ValuePerNeighbour vpn in Program.neighbourOptions) {
                counter += vpn.value;
                if (counter > random)
                    return vpn.type;
            }
            throw new Exception("Shit's broken: total chance of neighbourType < 1");
        }

        public static List<int> DaysFromRandom(int freq) {
            List<int> days = new List<int>();
            if (freq == 1) {
                double random = Rnd;
                days.Add((int)(random * 5));
            }
            if (freq == 2) {
                double random = Rnd;
                int fst = (int)(random * 2);
                days.Add(fst);
                days.Add(fst + 3);
            }
            if (freq == 3) {
                days.Add(0);
                days.Add(2);
                days.Add(4);
            }
            if (freq == 4) {
                double random = Rnd;
                int not = (int)(random * 5);
                for (int i = 0; i < 5; i++) {
                    if (i != not) {
                        days.Add(i);
                    }
                }
            }
            return days;
        }

        public static List<int> DaysFromPreference(int freq, int preference) {
            List<int> days = new List<int>();
            if (freq == 1) {
                days.Add(preference);
            }
            else if (freq == 2) {
                if (preference == 2) {
                    double random = Rnd;
                    int fst = (int)(random * 2);
                    days.Add(fst);
                    days.Add(fst + 3);
                }
                else {
                    days.Add(preference);
                    days.Add((preference + 3) % 6);
                }
            }
            else if (freq == 3) {
                days.Add(0);
                days.Add(2);
                days.Add(4);
            }
            else if (freq == 4) {
                double random = Rnd;
                int not = (int)(random * 4);
                if (not >= preference) not++;
                for (int i = 0; i < 5; i++) {
                    if (i != not) {
                        days.Add(i);
                    }
                }
            }
            return days;
        }

        public static int PathValue(short from, short to) {
            if (from == to) return 0;
            return Program.paths[from].Paths[to];
        }

        public static bool Test(Solution s, string message = "") {
            Console.WriteLine(message);
            bool different = false;
            double tv = 0, dv = 0, tp = 0, wp = 0;
            double[,] locTim = new double[2, 5];
            Dictionary<Cycle, int> cycWe = new Dictionary<Cycle, int>();
            foreach (OrderPosition op in s.allPositions) {
                if (op.active) {
                    locTim[op.truck, op.day] += PathValue(op.order.Location, s.NextActive(op).Location);
                    locTim[op.truck, op.day] += op.order.Time;
                    if (!cycWe.ContainsKey(op.cycle)) cycWe.Add(op.cycle, 0);
                    cycWe[op.cycle] += op.order.ContainerVolume;
                }
                else {
                    dv += op.order.Time * 3 * op.order.Frequency;
                }
            }
            foreach (Cycle c in s.allCycles) {
                Order act;
                if (c.first.active) act = c.first.order; 
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
            if (!TheSameISwear(tp, s.tp)) { Console.WriteLine("TimePenalty should be " + tp + " but it is " + s.tp + "; difference: " + -(tp - s.tp)); different = true; }
            if (!TheSameISwear(wp, s.wp)) { Console.WriteLine("WeightPenalty should be " + wp + " but it is " + s.wp + "; difference: " + -(wp - s.wp)); different = true; }
            //if (different) throw new Exception("One or more values were inequal; check console.");
            return different;
        }

        public static bool TheSameISwear(double one, double theOther) {
            return one < theOther + .5 && one > theOther - .5;
        }
    }
}
