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
            var sinkData = StockDataSetDerived<StockDataSink, StockDataSource>.Derive(sourceData, session.SinkFile);
            session.SinkFile.SetSegments(sinkData);

            // Load the first set of data
            foreach(var pair in sinkData)
            {
                pair.Value[0].Load();
            }
        }
    }
}
