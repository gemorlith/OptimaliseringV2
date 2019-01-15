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
        public abstract Neighbour RndNeighbour(Solution solution);
        public abstract bool IsEmpty(Solution solution);
    }
    public class ToggleSpace : NeighbourSpace {
        public override bool IsEmpty(Solution solution) {
            return false;
        }
        public override Neighbour RndNeighbour(Solution solution) {
            int rnd = (int)(Util.Rnd * solution.allPositions.Count());
            OrderPosition op = solution.allPositions[rnd];
            if (op.active) return new DisableNeighbour(solution, op);
            return new ActivateNeighbour(solution, op);
        }
    }
}
