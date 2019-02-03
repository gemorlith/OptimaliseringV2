using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GroteOpdrachtV2 {
    class Program {

        #region Debug
        public const long printFreq = 100000;
        public const long saveFreq = 50; //saves solution in temp file every [pasteFreq * saveFreq] neighbours
        #endregion Debug

        #region Parameters
        public static float annealingStartT = .1f;//150
        public static StartSolutionGenerator Generator = new ReadGenerator(".../.../Solutions/BestSolution.txt");
        //public static StartSolutionGenerator Generator = new EmptyGenerator();
        public static SearchType Searcher = new SimulatedAnnealingMK1();
        public const long maxIterations = 500000000;//10000000?
        public const double annealingQPerNSSize = 16;//8
        public const float alpha = 0.998f;//0.99
        public const double overTimePenaltyBase = 8;//?       
        public const double overWeightPenaltyBase = 80;//>15
        public const double wrongFreqPenaltyBase = 400;//10000
        public const double wrongDayPentaltyBase = 1500;//10000
        public static List<ValuePerNeighbour> neighbourOptions; // Initialised in Main()
        public static int complexityEstimate = 20000;
        public const double timePenMult = 1;//crashes if <> 1
        public const double weightPenMult = 1;
        public const double dayPenMult = 1;
        public const double freqPenMult = 1;
        #endregion Parameters

        #region Constants
        public const int MaxCarry = 20000;
        public const int MaxTime = 12 * 60 * 60;
        public const short DisposalTime = 30 * 60;
        public const short Home = 287;
        public static Order HomeOrder = new Order(0, "", 0, 0, 0, 30, 287);
        public static long MaxPrint = maxIterations / printFreq;
        #endregion Constants

        #region Variables
        public static double minValue = double.MaxValue;
        public static double unviableMinValue = double.MaxValue;
        public static int[,] paths = new int[1099,1099];
        public static List<Order> allOrders = new List<Order>();
        public static OrderPosition[] allPositions;
        public static Dictionary<int, Order> orderByID = new Dictionary<int, Order>();
        public static Random random = new Random();
        public static double overTimePenalty = overTimePenaltyBase;
        public static double overWeightPenalty = overWeightPenaltyBase;
        public static double wrongFreqPenalty = wrongDayPentaltyBase;
        public static double wrongDayPentalty = wrongDayPentaltyBase;
        #endregion Variables

        static void Main(string[] args) {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            GetDistances();
            GetOrders();
            neighbourOptions = new List<ValuePerNeighbour> {
                new ValuePerNeighbour(0.001f, new ToggleSpace()),
                new ValuePerNeighbour(0.299f, new ActivateSpace()),
                new ValuePerNeighbour(0.22f, new MoveSpace()),
                new ValuePerNeighbour(0.1f, new SwapSpace()),
                new ValuePerNeighbour(0.01f, new Opt2Space()),
                new ValuePerNeighbour(0.17f, new MoveAndActivateSpace()),
                new ValuePerNeighbour(0.2f, new ToggleOrderSpace())
            };
            Util.NormalizeVPNs(neighbourOptions);
            for (int i = 0; i < 10000; i++) {
                Searcher.Search();
                Console.WriteLine(i);
                annealingStartT *= 0.8f;
                if (annealingStartT < 0.05f) {
                    annealingStartT += 1f;
                }
                ResetOps();
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
                short destination = short.Parse(splitted[1]);
                int tijdsduur = int.Parse(splitted[3]);
                paths[origin, destination] = tijdsduur;
            }
            /*
            foreach (DirectionList dl in paths.Values) {
                List<KeyValuePair<short, int>> orderedPaths = dl.Paths.OrderBy(x => x.Value).ToList();
                foreach (KeyValuePair<short, int> kv in orderedPaths) {
                    dl.SortedDistances.Add(kv.Key);
                }
            }*/
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
            }
            orderByID.Add(0, new Order(0, "WHAT", 0, 0, 0, DisposalTime / 60, Home));
            ResetOps();
        }
        #endregion Setup

        static void ResetOps() {
            List<OrderPosition> opList = new List<OrderPosition>();
            foreach (Order o in allOrders) {
                for (int i = 0; i < o.Frequency; i++) {
                    o.ActiveFreq = 0;
                    o.LastFreqPenAmount = 0;
                    o.LastValidPlan = 0;
                    OrderPosition op = new OrderPosition(o, 0, 0, null, false);
                    opList.Add(op);
                    o.Positions[i] = op;
                }
            }
            allPositions = opList.ToArray();
        }
    }
}
