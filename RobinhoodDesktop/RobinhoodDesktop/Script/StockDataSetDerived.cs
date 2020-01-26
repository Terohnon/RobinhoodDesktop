using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class StockDataSetDerived<T, U, V> : StockDataSet<T> where T : struct, StockData where U : struct, StockData 
    {
        public StockDataSetDerived(StockDataSet<U> source, StockDataFile file, StockDataCreator create)
        {
            this.SourceData = source;
            this.File = file;
            this.Start = source.Start;
            this.Symbol = source.Symbol;
            this.Create = create;
        }

        protected StockDataSetDerived()
        {

        }

        /// <summary>
        /// Creates a derived set of data from the given source
        /// </summary>
        /// <param name="source">The source data</param>
        /// <returns>The derived data</returns>
        public static Dictionary<string, List<StockDataSetDerived<T, U, V>>> Derive(Dictionary<string, List<StockDataSet<U>>> source, StockDataFile file, StockDataCreator create)
        {
            var derived = new Dictionary<string, List<StockDataSetDerived<T, U, V>>>();
            foreach(KeyValuePair<string, List<StockDataSet<U>>> pair in source)
            {
                derived[pair.Key] = new List<StockDataSetDerived<T, U, V>>(pair.Value.Count);
                foreach(StockDataSet<U> srcSet in pair.Value)
                {
                    derived[pair.Key].Add(new StockDataSetDerived<T, U, V>(srcSet, file, create));
                }
            }

            return derived;
        }

        #region Variables
        /// <summary>
        /// The data this is derived from
        /// </summary>
        public StockDataSet<U> SourceData;

        /// <summary>
        /// Custom state information that can be referenced while processing the data set
        /// </summary>
        public V ProcessingState;

        /// <summary>
        /// Callback used to create a derived data point
        /// </summary>
        public StockDataCreator Create;
        #endregion

        #region Types
        /// <summary>
        /// Callback used to create a stock data instance
        /// </summary>
        public delegate void StockDataCreator(StockDataSetDerived<T, U, V> data, int idx);
        #endregion

        /// <summary>
        /// Loads the data from the source file
        /// </summary>
        public override void Load()
        {
            if(!IsReady())
            {
                SourceData.Load();
                DataSet.Resize(SourceData.DataSet.Count);
                for(int idx = DataSet.Count; idx < SourceData.DataSet.Count; idx++)
                {
                    Create(this, idx);
                }
                DataSet.Initialize(DataSet.InternalArray);
            }
        }

        /// <summary>
        /// Clears only the derived data, leaving the source data intact
        /// </summary>
        public void ClearDerived()
        {
            base.Clear();
        }

        /// <summary>
        /// Clears both the source and the derived data
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            SourceData.Clear();
        }

        /// <summary>
        /// Indicates if all of the available data is ready
        /// </summary>
        /// <returns>True if the DataSet contains all available data</returns>
        public override bool IsReady()
        {
            return SourceData.IsReady() && (SourceData.DataSet.Count == DataSet.Count);
        }

        /// <summary>
        /// Adds a new source point
        /// </summary>
        /// <param name="source">The source point to add</param>
        public void Add(U source)
        {
            SourceData.DataSet.Add(source);
            DataSet.Add(new T());
            Create(this, SourceData.DataSet.Count - 1);
        }
    }
}
