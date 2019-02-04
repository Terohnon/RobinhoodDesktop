using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RobinhoodDesktop.Script
{
    public class StockDataSet<T> where T : StockData
    {
        public StockDataSet(string symbol, DateTime start, StockDataFile file, long address = -1)
        {
            this.Symbol = symbol;
            this.Start = start;
            this.File = file;
            this.StreamAddress = address;
            this.DataSet = new List<T>();
        }

        protected StockDataSet()
        {

        }

        #region Variables
        public string Symbol;
        public DateTime Start;
        public StockDataFile File;
        public long StreamAddress;
        public DateTime End
        {
            get { return (DataSet != null) ? Start.AddTicks(Interval.Ticks * DataSet.Count) : Start; }
        }
        public TimeSpan Interval
        {
            get { return File.Interval; }
        }
        public List<T> DataSet;
        #endregion

        /// <summary>
        /// Indicates if the data set has valid data
        /// </summary>
        /// <returns>True if data is available</returns>
        public virtual bool IsReady()
        {
            return (DataSet != null) && (DataSet.Count > 0);
        }

        /// <summary>
        /// Loads the data from the source file
        /// </summary>
        public virtual void Load()
        {
            if(!IsReady())
            {
                File.LoadSegment(this);
            }
        }

        /// <summary>
        /// Releases the reference to the data to free up memory
        /// </summary>
        public virtual void Clear()
        {
            this.DataSet.Clear();
        }
    }
}
