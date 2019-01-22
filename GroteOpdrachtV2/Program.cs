using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GroteOpdrachtV2 {
    class Program {

        #region Debug
        public const int pasteFreq = 10000;
        public const int saveFreq = 50; //saves solution in temp file every [pasteFreq * saveFreq] neighbours
        #endregion Debug

        #region Parameters
        public static float annealingStartT = 200f;//150
        public static StartSolutionGenerator Generator = new EmptyGenerator();
        public static SearchType Searcher = new SimulatedAnnealingMK1();
        public const int maxIterations = 500000000;//10000000?
        public const double annealingQPerNSSize = 8;//8
        public const float alpha = 0.995f;//0.99
        public const double overTimePenalty = 8;//?
        public const double overWeightPenalty = 100;//>15
        public static List<ValuePerNeighbour> neighbourOptions; // Initialised in Main()
        public static int complexityEstimate = 20000;
        #endregion Parameters

        #region Constants
        public const int MaxCarry = 20000;
        public const int MaxTime = 12 * 60 * 60;
        public const short DisposalTime = 30 * 60;
        public const short Home = 287;
        public static Order HomeOrder = new Order(0, "", 0, 0, 0, 30, 287);
        #endregion Constants

        #region Variables
        public static double minValue = double.MaxValue;
        public static double unviableMinValue = double.MaxValue;
        public static Dictionary<short, DirectionList> paths = new Dictionary<short, DirectionList>();
        public static List<Order> allOrders = new List<Order>();
        public static Dictionary<int, Order> orderByID = new Dictionary<int, Order>();
        public static Random random = new Random();
        #endregion Variables

        static void Main(string[] args) {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            GetDistances();
            GetOrders();
            neighbourOptions = new List<ValuePerNeighbour> {
                new ValuePerNeighbour(0.5f, new ToggleSpace()),
                new ValuePerNeighbour(0.5f, new MoveSpace())
            };
            for (int i = 0; i < 10000; i++) {
                Searcher.Search();
                Console.WriteLine(i);
                annealingStartT *= 0.9f;
                if (annealingStartT < 5f) {
                    annealingStartT += 25f;
                }
            }
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        #region Setup
        static void GetDistances() {
            StreamReader sr = new StreamReader("../../Distances.txt");
            string input;
            string[] splitted;
            sr.ReadLine();
            while ((input = sr.ReadLine()) != null) {
                splitted = input.Split(';');
                short origin = short.Parse(splitted[0]);
                if (!paths.ContainsKey(origin)) {
                    DirectionList d = new DirectionList(origin);
                    paths.Add(origin, d);
                }
                paths[origin].Paths.Add(short.Parse(splitted[1]), int.Parse(splitted[3]));
            }
            foreach (DirectionList dl in paths.Values) {
                List<KeyValuePair<short, int>> orderedPaths = dl.Paths.OrderBy(x => x.Value).ToList();
                foreach (KeyValuePair<short, int> kv in orderedPaths) {
                    dl.SortedDistances.Add(kv.Key);
                }
            }
        }

        static void GetOrders() {
            StreamReader sr = new StreamReader("../../Orders.txt");
            string input;
            string[] splitted;
            sr.ReadLine();
            while ((input = sr.ReadLine()) != null) {
                splitted = input.Split(';');
                int id = int.Parse(splitted[0]);
                string place = splitted[1];
                byte pwk = (byte)(splitted[2][0] - '0');
                byte containers = byte.Parse(splitted[3]);
                short containervolume = short.Parse(splitted[4]);
                float time = float.Parse(splitted[5]);
                short location = short.Parse(splitted[6]);
                long xCoordinate = long.Parse(splitted[7]);
                long yCoordinate = long.Parse(splitted[8]);
                Order order = new Order(id, place, pwk, containers, containervolume, time, location);
                allOrders.Add(order);
                orderByID.Add(order.ID, order);
                paths[location].Orders.Add(order);
                paths[location].XCoord = xCoordinate;
                paths[location].YCoord = yCoordinate;
            }
            orderByID.Add(0, new Order(0, "WHAT", 0, 0, 0, DisposalTime / 60, Home));
        }
        #endregion Setup
    }

    public class Order {
        public Order(int id, string place, byte frequency, byte containers, short containervolume, float time, short location) {
            ID = id;
            Place = place;
            Frequency = frequency;
            ContainerVolume = (short)(containervolume * containers / 5);
            Time = 60 * time;
            Location = location;
        }
        public int ID { get; set; }
        public string Place { get; set; }
        public byte Frequency { get; set; }
        public short ContainerVolume { get; set; }
        public float Time { get; set; }
        public short Location { get; set; }
    }

    public class DirectionList {
        public DirectionList(short start) {
            Start = start;
            Paths = new Dictionary<short, int>();
            SortedDistances = new List<short>();
            Orders = new List<Order>();
            XCoord = 0;
            YCoord = 0;
        }
        public short Start { get; set; }
        public Dictionary<short, int> Paths { get; set; }
        public List<short> SortedDistances { get; set; }
        public List<Order> Orders { get; set; }
        public long XCoord { get; set; }
        public long YCoord { get; set; }
    }
}
