using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class PriceEvaluator : StockEvaluator
    {
        #region Variables
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

        /// <summary>
        /// Stores the reference prices to compare each data point against
        /// </summary>
        public Dictionary<string, float> ReferencePrices = new Dictionary<string, float>();
        #endregion

        /// <summary>
        /// Evaluates
        /// </summary>
        /// <param name="data">The data set to work from</param>
        /// <param name="index">The index which should be evaluated</param>
        /// <param name="target">The reference point to use when evaluating the stock</param>
        /// <returns>True if the analyzer criteria is met at the given data point</returns>
        public override bool Evaluate(StockDataSet<StockDataSink> data, int index, StockProcessor.ProcessingTarget target)
        {
            float refPrice;
            if(!ReferencePrices.TryGetValue(target.Symbol, out refPrice))
            {
                refPrice = data[index].Price;
                ReferencePrices.Add(target.Symbol, refPrice);
            }
            var percentDiff = ((data[index].Price - refPrice) / refPrice);
            return (((percentDiff >= Percentage) && MonitorIncrease) ||
                    ((percentDiff <= -Percentage) && MonitorDecrease));
        }
    }
}
