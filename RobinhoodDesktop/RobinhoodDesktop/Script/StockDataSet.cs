using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using CSScriptLibrary;

namespace RobinhoodDesktop.Script
{

    public class StockDataSet<T> : StockDataInterface where T : struct, StockData
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
                if(m_array == null)
                {
                    m_array = new T[1];
                    m_count = 0;
                }
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

        /// <summary>
        /// The data currently being processed
        /// </summary>
        [ThreadStatic]
        public static List<StockDataSet<T>> ProcessingData;
        public DateTime End
        {
            get { return Start.AddTicks(Interval.Ticks * DataSet.Count); }
        }
        public virtual TimeSpan Interval
        {
            set {  }
            get { return File.Interval; }
        }

        /// <summary>
        /// The array holding the stock data points
        /// </summary>
        public readonly StockDataArray DataSet = new StockDataArray();

        /// <summary>
        /// Returns the last item in the data set
        /// </summary>
        public T Last
        {
            get { return DataSet.InternalArray[(DataSet.Count > 0) ? DataSet.Count - 1 : 0]; }
        }

        /// <summary>
        /// The previous data set in the series (allows datasets to reference back across gaps in the time sequence)
        /// </summary>
        public StockDataSet<T> Previous;

        /// <summary>
        /// Outputs information about the data set
        /// </summary>
        /// <param name="symbol">The stock symbol the dataset is for</param>
        /// <param name="start">The start time of the dataset</param>
        /// <param name="interval">The interval between data points in the set</param>
        public void GetInfo(out string symbol, out DateTime start, out TimeSpan interval)
        {
            symbol = this.Symbol;
            start = this.Start;
            interval = this.Interval;
        }

        /// <summary>
        /// Sets the interval between points for the data set
        /// </summary>
        /// <param name="interval">The interval to set</param>
        public void SetInterval(TimeSpan interval)
        {
            this.Interval = interval;
        }
        #endregion

        /// <summary>
        /// Allows bracket operator to be used on the data set
        /// </summary>
        /// <param name="i">The index to access</param>
        /// <returns>The specified item in the data set</returns>
        public T this[int i]
        {
            get {
                var set = this;
                while((i < 0) && (set.Previous != null))
                {
                    set = set.Previous;
                    i += set.Count;
                }
                return set.DataSet.InternalArray[(i >= 0) ? i : 0];
            }
        }

        /// <summary>
        /// Limits the specified index to the available data range
        /// </summary>
        /// <param name="index">The index to limit</param>
        /// <returns>The limited index</returns>
        public virtual int LimitIndex(int index)
        {
            int minIdx = 0;
            var prevSet = Previous;
            while((minIdx > index) && (prevSet != null))
            {
                minIdx -= prevSet.Count;
                prevSet = prevSet.Previous;
            }
            return (index >= minIdx) ? index : minIdx;
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
        /// <param name="session">The session currently being processed</param>
        /// </summary>
        public virtual void Load(StockSession session)
        {
            if(!IsReady())
            {
                File.LoadSegment(this, session);
            }
        }

        /// <summary>
        /// Releases the reference to the data to free up memory
        /// </summary>
        public virtual void Clear(MemoryScheme keep = MemoryScheme.MEM_KEEP_NONE)
        {
            if(keep == MemoryScheme.MEM_KEEP_NONE)
            {
                this.DataSet.Clear();
            }
        }

        /// <summary>
        /// Returns the number of points in the source data set
        /// </summary>
        /// <returns>The number of data points</returns>
        public virtual int GetSourceCount()
        {
            return File.GetSegmentSize(this);
        }

        /// <summary>
        /// Returns the type held in the stock data set
        /// </summary>
        /// <returns>The data type</returns>
        public virtual Type GetDataType()
        {
            return typeof(T);
        }

        /// <summary>
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <param name="expression">The expression to get a value from the dataset</param>
        /// <returns>The delegate used to get the desired value from a dataset</returns>
        public Func<StockDataInterface, int, object> GetExpressionEvaluator(string expression, StockSession session)
        {
            Func<StockDataSet<T>, int, object> accessor = null;

            // Check for the special case of requesting the time
            if (expression.Equals("Time"))
            {
                accessor = new Func<StockDataSet<T>, int, object>((data, index) =>
                {
                    return data.Time(index);
                });
            }
            else
            {
                // Order the list based on the lame length
                Type dataType = GetDataType();
                var fields = new List<string>();
                fields.AddRange(typeof(T).GetFields().ToList().ConvertAll((f) => { return f.Name; }));
                fields.AddRange(typeof(T).GetProperties().ToList().ConvertAll((f) => { return f.Name; }));
                fields.AddRange(typeof(T).GetMethods().ToList().ConvertAll((f) => { return f.Name; }));
                fields.Sort((f1, f2) => { return f2.Length.CompareTo(f1.Length); });

                // First remove any string literals
                string src = expression;
                List<string> stringLiterals = new List<string>();
                for (int i = src.IndexOf('"'); (i >= 0) && (i < src.Length); i = src.IndexOf('"'))
                {
                    int end = src.IndexOf('"', i + 1);
                    if ((end >= 0) && (end < src.Length))
                    {
                        stringLiterals.Add(src.Substring(i, (end - i) + 1));
                        src = src.Replace(stringLiterals.Last(), string.Format("<=s{0}>", stringLiterals.Count - 1));
                    }
                }

                // First replace the fields with an index to prevent names within a name from getting messed up
                for (int i = 0; i < fields.Count; i++)
                {
                    src = src.Replace(fields[i], string.Format("<={0}>", i));
                }

                // Next pre-pend the data set to the field names
                for (int i = 0; i < fields.Count; i++)
                {
                    src = src.Replace(string.Format("<={0}>", i), string.Format("data[updateIndex].{0}", fields[i]));
                }

                // Restore the string literals
                for (int i = 0; i < stringLiterals.Count; i++)
                {
                    src = src.Replace(string.Format("<=s{0}>", i), stringLiterals[i]);
                }

                // Build the expression into an accessor function
                //src = "namespace RobinhoodDesktop.Script { public class ExpressionAccessor{ public static object GetValue(StockDataSet<" + typeof(T).Name + "> data, int updateIndex) { return " + src + ";} } }";
                var compiler = CSScript.MonoEvaluator.ReferenceAssemblyOf<T>();
                compiler = compiler.ReferenceAssembly(session.ScriptInstance.Location);
                //var script = CSScript.LoadCode(src);
                //CSScript.Evaluator.
                //accessor = script.GetStaticMethod("*.*");
                accessor = compiler.LoadDelegate<Func<StockDataSet<T>, int, object>>(@"object GetValue(RobinhoodDesktop.Script.StockDataSet<" + typeof(T).FullName + "> data, int updateIndex) { return " + src + ";}");
            }
            return new Func<StockDataInterface, int, object>((data, index) => { return accessor((StockDataSet<T>)data, index); });
        }

        /// <summary>
        /// Returns the number of points in the dataset
        /// </summary>
        /// <returns>The number of points in the set</returns>
        public int GetCount()
        {
            return Count;
        }
    }
}
