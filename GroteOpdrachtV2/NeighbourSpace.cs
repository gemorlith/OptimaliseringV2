namespace GroteOpdrachtV2 {
    public class ValuePerNeighbour {
        // Simple class for storing the chances of choosing a specific NeighbourSpace
        public float value;
        public NeighbourSpace type;
        public ValuePerNeighbour(float value, NeighbourSpace type) {
            this.value = value;
            this.type = type;
        }
    }

    public abstract class NeighbourSpace {
        // Template for all NeighbourSpaces
        public abstract bool IsEmpty(Solution solution);
        public abstract Neighbour RndNeighbour(Solution solution);
    }
    public class ToggleSpace : NeighbourSpace {
        // NeighbourSpace that activates a randomly chosen OrderPosition if it was inactive, and vice versa
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[rnd];
            if (op.Active) return new Deactivate(solution, op);
            return new ActivateNeighbour(solution, op);
        }
    }

    public class ActivateSpace : NeighbourSpace {
        // NeighbourSpace that activates a randomly chosen inactive OrderPosition
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            for (int i = 0; i < 100; i++) {
                int rnd = (int)(Util.Rnd * Program.allPositions.Length);
                OrderPosition op = Program.allPositions[rnd];
                if (!op.Active) return new ActivateNeighbour(solution, op);
            }
            // If after 100 tries, no inactive OrderPosition was found, the NeighbourSpace does nothing
            return null;
        }
    }

    public class MoveSpace : NeighbourSpace {
        // NeighbourSpace that moves a randomly chosen OrderPosition to after another OrderPosition or at the start of an existing or new Cycle
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd1 = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[rnd1];
            int amount = Program.allPositions.Length - 1 + solution.allCycles.Count + 10; // OrderPositions to place 'op' after + amount of existing Cycles + possible new Cycles
            int rnd2 = (int)(Util.Rnd * amount);
            if (rnd2 >= rnd1) rnd2++; // To make sure it doesn't choose to place 'op' after itself

            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd2 < Program.allPositions.Length) { // Move to after an existing order
                prev = Program.allPositions[rnd2];
                cycle = prev.cycle;
                truck = prev.truck; day = prev.Day;
            }
            else if (rnd2 < Program.allPositions.Length + solution.allCycles.Count) { // Move to beginning of existing cycle
                prev = null;
                cycle = solution.allCycles[rnd2 - Program.allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else { // Create new cycle
                prev = null;
                cycle = null;
                int truckday = rnd2 - Program.allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveNeighbour(solution, op, prev, day, truck, cycle);
        }
    }

    public class SwapSpace : NeighbourSpace {
        // NeighbourSpace that swaps two randomly chosen OrderPositions
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd1 = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op1 = Program.allPositions[rnd1];
            int rnd2 = (int)(Util.Rnd * Program.allPositions.Length - 1);
            if (rnd2 >= rnd1) rnd2++; // To make sure it doesn't choose to swap 'op1' with itself
            OrderPosition op2 = Program.allPositions[rnd2];
            return new SwapNeighbour(solution, op1, op2);
        }
    }

    public class Opt2Space : NeighbourSpace {
        // NeighbourSpace that mirrors a randomly chosen sequence of consecutive OrderPositions
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition startOp = Program.allPositions[rnd];
            // Find the amount of OrderPositions coming after 'startOp'
            OrderPosition op = startOp;
            int count = 0;
            while (op.Next != null) {
                op = op.Next;
                count++;
            }
            int length = (int)(Util.Rnd * count) + 1;
            return new Opt2Neighbour(solution, startOp, length);
        }
    }

    public class MoveAndActivateSpace : NeighbourSpace {
        // NeighbourSpace that performs the same actions as MoveSpace, then activates the chosen OrderPosition if it was not active already
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd1 = (int)(Util.Rnd * Program.allPositions.Length);
            OrderPosition op = Program.allPositions[rnd1];
            int amount = Program.allPositions.Length - 1 + solution.allCycles.Count + 10; // OrderPositions to place 'op' after + amount of existing Cycles + possible new Cycles
            int rnd2 = (int)(Util.Rnd * amount);
            if (rnd2 >= rnd1) rnd2++; // To make sure it doesn't choose to place 'op' after itself
            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd2 < Program.allPositions.Length) { // Move to after an existing order
                prev = Program.allPositions[rnd2];
                cycle = prev.cycle;
                truck = prev.truck; day = prev.Day;
            }
            else if (rnd2 < Program.allPositions.Length + solution.allCycles.Count) { // Move to beginning of existing cycle
                prev = null;
                cycle = solution.allCycles[rnd2 - Program.allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else { // Create new cycle
                prev = null;
                cycle = null;
                int truckday = rnd2 - Program.allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveAndSetNeighbour(solution, op, prev, day, truck, cycle, true);
        }
    }

    public class ToggleOrderSpace : NeighbourSpace {
        // NeighbourSpace that activates or deactivates all OrderPositions corresponding to a randomly chosen Order, depending on a randomly chosen boolean
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
