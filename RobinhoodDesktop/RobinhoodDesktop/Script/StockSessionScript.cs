using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class StockSessionScript
    {
        /// <summary>
        /// Executes a run of processing the stock data
        /// </summary>
        /// <param name="session">The session configuration</param>
        public static void Run(StockSession session)
        {
            // Set up the derived data sink
            var sourceData = session.SourceFile.GetSegments<StockDataSource>();
            var sinkData = StockDataSetDerived<StockDataSink, StockDataSource>.Derive(sourceData, session.SinkFile, (data, idx) =>
                {
                    var point = new StockDataSink();
                    point.Update(data, idx);
                    return point;
                });
            session.SinkFile.SetSegments(sinkData);

            // Find when a stock increases by a certain amount
            PriceMonitor priceMonitor = new PriceMonitor(0.025);
            priceMonitor.MonitorDecrease = false;
            sinkData["AA"][0].Load();
            priceMonitor.Monitor("AA", sinkData["AA"][0][0].Price, (string stock, float startPrice, float endPrice, DateTime time) =>
            {
                System.Windows.Forms.MessageBox.Show(string.Format("{0} {1:C}->{2:C} @ {3}", stock, startPrice, endPrice, time));
                return true;
            });

            // Load the first set of data
            foreach(var pair in sinkData)
            {
                foreach(var set in pair.Value)
                {
                    set.Load();
                    priceMonitor.Process(set, 0);
                }
            }
        }
    }
}
