using System.Collections.Generic;

namespace GroteOpdrachtV2 {
    public abstract class StartSolutionGenerator {
        public abstract Solution Generate();
    }

    /*public class DefaultGeneratorMK1 : StartSolutionGenerator {
        public override Solution Generate() {
            short pos = Program.Home, oldPos = Program.Home;
            int load = 0, vehicle = 0, cycle = 0, day = 1;
            double timeValue = 0, declineValue = 0, time = 0;

            List<int>[,] cycleWeighs = new List<int>[2, 5];
            double[,] localTimes = new double[2, 5];
            Order plan = null;
            List<List<int>>[,] orderList = new List<List<int>>[2, 5];
            Dictionary<int, byte> doneList = new Dictionary<int, byte>();
            HashSet<int>[] planning = new HashSet<int>[5];
            List<int> declined = new List<int>();

            for (int i = 0; i < 5; i++) {
                planning[i] = new HashSet<int>();
            }
            for (int i = 0; i < Program.allOrders.Count; i++) {
                doneList.Add(Program.allOrders[i].ID, 0);
                declined.Add(Program.allOrders[i].ID);
            }
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 5; j++) {
                    orderList[i, j] = new List<List<int>>();
                    cycleWeighs[i, j] = new List<int>();
                }
            }
            while (day < 6) {
                bool stillOrders = MakePlan();
                while (stillOrders && load <= Program.MaxCarry && time + Program.DisposalTime + Program.PathValue(pos, Program.Home) < Program.MaxTime) {
                    ExecutePlan(plan);
                    stillOrders = MakePlan();
                }
                if (time + Program.DisposalTime + Program.PathValue(pos, Program.Home) > Program.MaxTime || !stillOrders) {
                    if (load != 0) {
                        GoHome(stillOrders);
                        cycle++;
                    }
                    if (vehicle == 0) {
                        vehicle++;
                        time = 0;
                        cycle = 0;
                    }
                    else {
                        day++;
                        if (day == 6) {
                            break;
                        }
                        cycle = 0;
                        vehicle = 0;
                        time = 0;
                    }
                }
                else if (load != 0) {
                    GoHome(stillOrders);
                    cycle++;
                }
            }
            foreach (Order o in Program.allOrders) {
                if (doneList[o.ID] != 0 && doneList[o.ID] != o.Frequency) {
                    if (!declined.Contains(o.ID)) declined.Add(o.ID);
                    foreach (HashSet<int> hs in planning) hs.Remove(o.ID);
                    foreach (List<List<int>> l in orderList) {
                        foreach (List<int> sl in l) sl.Remove(o.ID);
                    }
                }
            }
            foreach (int id in declined) {
                Order o = Program.orderByID[id];
                declineValue += o.Time * o.Frequency * 3;
            }
            for (int t = 0; t < 2; t++) {
                for (int d = 0; d < 5; d++) {
                    double td_totalTime = 0;
                    for (int c = 0; c < orderList[t, d].Count; c++) {
                        List<int> orders = orderList[t, d][c];
                        if (orders.Count >= 1) {
                            short from = Program.Home, to;
                            for (int i = 0; i < orders.Count; i++) {
                                to = Program.orderByID[orders[i]].Location;
                                td_totalTime += Program.orderByID[orders[i]].Time;
                                td_totalTime += Program.PathValue(from, to);
                                from = Program.orderByID[orders[i]].Location;
                            }
                            td_totalTime += Program.PathValue(from, Program.Home);
                            td_totalTime += Program.DisposalTime;
                        }
                    }
                    localTimes[t, d] += td_totalTime;
                    timeValue += td_totalTime;
                }
            }
            return new Solution(orderList, planning, declined, timeValue, declineValue, localTimes, cycleWeighs, 0);

            bool CanIGetTrashToday(Order order) {
                int frequency = order.Frequency;
                if (doneList[order.ID] >= frequency) return false;
                if (frequency >= 4 || frequency == 1) return true;
                if (frequency == 3 && day % 2 == 1) return true;
                if (frequency == 2 && day < 3 && doneList[order.ID] == 0) {
                    return true;
                }
                if (frequency == 2 && day > 3) {
                    return planning[day - 4].Contains(order.ID);
                }
                return false;

            }
            bool MakePlan() {
                foreach (Order o in Program.paths[pos].Orders) {
                    if (!planning[day - 1].Contains(o.ID) && CanIGetTrashToday(o)) {
                        PlanSpecific(o);
                        return true;
                    }
                }
                for (int i = 0; i < Program.paths[pos].SortedDistances.Count; i++) {
                    List<Order> os = Program.paths[Program.paths[pos].SortedDistances[i]].Orders;
                    foreach (Order o in os) {
                        if (!planning[day - 1].Contains(o.ID) && CanIGetTrashToday(o)) {
                            PlanSpecific(o);
                            return true;
                        }
                    }
                }
                return false;
            }
            void GoHome(bool stillOrders) {
                load = 0;
                if (stillOrders) {
                    time -= plan.Time;
                    time -= Program.PathValue(oldPos, pos);
                }
                time += Program.PathValue(oldPos, Program.Home);
                time += Program.DisposalTime;
                pos = Program.Home;
                oldPos = Program.Home;
            }
            void PlanSpecific(Order o) {
                plan = o;
                load += plan.ContainerVolume;
                time += plan.Time;
                time += Program.PathValue(pos, o.Location);
                oldPos = pos;
                pos = o.Location;
            }
            void ExecutePlan(Order order) {
                doneList[order.ID]++;
                planning[day - 1].Add(order.ID);
                if (cycle >= orderList[vehicle, day - 1].Count) {
                    orderList[vehicle, day - 1].Add(new List<int>());
                    cycleWeighs[vehicle, day - 1].Add(0);
                }
                orderList[vehicle, day - 1][cycle].Add(order.ID);
                cycleWeighs[vehicle, day - 1][cycle] += order.ContainerVolume;
                declined.Remove(order.ID);
            }
        }
    }

    public class DefaultGeneratorMK2 : StartSolutionGenerator {
        public override Solution Generate() {
            short pos = Program.Home, oldPos = Program.Home;
            int load = 0, vehicle = 0, cycle = 0, day = 1;
            double timeValue = 0, declineValue = 0, time = 0;

            List<int>[,] cycleWeighs = new List<int>[2, 5];
            double[,] localTimes = new double[2, 5];
            Order plan = null;
            HashSet<int>[] doneSoFar = new HashSet<int>[5];
            List<List<int>>[,] orderList = new List<List<int>>[2, 5];
            Dictionary<int, byte> doneList = new Dictionary<int, byte>();
            HashSet<int>[] planning = new HashSet<int>[5];
            List<int> declined = new List<int>();

            for (int i = 0; i < 5; i++) {
                planning[i] = new HashSet<int>();
                doneSoFar[i] = new HashSet<int>();
            }
            for (int i = 0; i < Program.allOrders.Count; i++) {
                doneList.Add(Program.allOrders[i].ID, 0);
                declined.Add(Program.allOrders[i].ID);
            }
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < 5; j++) {
                    orderList[i, j] = new List<List<int>>();
                    cycleWeighs[i, j] = new List<int>();
                }
            }
            foreach (Order o in Program.allOrders) {
                assignDates(o);
            }
            while (day < 6) {
                bool stillOrders = MakePlan();
                while (stillOrders && load <= Program.MaxCarry && time + Program.DisposalTime + Program.PathValue(pos, Program.Home) < Program.MaxTime) {
                    ExecutePlan(plan);
                    stillOrders = MakePlan();
                }
                if (time + Program.DisposalTime + Program.PathValue(pos, Program.Home) > Program.MaxTime || !stillOrders) {
                    if (load != 0) {
                        GoHome(stillOrders);
                        cycle++;
                    }
                    if (vehicle == 0) {
                        vehicle++;
                        time = 0;
                        cycle = 0;
                    }
                    else {
                        day++;
                        if (day == 6) {
                            break;
                        }
                        cycle = 0;
                        vehicle = 0;
                        time = 0;
                    }
                }
                else if (load != 0) {
                    GoHome(stillOrders);
                    cycle++;
                }
            }
            foreach (Order o in Program.allOrders) {
                if (doneList[o.ID] != 0 && doneList[o.ID] != o.Frequency) {
                    if (!declined.Contains(o.ID)) declined.Add(o.ID);
                    foreach (HashSet<int> hs in planning) hs.Remove(o.ID);
                    foreach (HashSet<int> hs in doneSoFar) hs.Remove(o.ID);
                    foreach (List<List<int>> l in orderList) {
                        foreach (List<int> sl in l) sl.Remove(o.ID);
                    }
                }
            }
            foreach (int id in declined) {
                Order o = Program.orderByID[id];
                declineValue += o.Time * o.Frequency * 3;
            }
            for (int t = 0; t < 2; t++) {
                for (int d = 0; d < 5; d++) {
                    double td_totalTime = 0;
                    for (int c = 0; c < orderList[t, d].Count; c++) {
                        List<int> orders = orderList[t, d][c];
                        if (orders.Count >= 1) {
                            short from = Program.Home, to;
                            for (int i = 0; i < orders.Count; i++) {
                                to = Program.orderByID[orders[i]].Location;
                                td_totalTime += Program.orderByID[orders[i]].Time;
                                td_totalTime += Program.PathValue(from, to);
                                from = Program.orderByID[orders[i]].Location;
                            }
                            td_totalTime += Program.PathValue(from, Program.Home);
                            td_totalTime += Program.DisposalTime;
                        }
                    }
                    localTimes[t, d] += td_totalTime;
                    timeValue += td_totalTime;
                }
            }
            return new Solution(orderList, doneSoFar, declined, timeValue, declineValue, localTimes, cycleWeighs, 0);

            void assignDates(Order o) {
                List<int> days = Program.DaysFromRandom(o.Frequency);
                foreach (int i in days) {
                    planning[i].Add(o.ID);
                }
            }

            bool CanIGetTrashToday(Order order) {
                return planning[day - 1].Contains(order.ID);
            }
            bool MakePlan() {
                foreach (Order o in Program.paths[pos].Orders) {
                    if (!doneSoFar[day - 1].Contains(o.ID) && CanIGetTrashToday(o)) {
                        PlanSpecific(o);
                        return true;
                    }
                }
                for (int i = 0; i < Program.paths[pos].SortedDistances.Count; i++) {
                    List<Order> os = Program.paths[Program.paths[pos].SortedDistances[i]].Orders;
                    foreach (Order o in os) {
                        if (!doneSoFar[day - 1].Contains(o.ID) && CanIGetTrashToday(o)) {
                            PlanSpecific(o);
                            return true;
                        }
                    }
                }
                return false;
            }
            void GoHome(bool stillOrders) {
                load = 0;
                if (stillOrders) {
                    time -= plan.Time;
                    time -= Program.PathValue(oldPos, pos);
                }
                time += Program.PathValue(oldPos, Program.Home);
                time += Program.DisposalTime;
                pos = Program.Home;
                oldPos = Program.Home;
            }
            void PlanSpecific(Order o) {
                plan = o;
                load += plan.ContainerVolume;
                time += plan.Time;
                time += Program.PathValue(pos, o.Location);
                oldPos = pos;
                pos = o.Location;
            }
            void ExecutePlan(Order order) {
                doneList[order.ID]++;
                doneSoFar[day - 1].Add(order.ID);
                if (cycle >= orderList[vehicle, day - 1].Count) {
                    orderList[vehicle, day - 1].Add(new List<int>());
                    cycleWeighs[vehicle, day - 1].Add(0);
                }
                orderList[vehicle, day - 1][cycle].Add(order.ID);
                cycleWeighs[vehicle, day - 1][cycle] += order.ContainerVolume;
                declined.Remove(order.ID);
            }
        }
    }*/

    public class EmptyGenerator : StartSolutionGenerator {
        public override Solution Generate() {
            double declineVal = 0;
            double[,] localtimes = new double[2, 5];
            List<Cycle>[,] cycles = new List<Cycle>[2, 5];
            for (int d = 0; d < 5; d++) {
                for (int t = 0; t < 2; t++) {
                    cycles[t, d] = new List<Cycle>();
                }
            }
            Cycle c = new Cycle(0, 0, 0, null);
            OrderPosition prev = null;
            foreach (Order o in Program.allOrders) {
                declineVal += o.Time * 3 * o.Frequency;
            }
            foreach (OrderPosition op in Program.allPositions) {
                op.cycle = c;
                op.previous = prev;
                if (prev != null) {
                    prev.next = op;
                }
                prev = op;
            }
            prev.next = null;
            c.first = Program.allPositions[0];
            cycles[0, 0].Add(c);
            return new Solution(0, declineVal, 0, localtimes, cycles);
        }
    }

    /*public class ReadGenerator : StartSolutionGenerator {
        string path;
        public ReadGenerator(string path) {
            this.path = path;
        }
        public override Solution Generate() {
            System.IO.StreamReader sr = new System.IO.StreamReader(path);
            string input;
            string[] inputs;

            List<List<int>>[,] ol = new List<List<int>>[2, 5];
            HashSet<int>[] pl = new HashSet<int>[5];
            List<int> dc = new List<int>();
            double declineVal = 0;
            double timeVal = 0;
            double[,] localtimes = new double[2, 5];
            List<int>[,] cw = new List<int>[2, 5];

            for (int i = 0; i < 5; i++) {
                pl[i] = new HashSet<int>();
                for (int t = 0; t < 2; t++) {
                    cw[t, i] = new List<int>();
                    ol[t, i] = new List<List<int>>();
                    ol[t, i].Add(new List<int>());
                    cw[t, i].Add(0);
                }
            }

            foreach (Order o in Program.allOrders) {
                dc.Add(o.ID);
                declineVal += o.Time * 3 * o.Frequency;
            }
            short from = Program.Home;
            while ((input = sr.ReadLine()) != null) {
                inputs = input.Split(';');
                int truck = int.Parse(inputs[0]) - 1;
                int day = int.Parse(inputs[1]) - 1;
                int nr = int.Parse(inputs[2]);
                int val = int.Parse(inputs[3]);
                Order o = Program.orderByID[val];

                pl[day].Add(val);
                dc.Remove(val);
                int cycle = ol[truck, day].Count - 1;
                if (val != 0) {
                    ol[truck, day][cycle].Add(val);
                    declineVal -= o.Time * 3;
                }
                else {
                    ol[truck, day].Add(new List<int>());
                    cw[truck, day].Add(0);
                }
                cw[truck, day][cycle] += o.ContainerVolume;
                localtimes[truck, day] += o.Time;
                localtimes[truck, day] += Program.PathValue(from, o.Location);
                timeVal += o.Time;
                timeVal += Program.PathValue(from, o.Location);
                from = o.Location;
            }
            foreach (List<List<int>> lli in ol) {
                if (lli != null) {
                    lli.RemoveAt(lli.Count - 1);
                }
            }
            sr.Close();
            return new Solution(ol, pl, dc, timeVal, declineVal, localtimes, cw, 0);
        }
    }*/
}
