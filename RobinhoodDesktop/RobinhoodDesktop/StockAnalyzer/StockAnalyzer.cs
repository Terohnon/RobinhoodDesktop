using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public abstract class StockAnalyzer
    {
        #region Variables
        /// <summary>
        /// Callback that is executed when the analyzer's condition(s) are met
        /// </summary>
        public AnalyzerNotifier Notify;
        #endregion

        #region Types
        /// <summary>
        /// Callback that is executed when the analyzer's condition(s) are met
        /// </summary>
        public delegate void AnalyzerNotifier(string symbol, DateTime time);
        #endregion

        /// <summary>
        /// Processes the analyzer for a symbol over a time period
        /// <param name="data">The data to process</param>
        /// <param name="startIndex">The index at which the processing should start (negative values are reletive to the end of the list)</param>
        /// <param name="endIndex">The index at which the processing should end (negative values are reletive to the end of the list)</param>
        /// </summary>
        public virtual void Process<T>(StockDataSet<T> data, int startIndex = -1, int endIndex = -1) where T : struct, StockData
        {
            if(startIndex < 0) startIndex += data.DataSet.Count;
            if(endIndex < 0) startIndex += data.DataSet.Count;
            if((startIndex >= 0) && (endIndex < data.DataSet.Count))
            {
                for(int idx = startIndex; idx <= endIndex; idx++)
                {
                    if(Evaluate(data, idx) && (Notify != null))
                    {
                        Notify(data.Symbol, data.Start.AddSeconds(data.Interval.TotalSeconds * idx));
                    }
                }
            }
        }

        /// <summary>
        /// Evaluates
        /// </summary>
        /// <param name="data">The data set to work from</param>
        /// <param name="index">The index which should be evaluated</param>
        /// <returns>True if the analyzer criteria is met at the given data point</returns>
        public abstract bool Evaluate<T>(StockDataSet<T> data, int index) where T : struct, StockData;
    }
}
