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
                for (int v = 1; v <= 2; v++) {
                    for (int c = 0; c < s.firsts[v - 1, d - 1].Count; c++) {
                        OrderPosition current = s.firsts[v - 1, d - 1][c];
                        while (current != null) {
                            if (!current.active) {
                                current = current.next;
                                continue;
                            }
                            sw.Write(v); // Vehicle
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
                        sw.Write(v); // Vehicle
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
    }
}
