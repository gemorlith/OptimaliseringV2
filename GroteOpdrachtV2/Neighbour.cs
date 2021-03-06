﻿namespace GroteOpdrachtV2 {
    public abstract class Neighbour {
        // Template for all Neighbours
        protected Solution s;
        public abstract void Apply();
        public abstract Neighbour Reverse();
        public abstract int ShadowGain();
    }
    public class ActivateNeighbour : Neighbour {
        // Neighbour that activates a given OrderPosition
        OrderPosition op;
        public ActivateNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply() {
            s.SetActive(true, op);
        }
        public override Neighbour Reverse() {
            return new DeactivateNeighbour(s, op);
        }
        public override int ShadowGain() {
            return 0;
        }
    }
    public class DeactivateNeighbour : Neighbour {
        // Neighbour that deactivates a given OrderPosition
        OrderPosition op;
        public DeactivateNeighbour(Solution s, OrderPosition op) {
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
        // Neighbour that moves OrderPosition 'op' to after OrderPosition 'newPrevious', at the start of Cycle 'cycle' or at the start of a new Cycle on day 'day' and truck 'truck'
        OrderPosition op, newPrevious, oldPrevious;
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
            if (op != newPrevious) {
                bool active = op.Active;
                if (active) s.SetActive(false, op);
                s.RemoveOrder(op);
                s.AddOrder(op, newPrevious, truck, day, cycle);
                if (active) s.SetActive(true, op);
            }
        }
        public override Neighbour Reverse() {
            return new MoveNeighbour(s, op, oldPrevious, oldDay, oldTruck, oldCycle);
        }
        public override int ShadowGain() {
            return op.Shadow - initialShadow;
        }
    }
    public class SwapNeighbour : Neighbour {
        // Neighbour that swaps two OrderPositions
        OrderPosition op1, op2;
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
        // Neighbour that mirrors a sequence of consecutive OrderPositions from 'op' with a length of 'length'
        OrderPosition op, lastOp;
        OrderPosition[] nodes;
        Cycle cycle;
        int length;
        byte day, truck;
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

    public class MoveAndSetNeighbour : Neighbour {
        // Neighbour that performs the same actions as MoveNeighbour, then activates OrderPosition 'op' if it was not active already
        OrderPosition op, newPrevious;
        MoveNeighbour mn;
        Cycle cycle;
        bool active, status;
        byte day, truck;
        int initialShadow;
        public MoveAndSetNeighbour(Solution s, OrderPosition op, OrderPosition newPrevious, byte day, byte truck, Cycle cycle, bool status) {
            this.s = s;
            this.op = op;
            this.newPrevious = newPrevious;
            this.day = day;
            this.truck = truck;
            this.cycle = cycle;
            this.status = status;
            mn = new MoveNeighbour(s, op, newPrevious, day, truck, cycle);
            active = op.Active;
            initialShadow = op.Shadow;
        }
        public override void Apply() {
            mn.Apply();
            if (active != status) {
                s.SetActive(status, op);
            }
        }
        public override Neighbour Reverse() {
            if (active == status) {
                return mn.Reverse();
            }
            return new MoveAndSetNeighbour(s, op, newPrevious, day, truck, cycle, active);
        }
        public override int ShadowGain() {
            return op.Shadow - initialShadow;
        }
    }

    public class SetMultipleNeighbour : Neighbour {
        // Neighbour that activates or deactivates OrderPositions in array 'ops', depending on the corresponding boolean array 'statuses'
        OrderPosition[] ops;
        bool[] statuses, oldStats;
        public SetMultipleNeighbour(Solution s, OrderPosition[] ops, bool[] statuses) {
            this.s = s;
            this.ops = ops;
            this.statuses = statuses;
            oldStats = new bool[statuses.Length];
        }
        public override void Apply() {
            for (int i = 0; i < ops.Length; i++) {
                oldStats[i] = ops[i].Active;
                if (statuses[i]) { if (!ops[i].Active) (new ActivateNeighbour(s, ops[i])).Apply(); }
                else if (ops[i].Active) (new DeactivateNeighbour(s, ops[i])).Apply();
            }
        }
        public override Neighbour Reverse() {
            return new SetMultipleNeighbour(s, ops, oldStats);
        }
        public override int ShadowGain() {
            return 0;
        }
    }

    public class ToggleOrderNeighbour : Neighbour {
        // Neighbour that activates or deactivates all the OrderPositions corresponding to Order 'o', depending on boolean 'status'
        Order o;
        OrderPosition[] ops;
        SetMultipleNeighbour smn;
        bool status;
        bool[] oldStats, newStats;
        public ToggleOrderNeighbour(Solution s, Order o, bool status) {
            this.s = s;
            this.o = o;
            this.status = status;
            ops = o.Positions;
            oldStats = new bool[ops.Length];
            newStats = new bool[ops.Length];
        }
        public override void Apply() {
            for (int i = 0; i < ops.Length; i++) {
                oldStats[i] = ops[i].Active;
                newStats[i] = status;
            }
            smn = new SetMultipleNeighbour(s, ops, newStats);
            smn.Apply();
        }
        public override Neighbour Reverse() {
            return smn.Reverse();
        }
        public override int ShadowGain() {
            return 0;
        }
    }
}
