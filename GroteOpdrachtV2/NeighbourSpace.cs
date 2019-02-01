namespace GroteOpdrachtV2 {
    public struct ValuePerNeighbour {
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
}
