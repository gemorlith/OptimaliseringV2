using System;

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
        public abstract Neighbour RndNeighbour(Random rng, OrderPosition[] allPositions, Solution solution);
    }
    public class ToggleSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Random rng, OrderPosition[] allPositions, Solution solution) {
            int rnd = (int)(rng.NextDouble() * allPositions.Length);
            OrderPosition op = allPositions[rnd];
            if (op.Active) return new DisableNeighbour(solution, op);
            return new ActivateNeighbour(solution, op);
        }
    }

    public class ActivateSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Random rng, OrderPosition[] allPositions, Solution solution) {
            bool active = false;
            int count = 0;
            while(count < 100) {
                count++;
                int rnd = (int)(rng.NextDouble() * allPositions.Length);
                OrderPosition op = allPositions[rnd];
                if (op.Active) return new DisableNeighbour(solution, op);
            }
            return null;
        }
    }

    public class MoveSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Random rng, OrderPosition[] allPositions, Solution solution) {
            int ind = (int)(rng.NextDouble() * allPositions.Length);
            OrderPosition op = allPositions[ind];
            int amount = allPositions.Length - 1 + solution.allCycles.Count + 10;
            int rnd = (int)(rng.NextDouble() * amount);
            if (rnd >= ind) rnd++;
            OrderPosition prev;
            Cycle cycle;
            byte truck, day;
            if (rnd < allPositions.Length) { // Move to after an existing order
                prev = allPositions[rnd];
                cycle = prev.cycle;
                truck = prev.truck; day = prev.Day;
            }
            else if (rnd < allPositions.Length + solution.allCycles.Count) { // Move to beginning of existing cycle
                prev = null;
                cycle = solution.allCycles[rnd - allPositions.Length];
                truck = cycle.truck; day = cycle.day;
            }
            else { // Create new cycle
                prev = null;
                cycle = null;
                int truckday = rnd - allPositions.Length - solution.allCycles.Count;
                truck = (byte)(truckday % 2); day = (byte)(truckday % 5);
            }
            return new MoveNeighbour(solution, op, prev, day, truck, cycle);
        }
    }
}
