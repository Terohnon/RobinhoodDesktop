using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class StockDataSetDerived<T, U> : StockDataSet<T> where T : struct, StockData where U : struct, StockData 
    {
        public StockDataSetDerived(StockDataSet<U> source, StockDataFile file)
        {
            this.SourceData = source;
            this.File = file;
            this.Start = source.Start;
            this.Symbol = source.Symbol;
        }

        protected StockDataSetDerived()
        {

        }

        /// <summary>
        /// Creates a derived set of data from the given source
        /// </summary>
        /// <param name="source">The source data</param>
        /// <returns>The derived data</returns>
        public static Dictionary<string, List<StockDataSet<T>>> Derive(Dictionary<string, List<StockDataSet<U>>> source, StockDataFile file)
        {
            Dictionary<string, List<StockDataSet<T>>> derived = new Dictionary<string, List<StockDataSet<T>>>();
            foreach(KeyValuePair<string, List<StockDataSet<U>>> pair in source)
            {
                derived[pair.Key] = new List<StockDataSet<T>>(pair.Value.Count);
                foreach(StockDataSet<U> srcSet in pair.Value)
                {
                    derived[pair.Key].Add(new StockDataSetDerived<T, U>(srcSet, file));
                }
            }

            return derived;
        }

        #region
        /// <summary>
        /// The data this is derived from
        /// </summary>
        public StockDataSet<U> SourceData;
        #endregion

        #region Types
        /// <summary>
        /// Callback used to create a stock data instance
        /// </summary>
        /// <returns>The created stock data instance</returns>
        public delegate T StockDataCreator();
        #endregion

        /// <summary>
        /// Loads the data from the source file
        /// </summary>
        public override void Load()
        {
            if(!IsReady())
            {
                SourceData.Load();
                DataSet.Initialize(SourceData.DataSet.Count);
                for(int idx = 0; idx < SourceData.DataSet.Count; idx++)
                {
                    var datum = new T();
                    datum.Update(SourceData.DataSet, idx);
                    DataSet.Add(datum);
                }
            }
        }
    }
}
