using System;
using System.Collections.Generic;
namespace GroteOpdrachtV2 {
    public abstract class Neighbour {
        protected Solution s;
        public abstract void Apply(int[,] paths);
        public abstract int ShadowGain();
        public abstract Neighbour Reverse();
    }
    public class ActivateNeighbour : Neighbour {
        OrderPosition op;
        public ActivateNeighbour(Solution s, OrderPosition op) {
            this.s = s;
            this.op = op;
        }
        public override void Apply(int[,] paths) {
            s.SetActive(paths, true, op);
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
        public override void Apply(int[,] paths) {
            s.SetActive(paths, false, op);
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
        public override void Apply(int[,] paths) {
            bool active = op.Active;
            if (active) s.SetActive(paths, false, op);
            s.RemoveOrder(op, paths);
            s.AddOrder(op, newPrevious, paths, truck, day, cycle);
            if (active) s.SetActive(paths, true, op);
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
        public override void Apply(int[,] paths) {
            OrderPosition prev1 = op1.Previous;
            Cycle cycle1 = op1.cycle;
            byte day1 = op1.Day, truck1 = op1.truck;
            MoveNeighbour move1 = new MoveNeighbour(s, op1, op2.Previous, op2.Day, op2.truck, op2.cycle);
            move1.Apply(paths);
            MoveNeighbour move2 = new MoveNeighbour(s, op2, prev1, day1, truck1, cycle1);
            move2.Apply(paths);
            shadow = move1.ShadowGain() + move2.ShadowGain();
        }
        public override Neighbour Reverse() {
            return new SwapNeighbour(s, op2, op1);
        }
        public override int ShadowGain() {
            return shadow;
        }
    }
}
