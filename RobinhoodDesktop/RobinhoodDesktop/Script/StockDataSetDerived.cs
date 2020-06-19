using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class StockDataSetDerived<T, U, V> : StockDataSet<T> where T : struct, StockData where U : struct, StockData 
    {
        public StockDataSetDerived(StockDataSet<U> source, StockDataFile file, StockDataCreator create, StockProcessingStateAccessor stateGetter)
        {
            this.SourceData = source;
            this.File = file;
            this.Start = source.Start;
            this.Symbol = source.Symbol;
            this.Create = create;
            this.GetState = stateGetter;
        }

        protected StockDataSetDerived()
        {

        }

        /// <summary>
        /// Creates a derived set of data from the given source
        /// </summary>
        /// <param name="source">The source data</param>
        /// <returns>The derived data</returns>
        public static Dictionary<string, List<StockDataSetDerived<T, U, V>>> Derive(Dictionary<string, List<StockDataSet<U>>> source, StockDataFile file, StockDataCreator create, StockProcessingStateAccessor stateGetter)
        {
            var derived = new Dictionary<string, List<StockDataSetDerived<T, U, V>>>();
            foreach(KeyValuePair<string, List<StockDataSet<U>>> pair in source)
            {
                StockDataSetDerived<T, U, V> prevSet = null;
                derived[pair.Key] = new List<StockDataSetDerived<T, U, V>>(pair.Value.Count);
                foreach(StockDataSet<U> srcSet in pair.Value)
                {
                    var newSet = new StockDataSetDerived<T, U, V>(srcSet, file, create, stateGetter);
                    newSet.Previous = prevSet;
                    prevSet = newSet;
                    derived[pair.Key].Add(newSet);
                }
            }

            return derived;
        }

        /// <summary>
        /// Sets the stock data segments which are present in this file
        /// </summary>
        /// <typeparam name="T">The derived data point type</typeparam>
        /// <typeparam name="U">The base data point type</typeparam>
        /// <typeparam name="V">The processing state type</typeparam>
        /// <param name="segments">The set of segments to specify for this file</param>
        public static Dictionary<string, List<StockDataSet<T>>> CastToBase(Dictionary<string, List<StockDataSetDerived<T, U, V>>> segments)
        {
            Dictionary<string, List<StockDataSet<T>>> castedSeg = segments.Cast<KeyValuePair<string, List<StockDataSetDerived<T, U, V>>>>().ToDictionary(
                (KeyValuePair<string, List<StockDataSetDerived<T, U, V>>> pair) => { return (string)pair.Key; },
                (KeyValuePair<string, List<StockDataSetDerived<T, U, V>>> pair) => { return pair.Value.ConvertAll((x) => { return (StockDataSet<T>)x; }); }
            );
            return castedSeg;
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

        /// <summary>
        /// Gets the processing state to use
        /// </summary>
        StockProcessingStateAccessor GetState;

        /// <summary>
        /// The interval between ticks. Can be overridden to set a decrased interval from the base data set.
        /// </summary>
        public override TimeSpan Interval
        {
            set {
                if (_interval != value)
                {
                    _interval = value;
                    if (DataSet.Count > 0)
                    {
                        DataSet.Clear();
                    }
                }
            }
            get { return (_interval != TimeSpan.Zero) ? _interval : File.Interval; }
        }
        private TimeSpan _interval = TimeSpan.Zero;
        #endregion

        #region Types
        /// <summary>
        /// Callback used to create a stock data instance
        /// </summary>
        public delegate void StockDataCreator(StockDataSetDerived<T, U, V> data, int idx);

        /// <summary>
        /// Callback used to create a stock data instance
        /// </summary>
        public delegate V StockProcessingStateAccessor(StockDataSetInterface data);
        #endregion

        /// <summary>
        /// Loads the data from the source file
        /// <param name="session">The session currently being processed</param>
        /// </summary>
        public override void Load(StockSession session)
        {
            if(!IsReady())
            {
                SourceData.Load(session);
                int endIdx = (int)((SourceData.DataSet.Count * SourceData.Interval.Ticks) / Interval.Ticks);
                DataSet.Resize(endIdx);
                ProcessingState = GetState(this);
                for(int idx = DataSet.Count; idx < endIdx; idx++)
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
        /// <param name="keep">Indicates which data should be kept</param>
        /// </summary>
        public override void Clear(MemoryScheme keep = MemoryScheme.MEM_KEEP_NONE)
        {
            if(keep != MemoryScheme.MEM_KEEP_DERIVED)
            {
                base.Clear();
            }
            SourceData.Clear(keep);
        }

        /// <summary>
        /// Indicates if all of the available data is ready
        /// </summary>
        /// <returns>True if the DataSet contains all available data</returns>
        public override bool IsReady()
        {
            return SourceData.IsReady() && (SourceData.DataSet.Count == GetSourceIndex(DataSet.Count));
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

        /// <summary>
        /// Calculates the source index corresponding to the specified derived index
        /// </summary>
        /// <param name="index">The index into the derived data set</param>
        /// <returns>The corresponding source data set index (will match if the interval is the same)</returns>
        public int GetSourceIndex(int index)
        {
            int srcIndex = index;
            if(Interval != SourceData.Interval)
            {
                srcIndex = (int)((Interval.Ticks * index) / SourceData.Interval.Ticks);
            }
            return srcIndex;
        }

        /// <summary>
        /// Returns the number of points in the source data set
        /// </summary>
        /// <returns>The number of data points</returns>
        public override int GetSourceCount()
        {
            return SourceData.GetSourceCount();
        }
    }
}
