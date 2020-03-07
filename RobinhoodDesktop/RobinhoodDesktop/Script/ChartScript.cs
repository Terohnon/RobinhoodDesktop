using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop.Script
{
    public class ChartScript
    {
        /// <summary>
        /// Executes a run of processing the stock data
        /// </summary>
        /// <param name="session">The session configuration</param>
        public static void Run(StockSession session)
        {

        }

        /// <summary>
        /// Creates a stock data chart
        /// </summary>
        /// <param name="session">The session the chart is a part of</param>
        /// <returns>The chart, as a generic control</returns>
        public static Control CreateChart(StockSession session)
        {
            StockProcessor processor = new StockProcessor(session);
            return (Control)(new DataChartGui<StockDataSink>(session.SinkFile)).GuiPanel;
        }
    }
}
