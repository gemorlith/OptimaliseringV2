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
            Util.Test(this);
        }
        public Order NextActive(OrderPosition o) {
            OrderPosition next = o.Next;
            if (next == null) return Program.HomeOrder;
            if (next == next.Next) throw new Exception("next.next is equal to next, that's a problem.");
            while (!next.Active) {
                next = next.Next;
                if (next == null) return Program.HomeOrder;
            }
            return next.order;
        }
        public Order PrevActive(OrderPosition o) {
            OrderPosition prev = o.Previous;
            if (prev == null) return Program.HomeOrder;
            if (prev == prev.Previous) throw new Exception("previous.previous is equal to previous, that's a problem.");
            while (!prev.Active) {
                prev = prev.Previous;
                if (prev == null) return Program.HomeOrder;
            }
            return prev.order;
        }
        public double Value { get { return (timeValue + declineValue + penaltyValue) / 60; } }
        public void SetActive(bool setting, OrderPosition op) {
            op.Active = setting;
            Order prev = PrevActive(op);
            Order next = NextActive(op);
            int withoutTime = Util.PathValue(prev.Location, next.Location);
            int withTime = Util.PathValue(prev.Location, op.order.Location) + Util.PathValue(op.order.Location, next.Location);
            float time;
            int truck = op.truck;
            int day = op.Day;
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
                if (prev == Program.HomeOrder && next == Program.HomeOrder) {
                    time -= Program.HomeOrder.Time;
                }
            }
            timeValue += time;
            declineValue += decline;

            // Begin UpdatePenalties-functie
            Order o = op.order;
            timePen += Program.overTimePenalty * (Math.Max(localTimes[truck, day] + time - Program.MaxTime, 0) - Math.Max(localTimes[truck, day] - Program.MaxTime, 0));
            weightPen += Program.overWeightPenalty * (Math.Max(op.cycle.cycleWeight + weight - Program.MaxCarry, 0) - Math.Max(op.cycle.cycleWeight - Program.MaxCarry, 0));
            freqPen += Program.wrongFreqPenalty * (double)Util.IncreaseFreqPenAmount(o);
            wrongDayPen += Program.wrongDayPentalty * (double)Util.IncreaseInvalidDayPlanning(o);
            penaltyValue = timePen + weightPen + freqPen + wrongDayPen;
            // Eind UpdatePenalties-functie

            localTimes[truck, day] += time;
            op.cycle.cycleWeight += weight;
            if (penaltyValue < 0) {
                if (timePen < 0) Console.WriteLine("TimePenalty: " + timePen);
                if (weightPen < 0) Console.WriteLine("WeightPenalty: " + weightPen);
                if (freqPen < 0) Console.WriteLine("FrequencyPenalty: " + freqPen);
                if (wrongDayPen < 0) Console.WriteLine("WrongDayPenalty: " + wrongDayPen);
                throw new Exception("Penalty value lager dan nul wtfrick.");
            }
            //string yeet;
            //if (setting) yeet = "Na activatie";
            //else yeet = "Na deactivatie";
            //Util.Test(this, yeet, false);
        }
        public void RemoveOrder(OrderPosition order) {
            if (order.Active) throw new Exception("Nou doe maar eerst inactive alsjeblieft.");
            if (order.Next != null) order.Next.Previous = order.Previous;
            if (order.Previous != null) order.Previous.Next = order.Next;
            else order.cycle.first = order.Next;

            if (order.Next == null && order.Previous == null) RemoveCycle(order.cycle);
            //Util.CheckPrevAndNextForLoops(order.next);
            //Util.CheckPrevAndNextForLoops(order.previous);
        }
        public void AddOrder(OrderPosition order, OrderPosition previous, byte truck, byte day, Cycle cycle) {
            if (previous == order) throw new Exception("Je probeert het order na zichzelf te plaatsen, doe maar niet!");
            if (order.Active) throw new Exception("Nou doe maar eerst inactive alsjeblieft.");
            if (cycle != null && cycle.first == null) cycle = AddCycle(day, truck);
            order.Previous = previous;
            if (previous != null) {
                if (truck != previous.truck ||
                    day != previous.Day ||
                    cycle != previous.cycle) throw new Exception("Truck, day or cycle doesn't match.");
                order.Next = previous.Next;
                previous.Next = order;
            }
            else {
                if (cycle != null) {
                    order.Next = cycle.first;
                    cycle.first = order;
                }
                else { // Create new cycle
                    cycle = AddCycle(day, truck);
                    cycle.first = order;
                    order.Next = null;
                }
            }
            if (order.Next != null) order.Next.Previous = order;

            order.Day = day;
            order.truck = truck;
            order.cycle = cycle;
            //Util.CheckPrevAndNextForLoops(order);
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
        // Om één of andere reden zorgt het aanroepen van de UpdatePenalties-functie voor problemen in Release-modus.
        // Daarom hebben we de inhoud van de functie direct in de SetActive-functie gezet.
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
        public int LastValidPlan { get; set; }
        public OrderPosition[] Positions { get; set; }
    }
}
