using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public abstract class StockEvaluator
    {
        /// <summary>
        /// Evaluates
        /// </summary>
        /// <param name="data">The data set to work from</param>
        /// <param name="index">The index which should be evaluated</param>
        /// <param name="target">The reference point to use when evaluating the stock</param>
        /// <returns>True if the analyzer criteria is met at the given data point</returns>
        public abstract bool Evaluate(StockDataSet<StockDataSink> data, int index, StockProcessor.ProcessingTarget target);
    }
}
