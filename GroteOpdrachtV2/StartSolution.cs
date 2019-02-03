using System.Collections.Generic;
using System.IO;

namespace GroteOpdrachtV2 {
    public abstract class StartSolutionGenerator {
        // Template for all StartSolutionGenerators
        public abstract Solution Generate();
    }

    public class EmptyGenerator : StartSolutionGenerator {
        // Generates a Solution in which none of the orders are carried out
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
                op.Previous = prev;
                if (prev != null) {
                    prev.Next = op;
                }
                prev = op;
            }
            prev.Next = null;
            c.first = Program.allPositions[0];
            cycles[0, 0].Add(c);
            return new Solution(0, declineVal, 0, localtimes, cycles);
        }
    }

    public class ReadGenerator : StartSolutionGenerator {
        // Generates a Solution class from a saved solution .txt file
        string path;
        public ReadGenerator(string path) {
            this.path = path;
        }
        public override Solution Generate() {
            StreamReader sr = new StreamReader(path);
            string input;
            string[] inputs;

            List<OrderPosition> plaatsbaar = new List<OrderPosition>();
            double timeVal = 0;
            double declineVal = 0;
            double penaltyVal = 0;
            double[,] localtimes = new double[2, 5];
            List<Cycle>[,] cycles = new List<Cycle>[2, 5];
            for (int d = 0; d < 5; d++) {
                for (int t = 0; t < 2; t++) {
                    cycles[t, d] = new List<Cycle>();
                }
            }
            List<Cycle> allCycles = new List<Cycle>();
            OrderPosition previous = null;
            input = sr.ReadLine();
            inputs = input.Split(';');
            byte truck = (byte)(int.Parse(inputs[0]) - 1);
            byte day = (byte)(int.Parse(inputs[1]) - 1);
            int counter = int.Parse(inputs[2]);
            int nr = int.Parse(inputs[3]);
            Order order = Program.orderByID[nr];
            order.Positions[0].Previous = null;
            localtimes[truck, day] += Util.PathValue(Program.HomeOrder.Location, order.Location) + order.Time;
            timeVal += Util.PathValue(Program.HomeOrder.Location, order.Location) + order.Time;

            Cycle c = new Cycle(day, truck, 0, order.Positions[0]);
            order.Positions[0].cycle = c;
            order.Positions[0].truck = truck;
            order.Positions[0].Day = day;
            c.cycleWeight += order.ContainerVolume;
            plaatsbaar.Add(order.Positions[0]);
            order.Positions[0].Active = true;
            previous = order.Positions[0];

            while ((input = sr.ReadLine()) != null) {
                inputs = input.Split(';');
                truck = (byte)(int.Parse(inputs[0]) - 1);
                day = (byte)(int.Parse(inputs[1]) - 1);
                counter = int.Parse(inputs[2]);
                nr = int.Parse(inputs[3]);
                order = Program.orderByID[nr];

                if (nr == 0) {
                    previous.Next = null;
                    cycles[truck, day].Add(c);
                    allCycles.Add(c);
                    localtimes[truck, day] += Util.PathValue(previous.order.Location, order.Location) + Program.DisposalTime;
                    timeVal += Util.PathValue(previous.order.Location, order.Location) + Program.DisposalTime;
                    previous = null;
                }
                else {
                    OrderPosition[] positions = order.Positions;
                    for (int i = 0; i < positions.Length; i++) {
                        if (positions[i].cycle == null) {
                            if (previous == null) {
                                c = new Cycle(day, truck, 0, positions[i]);
                                localtimes[truck, day] += Util.PathValue(Program.HomeOrder.Location, order.Location) + order.Time;
                                timeVal += Util.PathValue(Program.HomeOrder.Location, order.Location) + order.Time;
                            }
                            else {
                                previous.Next = positions[i];
                                localtimes[truck, day] += Util.PathValue(previous.order.Location, order.Location) + order.Time;
                                timeVal += Util.PathValue(previous.order.Location, order.Location) + order.Time;

                            }
                            positions[i].Active = true;
                            positions[i].Previous = previous;
                            positions[i].cycle = c;
                            positions[i].Day = day;
                            positions[i].truck = truck;
                            c.cycleWeight += order.ContainerVolume;
                            previous = positions[i];
                            plaatsbaar.Add(positions[i]);
                            break;
                        }
                    }
                }
            }
            int declinedAmount = 0;
            foreach (OrderPosition op in Program.allPositions) {
                if (!op.Active) {
                    declineVal += op.order.Time * 3;
                    declinedAmount++;
                    int index = (int)(Util.Rnd * (plaatsbaar.Count + allCycles.Count));
                    if (index < plaatsbaar.Count) {
                        op.Previous = plaatsbaar[index];
                        op.Next = plaatsbaar[index].Next;
                        op.Previous.Next = op;
                        op.cycle = plaatsbaar[index].cycle;
                        op.Day = plaatsbaar[index].Day;
                        op.truck = plaatsbaar[index].truck;
                        if (op.Next != null) {
                            op.Next.Previous = op;
                        }
                    }
                    else {
                        op.Next = allCycles[index - plaatsbaar.Count].first;
                        op.cycle = allCycles[index - plaatsbaar.Count].first.cycle;
                        op.Day = allCycles[index - plaatsbaar.Count].first.Day;
                        op.truck = allCycles[index - plaatsbaar.Count].first.truck;
                        allCycles[index - plaatsbaar.Count].first = op;
                        op.Next.Previous = op;

                    }
                }
            }

            for (int i = 0; i < allCycles.Count; i++) {
                if (allCycles[i].cycleWeight > Program.MaxCarry) {
                    penaltyVal += (allCycles[i].cycleWeight - Program.MaxCarry) * Program.overWeightPenalty;
                }
            }
            for (int d = 0; d < 5; d++) {
                for (int t = 0; t < 2; t++) {
                    if (localtimes[t, d] > Program.MaxTime) {
                        penaltyVal += (localtimes[t, d] - Program.MaxTime) * Program.overTimePenalty;
                    }
                }
            }

            sr.Close();
            return new Solution(timeVal, declineVal, penaltyVal, localtimes, cycles);
        }
    }
}
