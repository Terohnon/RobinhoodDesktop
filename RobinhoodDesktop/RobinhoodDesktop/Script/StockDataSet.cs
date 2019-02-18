using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RobinhoodDesktop.Script
{
    public class StockDataSet<T> where T : struct, StockData
    {
        public StockDataSet(string symbol, DateTime start, StockDataFile file, long address = -1)
        {
            this.Symbol = symbol;
            this.Start = start;
            this.File = file;
            this.StreamAddress = address;
        }

        protected StockDataSet()
        {

        }

        #region Types
        public class StockDataArray
        {
            T[] m_array;
            int m_count;

            public StockDataArray()
            {
                m_count = 0;
            }

            public T[] InternalArray { get { return m_array; } }

            public int Count { get { return m_count; } }

            public void Initialize(T[] data)
            {
                m_array = data;
                m_count = data.Length;
            }

            public void Resize(int capacity)
            {
                if(m_array == null)
                {
                    m_array = new T[capacity];
                }
                else if(m_array.Length != capacity)
                {
                    Array.Resize(ref m_array, capacity);
                }
            }

            public void Add(T element)
            {
                if(m_count == m_array.Length)
                {
                    Array.Resize(ref m_array, m_array.Length * 2);
                }

                m_array[m_count++] = element;
            }

            public void Clear()
            {
                m_array = null;
                m_count = 0;
            }
        }
        #endregion

        #region Variables
        public string Symbol;
        public DateTime Start;
        public StockDataFile File;
        public long StreamAddress;
        public DateTime End
        {
            get { return Start.AddTicks(Interval.Ticks * DataSet.Count); }
        }
        public TimeSpan Interval
        {
            get { return File.Interval; }
        }
        public readonly StockDataArray DataSet = new StockDataArray();
        #endregion

        /// <summary>
        /// Allows bracket operator to be used on the data set
        /// </summary>
        /// <param name="i">The index to access</param>
        /// <returns>The specified item in the data set</returns>
        public T this[int i]
        {
            get { return DataSet.InternalArray[i]; }
        }

        /// <summary>
        /// Returns the time corresponding to the specified data point
        /// </summary>
        /// <param name="i">The data point index</param>
        /// <returns>The time that data point was recorded</returns>
        public DateTime Time(int i)
        {
            return Start.AddTicks(Interval.Ticks * i);
        }

        /// <summary>
        /// The number of points in the data set
        /// </summary>
        public int Count
        {
            get { return DataSet.Count; }
        }

        /// <summary>
        /// Indicates if the data set has valid data
        /// </summary>
        /// <returns>True if data is available</returns>
        public virtual bool IsReady()
        {
            return (DataSet.Count > 0);
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
