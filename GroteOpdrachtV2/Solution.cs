using System;
using System.Collections.Generic;

namespace GroteOpdrachtV2 {
    public class Solution {
        public double timeValue, declineValue, penaltyValue;
        List<int>[,] cycleWeights;
        double[,] localTimes;
        public List<OrderPosition>[,] firsts;
        public OrderPosition[] allPositions;
        Order nextActive(OrderPosition o) {
            OrderPosition next = o.next;
            if (next == null) return Program.HomeOrder;
            if (next.active) return next.order;
            return nextActive(next);
        }
        Order prevActive(OrderPosition o) {
            OrderPosition prev = o.previous;
            if (prev == null) return Program.HomeOrder;
            if (prev.active) return prev.order;
            return prevActive(prev);
        }
        public double Value { get { return (timeValue + declineValue + penaltyValue) / 60; } }
        public void setActive(bool setting, OrderPosition op) {
            op.active = setting;
            Order prev = prevActive(op);
            Order next = nextActive(op);
            int withoutTime = Program.paths[prev.Location].Paths[next.Location];
            int withTime = Program.paths[prev.Location].Paths[op.order.Location] + Program.paths[op.order.Location].Paths[next.Location];
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
            cycleWeights[truck, day][op.cycle] += weight;

            int currentCycleWeight = cycleWeights[truck, day][op.cycle];
            penaltyValue += Program.overTimePenalty * (Math.Max(localTimes[truck, day] + time - Program.MaxTime, 0) - Math.Max(localTimes[truck, day] - Program.MaxTime, 0));
            penaltyValue += Program.overWeightPenalty * (Math.Max(currentCycleWeight + weight - Program.MaxCarry, 0) - Math.Max(currentCycleWeight - Program.MaxCarry, 0));
            if (penaltyValue < 0) {
                throw new Exception("je hoofd");
            }
        }
        public void RemoveOrder(OrderPosition order) {
            if (order.next != null) order.next.previous = order.previous;
            if (order.previous != null) order.previous.next = order.next;

            if (order.next == null && order.previous == null) {
                if (cycleWeights[order.truck, order.day][order.cycle] != 0) throw new Exception("yo shit broke");
                firsts[order.truck, order.day].RemoveAt(order.cycle);
                cycleWeights[order.truck, order.day].RemoveAt(order.cycle);
            }
        }
        public void AddOrder(OrderPosition order, OrderPosition previous, byte truck, byte day, byte cycle) {
            order.previous = previous;
            if (previous != null) {
                previous.next = order;
                order.next = previous.next;
            }
            else {
                if (firsts[truck, day].Count > cycle) {
                    order.next = firsts[truck, day][0];
                    firsts[truck, day][0] = order;
                }
                else {
                    firsts[truck, day].Add(order);
                    cycleWeights[truck, day].Add(0);
                }
            }
            if (order.next != null) order.next.previous = order;

            order.day = day;
            order.truck = truck;
            order.cycle = cycle;
        }
        public Solution(double timeValue, double declineValue, double penaltyValue, List<int>[,] cycleWeights, double[,] localTimes, List<OrderPosition>[,] firsts, OrderPosition[] allPositions) {
            this.timeValue = timeValue;
            this.declineValue = declineValue;
            this.penaltyValue = penaltyValue;
            this.cycleWeights = cycleWeights;
            this.localTimes = localTimes;
            this.firsts = firsts;
            this.allPositions = allPositions;
        }
    }

    public class OrderPosition {
        public Order order;
        public OrderPosition next;
        public OrderPosition previous;
        public byte day, truck, cycle;
        public bool active;
        public OrderPosition(Order o, byte day, byte truck, byte cycle, bool active) {
            order = o;
            this.day = day;
            this.truck = truck;
            this.cycle = cycle;
            this.active = active;
        }
    }
}
