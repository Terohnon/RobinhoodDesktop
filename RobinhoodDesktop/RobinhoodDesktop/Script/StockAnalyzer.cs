using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public abstract class StockAnalyzer
    {
        #region Types
        /// <summary>
        /// Callback that is executed when the analyzer's condition(s) are met
        /// </summary>
        public delegate void AnalyzerNotifier(string symbol, DateTime time);
        #endregion

        #region Variables
        /// <summary>
        /// Callback that is executed when the analyzer's condition(s) are met
        /// </summary>
        public AnalyzerNotifier Notify;
        #endregion

        /// <summary>
        /// Processes the analyzer for a symbol over a time period
        /// <param name="data">The data to process</param>
        /// <param name="startIndex">The index at which the processing should start (negative values are relative to the end of the list)</param>
        /// <param name="endIndex">The index at which the processing should end (negative values are relative to the end of the list)</param>
        /// </summary>
        public virtual void Process(StockDataSet<StockDataSink> data, int startIndex = -1, int endIndex = -1)
        {
            if(startIndex < 0) startIndex += data.DataSet.Count;
            if(endIndex < 0) endIndex += data.DataSet.Count;
            if((startIndex >= 0) && (endIndex < data.DataSet.Count))
            {
                for(int idx = startIndex; idx <= endIndex; idx++)
                {
                    if(Evaluate(data, idx))
                    {
                        if(Notify != null) Notify(data.Symbol, data.Time(idx));
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
        public abstract bool Evaluate(StockDataSet<StockDataSink> data, int index);
    }
}
