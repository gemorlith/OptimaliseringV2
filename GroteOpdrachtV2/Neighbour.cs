using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GroteOpdrachtV2 {
    public abstract class Neighbour {
        protected Solution s;
        public abstract void Apply();
        public abstract Neighbour Reverse();
    }
    public class ActivateNeighbour : Neighbour{
        OrderPosition op;
        public ActivateNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply() {
            s.setActive(true, op);
        }
        public override Neighbour Reverse() {
            return new DisableNeighbour(s, op);
        }
    }
    public class DisableNeighbour : Neighbour {
        OrderPosition op;
        public DisableNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply() {
            s.setActive(false, op);
        }
        public override Neighbour Reverse() {
            return new ActivateNeighbour(s, op);
        }
    }
    public class MoveNeighbour : Neighbour {
        OrderPosition op;
        OrderPosition newPrevious;
        OrderPosition oldPrevious;
        byte day, truck, cycle, oriDay, oriTruck, oriCycle;
        public MoveNeighbour(OrderPosition op, OrderPosition previous, byte day, byte truck, byte cycle) {
            this.op = op;
            this.newPrevious = previous;
            this.day = day;
            this.truck = truck;
            this.cycle = cycle;
            oldPrevious = op.previous;
            oriDay = op.day;
            oriTruck = op.truck;
            oriCycle = op.cycle;
        }
        public override void Apply() {
            bool active = false;
            if (op.active) {
                active = true;
                s.setActive(false, op);
            }
            s.RemoveOrder(op);
            s.AddOrder(op, newPrevious, truck, day, cycle);
            if (active) {
                s.setActive(true, op);
            }
        }
        public override Neighbour Reverse() {
            return new MoveNeighbour(op, oldPrevious, oriDay, oriTruck, oriCycle);
        }
    }

    public class SwapNeighbour : Neighbour {
        OrderPosition op1;
        OrderPosition op2;
        public SwapNeighbour(OrderPosition op1, OrderPosition op2) {
            this.op1 = op1;
            this.op2 = op2;
        }
        public override void Apply() {
            OrderPosition prev1 = op1.previous;
            byte day1 = op1.day, truck1 = op1.truck, cycle1 = op1.cycle;
            MoveNeighbour move1 = new MoveNeighbour(op1, op2.previous, op2.day, op2.truck, op2.cycle);
            move1.Apply();
            MoveNeighbour move2 = new MoveNeighbour(op2, prev1, day1, truck1, cycle1);
            move2.Apply();
        }
        public override Neighbour Reverse() {
            return new SwapNeighbour(op2, op1);
        }
    }
}
