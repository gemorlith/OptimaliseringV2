using System;
using System.Collections.Generic;
using System.IO;

namespace GroteOpdrachtV2 {
    class Util {
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

        public static NeighbourSpace NeighbourTypeFromRNG(Random r, List<ValuePerNeighbour> neighborOptions) {
            double counter = 0;
            double random = r.NextDouble();
            foreach (ValuePerNeighbour vpn in neighborOptions) {
                counter += vpn.value;
                if (counter > random)
                    return vpn.type;
            }
            throw new Exception("Shit's broken: total chance of neighbourType < 1");
        }

        public static List<int> DaysFromRandom(Random r, int freq) {
            List<int> days = new List<int>();
            if (freq == 1) {
                double random = r.NextDouble();
                days.Add((int)(random * 5));
            }
            if (freq == 2) {
                double random = r.NextDouble();
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
                double random = r.NextDouble();
                int not = (int)(random * 5);
                for (int i = 0; i < 5; i++) {
                    if (i != not) {
                        days.Add(i);
                    }
                }
            }
            return days;
        }

        public static List<int> DaysFromPreference(Random r, int freq, int preference) {
            List<int> days = new List<int>();
            if (freq == 1) {
                days.Add(preference);
            }
            else if (freq == 2) {
                if (preference == 2) {
                    double random = r.NextDouble();
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
                double random = r.NextDouble();
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

        public static int PathValue(int[,] paths, short from, short to) {
            if (from == to) return 0;
            return paths[from, to];
        }

        public static bool Test(Solution s, int[,] paths, OrderPosition[] allPositions, string message = "", bool print = true) {
            if (print) Console.WriteLine(message);
            bool different = false;
            double tv = 0, dv = 0, tp = 0, wp = 0;
            double[,] locTim = new double[2, 5];
            Dictionary<Cycle, int> cycWe = new Dictionary<Cycle, int>();
            foreach (OrderPosition op in allPositions) {
                if (op.Active) {
                    locTim[op.truck, op.Day] += PathValue(paths, op.order.Location, s.NextActive(op).Location);
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
                locTim[c.truck, c.day] += PathValue(paths, Program.Home, act.Location);
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

        public static bool TheSameISwear(double one, double theOther) {
            return one < theOther + .5 && one > theOther - .5;
        }

        public static void CheckPrevAndNextForLoops(OrderPosition o) {
            if (o == null) return;
            if (o.Previous == o) throw new Exception("AAAAAAAAA");
            if (o.Next == o) throw new Exception("AAaAAAaAA");
            if (o.Previous != null) if (o.Previous.Next != o) throw new Exception("AAAAAAAAAA?");
            if (o.Next != null) if (o.Next.Previous != o) throw new Exception("AAaaAAAaAA?");
        }

        public static int IncreaseFreqPenAmount(Order o) {
            int last = o.LastFreqPenAmount;
            int newFPA = FreqPenAmount(o.ActiveFreq, o.Frequency);
            o.LastFreqPenAmount = newFPA;
            return newFPA - last;
        }

        public static int FreqPenAmount(int currentFreq, int desiredFreq) {
            if (desiredFreq - currentFreq < 0) {
                int yeet = 0;
            }
            return Math.Min(currentFreq, desiredFreq - currentFreq);
        }

        public static int IncreaseInvalidDayPlanning(Order o) {
            int last = o.LastValidPlan;
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
            o.LastValidPlan = newVDP;
            return newVDP - last;
        }

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

        public static int ShadowPath (int[,] paths, OrderPosition op) {
            Order nxt = Program.HomeOrder;
            Order prv = Program.HomeOrder;
            if (op.Previous != null) prv = op.Previous.order;
            if (op.Next != null) nxt = op.Next.order;
            return PathValue(paths, prv.Location,op.order.Location) + PathValue(paths, op.order.Location, nxt.Location);
        }

        public static int ShadowDay (OrderPosition op) {
            int[] day = new int[1];
            day[0] = op.Day + 1;
            if (InvalidDayPlanning(op.order, day)) {
                return (int) Program.wrongDayPentalty;
            }
            return 0;
        }
    }
}
