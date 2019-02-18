using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    class PriceMonitor : StockAnalyzer
    {
        public PriceMonitor(double percentage, AnalyzerNotifier notify = null)
        {
            this.Percentage = percentage;
            this.Notify = notify;
        }

        #region Types
        /// <summary>
        /// Callback used to notify a specific instance of a price change
        /// </summary>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="startPrice">The price specified as the base</param>
        /// <param name="endPrice">The price that triggered the notification</param>
        /// <param name="time">The time at which the notification occurs</param>
        /// <returns>True if the notifier should be removed, false if it should continue to be monitored</returns>
        public delegate bool PriceChangeNotifier(string symbol, float startPrice, float endPrice, DateTime time);
        #endregion


        #region Variables
        /// <summary>
        /// Stores a list of symbols and corresponding starting points to be monitoring
        /// </summary>
        public Dictionary<string, List<Tuple<float, PriceChangeNotifier>>> Monitors = new Dictionary<string, List<Tuple<float, PriceChangeNotifier>>>();

        /// <summary>
        /// The change percentage that is being monitored
        /// </summary>
        public double Percentage;

        /// <summary>
        /// True if this should trigger for increases
        /// </summary>
        public bool MonitorIncrease = true;

        /// <summary>
        /// True if this should trigger for decreases
        /// </summary>
        public bool MonitorDecrease = true;
        #endregion

        /// <summary>
        /// Specifies a stock that should be monitored
        /// </summary>
        /// <param name="stock">The stock to monitor</param>
        /// <param name="startPrice">The base price to use as a comparison for the monitor</param>
        /// <param name="notify"></param>
        public void Monitor(string stock, float startPrice, PriceChangeNotifier notify = null)
        {
            List<Tuple<float, PriceChangeNotifier>> stockMonitors;
            if(!Monitors.TryGetValue(stock, out stockMonitors))
            {
                stockMonitors = new List<Tuple<float, PriceChangeNotifier>>();
                Monitors[stock] = stockMonitors;
            }
            stockMonitors.Add(new Tuple<float, PriceChangeNotifier>(startPrice, notify));
        }

        /// <summary>
        /// Evaluates the stock prices to see if it has changed by the specified amount
        /// </summary>
        /// <param name="data">The set of stock data to check</param>
        /// <param name="index">The current index into the stock data</param>
        /// <returns>True if any of the monitors pass</returns>
        public override bool Evaluate(StockDataSet<StockDataSink> data, int index)
        {
            bool condTrue = false;

            List<Tuple<float, PriceChangeNotifier>> stockMonitors;
            if(Monitors.TryGetValue(data.Symbol, out stockMonitors))
            {
                for(int monIdx = 0; monIdx < stockMonitors.Count; monIdx++)
                {
                    Tuple<float, PriceChangeNotifier> mon = stockMonitors[monIdx];
                    float diff = (data[index].Price - mon.Item1);
                    float percentDiff = (diff / mon.Item1);
                    bool inc = ((percentDiff >= Percentage) && MonitorIncrease);
                    bool dec = ((percentDiff <= -Percentage) && MonitorDecrease);
                    if(inc || dec)
                    {
                        condTrue = true;

                        if(mon.Item2 != null)
                        {
                            if(mon.Item2(data.Symbol, mon.Item1, data[index].Price, data.Time(index)))
                            {
                                // Remove this monitor
                                stockMonitors.RemoveAt(monIdx);
                                monIdx--;
                            }
                        }
                    }
                }
            }

            return condTrue;
        }
    }
}
