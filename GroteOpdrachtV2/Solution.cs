using System;
using System.Collections.Generic;

namespace GroteOpdrachtV2 {
    public class Solution {
        public double timeValue, declineValue, penaltyValue, tp, wp;
        public List<Cycle>[,] cycles;
        public List<Cycle> allCycles = new List<Cycle>();
        double[,] localTimes;
        public OrderPosition[] allPositions;
        public Solution(double timeValue, double declineValue, double penaltyValue, double[,] localTimes, List<Cycle>[,] cycles, OrderPosition[] allPositions) {
            this.timeValue = timeValue;
            this.declineValue = declineValue;
            this.penaltyValue = penaltyValue;
            tp = 0; wp = 0;
            this.localTimes = localTimes;
            this.cycles = cycles;
            this.allPositions = allPositions;
            foreach (List<Cycle> cl in cycles) {
                foreach (Cycle c in cl) {
                    allCycles.Add(c);
                }
            }
        }
        public Order NextActive(OrderPosition o) {
            OrderPosition next = o.next;
            if (next == null) return Program.HomeOrder;
            if (next == next.next) throw new Exception("next.next is equal to next, that's a problem.");
            while (!next.active) {
                next = next.next;
                if (next == null) return Program.HomeOrder;
            }
            return next.order;
        }
        public Order PrevActive(OrderPosition o) {
            OrderPosition prev = o.previous;
            if (prev == null) return Program.HomeOrder;
            if (prev == prev.previous) throw new Exception("previous.previous is equal to previous, that's a problem.");
            while (!prev.active) {
                prev = prev.previous;
                if (prev == null) return Program.HomeOrder;
            }
            return prev.order;
        }
        public double Value { get { return (timeValue + declineValue + penaltyValue) / 60; } }
        public void SetActive(bool setting, OrderPosition op) {
            op.active = setting;
            Order prev = PrevActive(op);
            Order next = NextActive(op);
            int withoutTime = Util.PathValue(prev.Location, next.Location);
            int withTime = Util.PathValue(prev.Location, op.order.Location) + Util.PathValue(op.order.Location, next.Location);
            float time;
            int truck = op.truck;
            int day = op.day;
            int weight;
            float decline;
            if (setting) {
                time = op.order.Time + withTime - withoutTime;
                weight = op.order.ContainerVolume;
                decline = -op.order.Time * 3;
                if (prev == Program.HomeOrder && next == Program.HomeOrder) time += Program.HomeOrder.Time;
            }
            else {
                time = -op.order.Time + withoutTime - withTime;
                weight = -op.order.ContainerVolume;
                decline = op.order.Time * 3;
                if (prev == Program.HomeOrder && next == Program.HomeOrder) time -= Program.HomeOrder.Time;
            }
            localTimes[truck, day] += time;
            timeValue += time;
            declineValue += decline;
            op.cycle.cycleWeight += weight;
            tp += Program.overTimePenalty * (Math.Max(localTimes[truck, day] + time - Program.MaxTime, 0) - Math.Max(localTimes[truck, day] - Program.MaxTime, 0));
            wp += Program.overWeightPenalty * (Math.Max(op.cycle.cycleWeight + weight - Program.MaxCarry, 0) - Math.Max(op.cycle.cycleWeight - Program.MaxCarry, 0));
            penaltyValue = tp + wp;
            if (penaltyValue < 0) throw new Exception("Penalty value lager dan nul wtfrick.");
            Util.Test(this, "Na (de)activatie");
        }
        public void RemoveOrder(OrderPosition order) {
            if (order.active) throw new Exception("Nou doe maar eerst inactive alsjeblieft.");
            if (order.next != null) order.next.previous = order.previous;
            if (order.previous != null) order.previous.next = order.next;
            else order.cycle.first = order.next;

            if (order.next == null && order.previous == null) RemoveCycle(order.cycle);
        }
        public void AddOrder(OrderPosition order, OrderPosition previous, byte truck, byte day, Cycle cycle) {
            if (order.active) throw new Exception("Nou doe maar eerst inactive alsjeblieft.");
            order.previous = previous;
            if (previous != null) {
                if (truck != previous.truck ||
                    day != previous.day ||
                    cycle != previous.cycle) throw new Exception("Truck, day or cycle doesn't match.");
                order.next = previous.next;
                previous.next = order;
            }
            else {
                if (cycle != null) {
                    order.next = cycle.first;
                    cycle.first = order;
                }
                else { // Create new cycle
                    cycle = AddCycle(day, truck);
                    cycle.first = order;
                    order.next = null;
                }
            }
            if (order.next != null) order.next.previous = order;

            order.day = day;
            order.truck = truck;
            order.cycle = cycle;
        }
        public void RemoveCycle(Cycle cycle) {
            if (cycle.first != null) throw new Exception("Do not remove a cycle that has active orders.");
            if (cycle.cycleWeight != 0) throw new Exception("Cycle weight is not zero but the cycle has no active orders. Maybe consider fixing the software.");
            allCycles.Remove(cycle);
            cycles[cycle.truck, cycle.day].Remove(cycle);
        }
        public Cycle AddCycle(byte day, byte truck) {
            Cycle c = new Cycle(day, truck, 0, null);
            allCycles.Add(c);
            cycles[truck, day].Add(c);
            return c;
        }
    }

    public class Cycle {
        public byte day, truck;
        public int cycleWeight;
        public OrderPosition first;
        public Cycle(byte day, byte truck, int cycleWeight, OrderPosition first) {
            this.day = day;
            this.truck = truck;
            this.cycleWeight = cycleWeight;
            this.first = first;
        }
    }

    public class OrderPosition {
        public Order order;
        public OrderPosition next;
        public OrderPosition previous;
        public Cycle cycle;
        public byte day, truck;
        public bool active;
        public OrderPosition(Order o, byte day, byte truck, Cycle cycle, bool active) {
            order = o;
            this.day = day;
            this.truck = truck;
            this.cycle = cycle;
            this.active = active;
        }
    }
}
