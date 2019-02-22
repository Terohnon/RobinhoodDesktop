using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    class StockSessionLiveScript
    {
        /// <summary>
        /// Executes a run of processing the stock data
        /// </summary>
        /// <param name="session">The session configuration</param>
        public static void Run(StockSession session)
        {
            // Set up the derived data sink
            var sourceData = new StockDataSet<StockDataSource>("OLED", DateTime.Now, session.SourceFile);
            var sinkData = new StockDataSetDerived<StockDataSink, StockDataSource>(sourceData, session.SinkFile, (data, idx) =>
            {
                var point = new StockDataSink();
                point.Update(data, idx);
                return point;
            });

            // Find when a stock increases by a certain amount
            float targetPrice = 129.0f;
            PriceMonitor buyMonitor = new PriceMonitor(0.025);
            buyMonitor.MonitorIncrease = false;
            buyMonitor.Monitor("OLED", targetPrice, (string stock, float startPrice, float endPrice, DateTime time) =>
            {
                System.Windows.Forms.MessageBox.Show(string.Format("{0} Buy Under {1} @ {2}", stock, endPrice, time));
                return true;
            });
            PriceMonitor sellMonitor = new PriceMonitor(0.0);
            sellMonitor.MonitorDecrease = false;
            sellMonitor.Monitor("OLED", targetPrice, (string stock, float startPrice, float endPrice, DateTime time) =>
            {
                System.Windows.Forms.MessageBox.Show(string.Format("{0} Sell Over {1} @ {2}", stock, endPrice, time));
                return true;
            });

            // Subscribe to the stock price
            var subscription = DataAccessor.Subscribe("OLED", new TimeSpan(0, 0, 5));
            subscription.Notify += (sub) =>
            {
                sinkData.Add(StockDataSource.CreateFromPrice((float)sub.Price));
                buyMonitor.Process(sinkData);
                sellMonitor.Process(sinkData);
            };
        }
    }
}
