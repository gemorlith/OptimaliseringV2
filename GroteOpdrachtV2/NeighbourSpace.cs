using System.Collections.Generic;

namespace GroteOpdrachtV2 {
    public class ValuePerNeighbour {
        public float value;
        public NeighbourSpace type;
        public ValuePerNeighbour(float value, NeighbourSpace type) {
            this.value = value;
            this.type = type;
        }
    }
    public abstract class NeighbourSpace {
        public abstract bool IsEmpty(Solution solution);
        public abstract Neighbour RndNeighbour(Solution solution);
    }
    public class ToggleSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[rnd];
            if (op.Active) return new DisableNeighbour(solution, op);
            return new ActivateNeighbour(solution, op);
        }
    }

    public class ActivateSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int count = 0;
            while (count < 100) {
                count++;
                int rnd = (int)(Util.Rnd * Program.allPositions.Length);
                OrderPosition op = Program.allPositions[rnd];
                if (!op.Active) return new ActivateNeighbour(solution, op);
            }
            return null;
        }
    }

    public class MoveSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int ind = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[ind];
            int amount = Program.allPositions.Length - 1 + solution.allCycles.Count + 10;
            int rnd = (int)(Util.Rnd * amount);
            if (rnd >= ind) rnd++;
            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd < Program.allPositions.Length) { // Move to after an existing order
                prev = Program.allPositions[rnd];
                cycle = prev.cycle;
                truck = prev.truck; day = prev.Day;
            }
            else if (rnd < Program.allPositions.Length + solution.allCycles.Count) { // Move to beginning of existing cycle
                prev = null;
                cycle = solution.allCycles[rnd - Program.allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else { // Create new cycle
                prev = null;
                cycle = null;
                int truckday = rnd - Program.allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveNeighbour(solution, op, prev, day, truck, cycle);
        }
    }

    public class SwapSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd1 = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op1 = Program.allPositions[rnd1];
            int rnd2 = (int)(Util.Rnd * Program.allPositions.Length - 1);
            if (rnd2 >= rnd1) rnd2++;
            OrderPosition op2 = Program.allPositions[rnd2];


            return new SwapNeighbour(solution, op1, op2);
        }
    }

    public class Opt2Space : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition startOp = Program.allPositions[rnd];
            OrderPosition op = startOp;
            int count = 0;
            while (op.Next != null) {
                count++;
                op = op.Next;
            }
            int length = (int)(Util.Rnd * count) + 1;


            return new Opt2Neighbour(solution, startOp, length);
        }
    }

    public class MoveAndActivateSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int ind = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[ind];
            int amount = Program.allPositions.Length - 1 + solution.allCycles.Count + 10;
            int rnd = (int)(Util.Rnd * amount);
            if (rnd >= ind) rnd++;
            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd < Program.allPositions.Length) { // Move to after an existing order
                prev = Program.allPositions[rnd];
                cycle = prev.cycle;
                truck = prev.truck; day = prev.Day;
            }
            else if (rnd < Program.allPositions.Length + solution.allCycles.Count) { // Move to beginning of existing cycle
                prev = null;
                cycle = solution.allCycles[rnd - Program.allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else { // Create new cycle
                prev = null;
                cycle = null;
                int truckday = rnd - Program.allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveAndSetNeighbour(solution, op, prev, day, truck, cycle, true);
        }
    }

    /*public class MoveAllPositionsSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int ind = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[ind];
            OrderPosition[] positions = op.order.Positions;
            List<byte> days = Util.DaysFromRandom(op.order.Frequency);
            Neighbour[] ns = new Neighbour[positions.Length];
            for (int i = 0; i < ns.Length; i++) {
                ns[i] = Util.MoveToDay(solution, positions[i], days[i]);
            }
            return new MultipleDayNeighbour(solution, positions, days);
        }
    }*/

    public class ToggleOrderSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int ind = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[ind];
            Order o = op.order;
            bool status = Util.RndBool;
            return new ToggleOrderNeighbour(solution, o, status);
        }
    }
}
