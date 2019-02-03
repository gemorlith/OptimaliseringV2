using System;
using System.Collections.Generic;

namespace GroteOpdrachtV2 {
    public class Solution {
        public double timeValue, declineValue, penaltyValue, timePen, weightPen, freqPen, wrongDayPen;
        public List<Cycle>[,] cycles;
        public List<Cycle> allCycles = new List<Cycle>();
        double[,] localTimes;
        public Solution(double timeValue, double declineValue, double penaltyValue, double[,] localTimes, List<Cycle>[,] cycles) {
            this.timeValue = timeValue;
            this.declineValue = declineValue;
            this.penaltyValue = penaltyValue;
            timePen = 0; weightPen = 0; freqPen = 0; wrongDayPen = 0;
            this.localTimes = localTimes;
            this.cycles = cycles;
            foreach (List<Cycle> cl in cycles) {
                foreach (Cycle c in cl) {
                    allCycles.Add(c);
                }
            }
            foreach (Order o in Program.allOrders) {
                Util.IncreaseFreqPenAmount(o);
                Util.IncreaseInvalidDayPlanning(o);
            }
        }
        // Functions for finding the next or previous active Order, respectively, since Orders are never removed, just set to inactive
        public Order NextActive(OrderPosition o) {
            OrderPosition next = o.Next;
            if (next == null) return Program.HomeOrder;
            while (!next.Active) {
                next = next.Next;
                if (next == null) return Program.HomeOrder;
            }
            return next.order;
        }
        public Order PrevActive(OrderPosition o) {
            OrderPosition prev = o.Previous;
            if (prev == null) return Program.HomeOrder;
            while (!prev.Active) {
                prev = prev.Previous;
                if (prev == null) return Program.HomeOrder;
            }
            return prev.order;
        }
        public double Value { get { return (timeValue + declineValue + penaltyValue) / 60; } }
        // Function for (de)activating a given OrderPosition, depending on boolean 'setting'
        public void SetActive(bool setting, OrderPosition op) {
            op.Active = setting;
            Order prev = PrevActive(op);
            Order next = NextActive(op);
            int withoutTime = Util.PathValue(prev.Location, next.Location);
            int withTime = Util.PathValue(prev.Location, op.order.Location) + Util.PathValue(op.order.Location, next.Location);
            int truck = op.truck;
            int day = op.Day;
            float time;
            int weight;
            float decline;
            // If the OrderPosition is being activated, add the appropriate time and weight differences, and remove the appropriate "penalty" time for declining an order
            if (setting) {
                time = op.order.Time + withTime - withoutTime;
                weight = op.order.ContainerVolume;
                decline = -op.order.Time * 3;
                if (prev == Program.HomeOrder && next == Program.HomeOrder) time += Program.HomeOrder.Time;
            }
            // Else, do the opposite
            else {
                time = -op.order.Time + withoutTime - withTime;
                weight = -op.order.ContainerVolume;
                decline = op.order.Time * 3;
                if (prev == Program.HomeOrder && next == Program.HomeOrder) time -= Program.HomeOrder.Time;
            }
            timeValue += time;
            declineValue += decline;

            // Start UpdatePenalties function (see explanation above the function)
            Order o = op.order;
            timePen += Program.overTimePenalty * (Math.Max(localTimes[truck, day] + time - Program.MaxTime, 0) - Math.Max(localTimes[truck, day] - Program.MaxTime, 0));
            weightPen += Program.overWeightPenalty * (Math.Max(op.cycle.cycleWeight + weight - Program.MaxCarry, 0) - Math.Max(op.cycle.cycleWeight - Program.MaxCarry, 0));
            freqPen += Program.wrongFreqPenalty * (double)Util.IncreaseFreqPenAmount(o);
            wrongDayPen += Program.wrongDayPentalty * (double)Util.IncreaseInvalidDayPlanning(o);
            penaltyValue = timePen + weightPen + freqPen + wrongDayPen;
            // End UpdatePenalties function

            localTimes[truck, day] += time;
            op.cycle.cycleWeight += weight;
        }
        // Function for removing an OrderPosition 'op' (that means tying op's Previous and Next to each other)
        public void RemoveOrder(OrderPosition op, bool mayRemoveCycle = true) {
            // Never call this function without deactivating op first! There used to be an Exception here for that, but we removed it for optimisation purposes.
            if (op.Next != null) op.Next.Previous = op.Previous;
            if (op.Previous != null) op.Previous.Next = op.Next;
            else op.cycle.first = op.Next;
            // Remove a Cycle if it has become empty
            if (op.Next == null && op.Previous == null && mayRemoveCycle) RemoveCycle(op.cycle);
        }
        // Function for adding an OrderPosition 'op' (that also means fixing all Previous and Next properties, but also adding Cycles if necessary)
        public void AddOrder(OrderPosition op, OrderPosition previous, byte truck, byte day, Cycle cycle, bool mayAddCycle = true) {
            // Never call this function without deactivating op first! There used to be an Exception here for that, but we removed it for optimisation purposes.
            // Never call this function with equal op and previous! There used to be an Exception here for that, but we removed it for optimisation purposes.
            if (cycle != null && cycle.first == null && mayAddCycle) cycle = AddCycle(day, truck);
            op.Previous = previous;
            // If op should be placed after a planned OrderPosition:
            if (previous != null) {
                op.Next = previous.Next;
                previous.Next = op;
            }
            // Else:
            else {
                // If op should be placed at the start of an existing Cycle:
                if (cycle != null) {
                    op.Next = cycle.first;
                    cycle.first = op;
                }
                // Else, if op should be placed at the start of a new Cycle:
                else {
                    cycle = AddCycle(day, truck);
                    cycle.first = op;
                    op.Next = null;
                }
            }
            if (op.Next != null) op.Next.Previous = op;

            op.Day = day;
            op.truck = truck;
            op.cycle = cycle;
        }
        // Functions for removing and adding a Cycle
        public void RemoveCycle(Cycle cycle) {
            allCycles.Remove(cycle);
            cycles[cycle.truck, cycle.day].Remove(cycle);
        }
        public Cycle AddCycle(byte day, byte truck) {
            Cycle c = new Cycle(day, truck, 0, null);
            allCycles.Add(c);
            cycles[truck, day].Add(c);
            return c;
        }
        // For some reason, calling the UpdatePenalties function causes problems in Release mode.
        // We have thus placed its contents directly into the SetActive function instead of calling it.
        public void UpdatePenalties(OrderPosition op, float time, int weight) {
            int truck = op.truck, day = op.Day;
            Order o = op.order;
            timePen += Program.overTimePenalty * (Math.Max(localTimes[truck, day] + time - Program.MaxTime, 0) - Math.Max(localTimes[truck, day] - Program.MaxTime, 0));
            weightPen += Program.overWeightPenalty * (Math.Max(op.cycle.cycleWeight + weight - Program.MaxCarry, 0) - Math.Max(op.cycle.cycleWeight - Program.MaxCarry, 0));
            freqPen += Program.wrongFreqPenalty * Util.IncreaseFreqPenAmount(o);
            wrongDayPen += Program.wrongDayPentalty * Util.IncreaseInvalidDayPlanning(o);
            penaltyValue = timePen + weightPen + freqPen + wrongDayPen;
        }
    }

    public class Cycle {
        // A class keeping track of a Cycle's day, truck, weight and first OrderPosition
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
        // A class containing the complete position information of an order, as well as whether or not it's being carried out and some other values
        public OrderPosition(Order o, byte day, byte truck, Cycle cycle, bool active) {
            order = o;
            this.Day = day;
            this.truck = truck;
            this.cycle = cycle;
            this.Active = active;
        }
        private bool active;
        public Order order;
        private OrderPosition nxt;
        private OrderPosition prv;
        public OrderPosition Next {
            get { return nxt; }
            set {
                nxt = value;
                pathShadow = Util.ShadowPath(this);
            }
        }
        public OrderPosition Previous {
            get { return prv; }
            set {
                prv = value;
                pathShadow = Util.ShadowPath(this);
            }
        }
        public Cycle cycle;
        public int pathShadow;
        public int dayShadow;
        public int Shadow { get { return pathShadow + dayShadow; } }
        private byte day;
        public byte Day { get { return day; } set { day = value; dayShadow = Util.ShadowDay(this); } }
        public byte truck;
        public bool Active {
            get { return active; }
            set {
                if (active && !value) order.ActiveFreq--;
                if (!active && value) order.ActiveFreq++;
                active = value;
            }
        }
    }

    public class Order {
        // A class containing the Order information extracted by the Program.GetOrders() function, as well as information about all the OrderPosition information it corresponds to
        public Order(int id, string place, byte frequency, byte containers, short containervolume, float time, short location) {
            ID = id;
            Place = place;
            Frequency = frequency;
            ContainerVolume = (short)(containervolume * containers / 5);
            Time = 60 * time;
            Location = location;
            Positions = new OrderPosition[frequency];
        }
        public int ID { get; set; }
        public string Place { get; set; }
        public byte Frequency { get; set; }
        public byte ActiveFreq { get; set; }
        public short ContainerVolume { get; set; }
        public float Time { get; set; }
        public short Location { get; set; }
        public int LastFreqPenAmount { get; set; }
        public int LastInvalidPlan { get; set; }
        public OrderPosition[] Positions { get; set; }
    }
}
