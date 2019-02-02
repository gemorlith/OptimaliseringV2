using System;
using System.Collections.Generic;
namespace GroteOpdrachtV2 {
    public abstract class Neighbour {
        protected Solution s;
        public abstract void Apply();
        public abstract int ShadowGain();
        public abstract Neighbour Reverse();
    }
    public class ActivateNeighbour : Neighbour {
        OrderPosition op;
        public ActivateNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply() {
            s.SetActive(true, op);
        }
        public override Neighbour Reverse() {
            return new DisableNeighbour(s, op);
        }
        public override int ShadowGain() {
            return 0;
        }
    }
    public class DisableNeighbour : Neighbour {
        OrderPosition op;
        public DisableNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply() {
            s.SetActive(false, op);
        }
        public override Neighbour Reverse() {
            return new ActivateNeighbour(s, op);
        }
        public override int ShadowGain() {
            return 0;
        }
    }
    public class MoveNeighbour : Neighbour {
        OrderPosition op;
        OrderPosition newPrevious;
        OrderPosition oldPrevious;
        Cycle cycle, oldCycle;
        byte day, truck, oldDay, oldTruck;
        int initialShadow;
        public MoveNeighbour(Solution s, OrderPosition op, OrderPosition newPrevious, byte day, byte truck, Cycle cycle) {
            this.s = s;
            this.op = op;
            this.newPrevious = newPrevious;
            this.day = day;
            this.truck = truck;
            this.cycle = cycle;
            oldPrevious = op.Previous;
            oldDay = op.Day;
            oldTruck = op.truck;
            oldCycle = op.cycle;
            initialShadow = op.Shadow;
        }
        public override void Apply() {
            bool active = op.Active;
            if (active) s.SetActive(false, op);
            s.RemoveOrder(op);
            s.AddOrder(op, newPrevious, truck, day, cycle);
            if (active) s.SetActive(true, op);
        }
        public override Neighbour Reverse() {
            return new MoveNeighbour(s, op, oldPrevious, oldDay, oldTruck, oldCycle);
        }
        public override int ShadowGain() {
            return op.Shadow - initialShadow;
        }
    }
    public class SwapNeighbour : Neighbour {
        OrderPosition op1;
        OrderPosition op2;
        int shadow;
        public SwapNeighbour(Solution s, OrderPosition op1, OrderPosition op2) {
            this.s = s;
            this.op1 = op1;
            this.op2 = op2;
        }
        public override void Apply() {
            OrderPosition prev1 = op1.Previous;
            Cycle cycle1 = op1.cycle;
            byte day1 = op1.Day, truck1 = op1.truck;
            MoveNeighbour move1 = new MoveNeighbour(s, op1, op2.Previous, op2.Day, op2.truck, op2.cycle);
            move1.Apply();
            MoveNeighbour move2 = new MoveNeighbour(s, op2, prev1, day1, truck1, cycle1);
            move2.Apply();
            shadow = move1.ShadowGain() + move2.ShadowGain();
        }
        public override Neighbour Reverse() {
            return new SwapNeighbour(s, op2, op1);
        }
        public override int ShadowGain() {
            return shadow;
        }
    }

    public class Opt2Neighbour : Neighbour {
        OrderPosition op;
        int length;
        OrderPosition lastOp;
        OrderPosition[] nodes;
        byte truck;
        byte day;
        Cycle cycle;
        bool[] nodeStat;
        public Opt2Neighbour(Solution s, OrderPosition op, int length) {
            this.s = s;
            this.op = op;
            this.length = length;
            nodes = new OrderPosition[length];
            nodeStat = new bool[length];
            truck = op.truck;
            day = op.Day;
            cycle = op.cycle;
        }
        public override void Apply() {
            OrderPosition before = op.Previous;
            OrderPosition curOp = op;
            OrderPosition nextOp = null;
            for (int i = 0; i < length; i++) {
                nodes[i] = curOp;
                nodeStat[i] = curOp.Active;
                if (curOp.Active) {
                    s.SetActive(false, curOp);
                }
                nextOp = curOp.Next;
                s.RemoveOrder(curOp, false);
                curOp = nextOp;
            }
            lastOp = nodes[length - 1];
            for (int i = length - 1; i >= 0; i--) {
                s.AddOrder(nodes[i], before, truck, day, cycle, false);
                before = nodes[i];
                if (nodeStat[i]) {
                    s.SetActive(true, before);
                }
            }
        }
        public override Neighbour Reverse() {
            return new Opt2Neighbour(s, lastOp, length);
        }
        public override int ShadowGain() {
            return 0; // Will almost never be called, but we can still implement it sometime if we have spare time.
        }
    }
}
