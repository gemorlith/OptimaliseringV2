using System.Linq;

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
            int rnd = (int)(Util.Rnd * solution.allPositions.Length);
            OrderPosition op = solution.allPositions[rnd];
            if (op.active) return new DisableNeighbour(solution, op);
            return new ActivateNeighbour(solution, op);
        }
    }
    public class MoveSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * solution.allPositions.Length);
            OrderPosition op = solution.allPositions[rnd];
            int amount = solution.allPositions.Length + solution.allCycles.Count + 10;
            rnd = (int)(Util.Rnd * amount);
            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd < solution.allPositions.Length) {
                prev = solution.allPositions[rnd];
                cycle = null;
                truck = 0; day = 0;
            }
            else if (rnd < solution.allPositions.Length + solution.allCycles.Count) {
                prev = null;
                cycle = solution.allCycles[rnd - solution.allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else {
                prev = null;
                cycle = null;
                int truckday = rnd - solution.allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveNeighbour(op, prev, day, truck, cycle);
        }
    }
}
