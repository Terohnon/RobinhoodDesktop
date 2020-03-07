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
            // Create the stock processor
            StockProcessor processor = new StockProcessor(session);

            // Set the change percentages to monitor
            StockDataSink.ChangePercentages = new float[] { -0.025f, -0.05f, -0.10f };

            // Set the evaluator and resulting action
            processor.Evaluator = new PriceEvaluator()
            {
                Percentage = 0.025f,
                MonitorIncrease = true,
                MonitorDecrease = false,
                ReferencePrices = new Dictionary<string, float>() { { "GNTX", 18.0f} }
            };
            processor.Action = new StockAction();
            processor.Action.Do += processor.Action.Notify;

            // Add targets for the stocks to monitor
            processor.Add(new StockProcessor.ProcessingTarget("GNTX"));

            // Run the data through the processor
            processor.Process(false, StockProcessor.MemoryScheme.MEM_KEEP_DERIVED);
        }
    }
}
