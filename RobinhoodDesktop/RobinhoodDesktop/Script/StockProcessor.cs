using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public partial class StockProcessingState
    {
        /// <summary>
        /// Stores the time corresponding to the start time of the last dataset that was processed
        /// </summary>
        public DateTime LastProcessedStartTime;

        /// <summary>
        /// The data set being processed
        /// </summary>
        public List<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>> DataSet;

        /// <summary>
        /// The index of the data set currently being processed
        /// </summary>
        public int DataSetIndex = 0;

        /// <summary>
        /// The index of the data point currently being processed
        /// </summary>
        public int DataPointIndex = 0;
    }

    [Serializable]
    public class StockProcessor
    {
        /// <summary>
        /// The created stock-processor
        /// </summary>
        private static StockProcessor Instance;

        /// <summary>
        /// Returns the stock processor instance, or creates one if there isn't one already
        /// </summary>
        /// <param name="session">The stock session to create the processor for</param>
        /// <returns>The stock processor instance</returns>
        public static StockProcessor GetInstance(StockSession session = null)
        {
            if(Instance == null)
            {
                if(session != null)
                {
                    Instance = new StockProcessor(session);
                }
                else
                {
                    throw new Exception("Must specify the session the first time the processor instance is accessed.");
                }
            }

            return Instance;
        }

        public StockProcessor(StockSession session)
        {
            this.Session = session;

            // Load the source file static information
            Session.SourceFile.LoadStaticData(session);

            // Create the derived data set
            HistoricalData = StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>.Derive(Session.SourceFile.GetSegments<StockDataSource>(), Session.SinkFile, CreateSink, GetProcessingState);
            DerivedData = StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>.CastToInterface(HistoricalData);
            Session.Data = DerivedData;
            Session.SinkFile.SetSegments<StockDataSink>(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>.CastToBase(HistoricalData));
        }

        #region Types
        [Serializable]
        public class ProcessingTarget
        {
            public string Symbol;
            public DateTime ReferenceTime;
            public StockDataSink Reference;

            public int ProcessedSetIdx;
            public int ProcessedDataIdx;
            public int ProcessedLiveIdx;

            public ProcessingTarget(string symbol, DateTime referenceTime, StockDataSink reference)
            {
                this.Symbol = symbol;
                this.ReferenceTime = referenceTime;
                this.Reference = reference;
                this.ProcessedSetIdx = 0;
                this.ProcessedDataIdx = 0;
                this.ProcessedLiveIdx = 0;
            }

            public ProcessingTarget(string symbol)
            {
                this.Symbol = symbol;
                this.ReferenceTime = DateTime.Now;
                this.Reference = new StockDataSink();
                this.ProcessedSetIdx = 0;
                this.ProcessedDataIdx = 0;
                this.ProcessedLiveIdx = 0;
            }
        }

        #endregion

        #region Variables
        /// <summary>
        /// The list of stocks that will be processed
        /// </summary>
        public List<ProcessingTarget> Targets = new List<ProcessingTarget>();

        /// <summary>
        /// The historical data that can be processed
        /// </summary>
        [NonSerialized]
        public Dictionary<string, List<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>>> HistoricalData;

        /// <summary>
        /// The historical data, cast to a simple data set container
        /// </summary>
        [NonSerialized]
        public Dictionary<string, List<StockDataInterface>> DerivedData;

        /// <summary>
        /// Indicates if the processor should operate on live data
        /// </summary>
        public bool Live = false;

        /// <summary>
        /// The analyzer(s) that should be used
        /// </summary>
        public StockEvaluator Evaluator;

        /// <summary>
        /// The action that should be executed when a target evaluates as true 
        /// </summary>
        public StockAction Action;

        /// <summary>
        /// Delegate executed when the processor has completed
        /// </summary>
        public Action Complete;

        /// <summary>
        /// The interval between samples when processing live data
        /// </summary>
        private TimeSpan LiveInterval;

        /// <summary>
        /// Stores live data as it is received
        /// </summary>
        [NonSerialized]
        public Dictionary<string, Tuple<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>, DataAccessor.Subscription>> LiveData;

        /// <summary>
        /// Queue used to indicate which targets have data available
        /// </summary>
        private System.Collections.Concurrent.BlockingCollection<ProcessingTarget> LiveProcessingQueue;

        /// <summary>
        /// The thread used to process the live data
        /// </summary>
        [NonSerialized]
        private System.Threading.Thread LiveThread;

        /// <summary>
        /// The session this is part of
        /// </summary>
        public StockSession Session;

        /// <summary>
        /// Used to look up the appropriate processing state
        /// </summary>
        [NonSerialized]
        private Dictionary<string, Dictionary<TimeSpan, StockProcessingState>> ProcessingStates = new Dictionary<string, Dictionary<TimeSpan, StockProcessingState>>();
        #endregion

        /// <summary>
        /// Processes the specified data using the evaluator and resulting action
        /// <param name="multithreaded">Indicates if the processing should be done multithreaded</param>
        /// <param name="keep">Indicates if the processed data should remain in memory,
        ///                                     or should be cleared once processing is complete.</param>
        /// </summary>
        public virtual void Process(bool multithreaded = false, MemoryScheme keep = MemoryScheme.MEM_KEEP_NONE)
        {
            System.Action<ProcessingTarget> processFunc = (ProcessingTarget target) =>
            {
                List<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>> sets = HistoricalData[target.Symbol];
                var processingState = new StockProcessingState();
                for(; target.ProcessedSetIdx < sets.Count; target.ProcessedSetIdx++)
                {
                    // Process all of the data sets for the target
                    StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> set = sets[target.ProcessedSetIdx];
                    set.Load(Session);
                    for(; (target.ProcessedDataIdx < set.Count); target.ProcessedDataIdx++)
                    {
                        if(Evaluator.Evaluate(set, target.ProcessedDataIdx, target))
                        {
                            Action.Do(this, target, set.Time(target.ProcessedDataIdx));
                        }
                    }

                    // Clean up the memory after processing has completed
                    if(keep == MemoryScheme.MEM_KEEP_NONE) set.Clear();
                    else if(keep == MemoryScheme.MEM_KEEP_SOURCE) set.ClearDerived();
                }
            };

            // Process each of the targets
            if(multithreaded)
            {
                System.Threading.Tasks.Parallel.ForEach(Targets, processFunc);
            }
            else
            {
                foreach(ProcessingTarget target in Targets)
                {
                    processFunc(target);
                }
            }

            // Indicate processing has completed
            if(Complete != null) Complete();
        }

        /// <summary>
        /// Adds a new processing target
        /// </summary>
        /// <param name="target">The target point to add</param>
        public void Add(ProcessingTarget target)
        {
            Targets.Add(target);

            // If live, set up a subscription
            if(Live && !LiveData.ContainsKey(target.Symbol))
            {
                var sourceList = new StockDataSet<StockDataSource>(target.Symbol, DateTime.Now, Session.SourceFile);
                var data = new StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>(sourceList, Session.SinkFile, CreateSink, GetProcessingState);
                var sub = DataAccessor.Subscribe(target.Symbol, LiveInterval);
                sub.Notify += (DataAccessor.Subscription s) =>
                {
                    LiveData[s.Symbol].Item1.Add(StockDataSource.CreateFromPrice((float)s.Price));
                    LiveProcessingQueue.Add(target);
                };

                LiveData[target.Symbol] = new Tuple<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>, DataAccessor.Subscription>(data, sub);
                DerivedData[target.Symbol].Add((StockDataSet<StockDataSink>)data);
            }
        }

        /// <summary>
        /// Removes a target from the processor
        /// </summary>
        /// <param name="target">The target to remove</param>
        public void Remove(ProcessingTarget target)
        {
            bool lastEntryForSymbol = true;
            foreach(var t in Targets)
            {
                if(t.Symbol.Equals(target.Symbol))
                {
                    lastEntryForSymbol = false;
                    break;
                }
            }

            if(lastEntryForSymbol)
            {
                // Un-subscribe if this is the last instance of that symbol
                DataAccessor.Unsubscribe(LiveData[target.Symbol].Item2);
                LiveData.Remove(target.Symbol);
            }

            Targets.Remove(target);
        }

        /// <summary>
        /// Instructs the processor to access live stock data
        /// </summary>
        /// <param name="liveInterval">The interval at which to access the live data</param>
        public void SetLive(TimeSpan? liveInterval = null)
        {
            this.LiveData = new Dictionary<string, Tuple<StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState>, DataAccessor.Subscription>>();
            this.DerivedData = new Dictionary<string, List<StockDataInterface>>();
            this.LiveInterval = ((liveInterval != null) ? liveInterval.Value : new TimeSpan(0, 0, 1));
            this.Live = true;

            this.LiveProcessingQueue = new System.Collections.Concurrent.BlockingCollection<ProcessingTarget>();
            this.LiveThread = new System.Threading.Thread(ProcessLive);
            this.LiveThread.Start();
        }

        /// <summary>
        /// Ends any ongoing processing and closes the processor
        /// </summary>
        public void Close()
        {
            if(LiveProcessingQueue != null)
            {
                LiveProcessingQueue.CompleteAdding();
            }
        }

        /// <summary>
        /// Task for processing live data
        /// </summary>
        private void ProcessLive()
        {
            foreach(var target in LiveProcessingQueue.GetConsumingEnumerable())
            {
                StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> set = LiveData[target.Symbol].Item1;
                if(target.Reference.Price == 0) target.Reference = set[0];

                for(; (target.ProcessedLiveIdx < set.Count); target.ProcessedLiveIdx++)
                {
                    if(Evaluator.Evaluate(set, target.ProcessedLiveIdx, target))
                    {
                        Action.Do(this, target, set.Time(target.ProcessedLiveIdx));
                    }
                }
            }
        }

        #region Utilities
        /// <summary>
        /// Creates a new instance of a StockDataSink based on a StockDataSource
        /// </summary>
        /// <param name="data">Source data</param>
        /// <param name="idx">Index in the source data to base the new point off of</param>
        public static void CreateSink(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int idx)
        {
            data.ProcessingState.DataPointIndex = idx;
            data.DataSet.InternalArray[idx].Update(data, idx);
        }

        /// <summary>
        /// Callback used to create a stock data instance
        /// </summary>
        public StockProcessingState GetProcessingState(StockDataInterface data)
        {
            Dictionary<TimeSpan, StockProcessingState> intervals;
            StockProcessingState state;
            string symbol;
            DateTime start;
            TimeSpan interval;
            data.GetInfo(out symbol, out start, out interval);
            if(!ProcessingStates.TryGetValue(symbol, out intervals) || (intervals == null))
            {
                state = new StockProcessingState();
                state.DataSet = HistoricalData[symbol];
                ProcessingStates[symbol] = new Dictionary<TimeSpan, StockProcessingState>() { { interval, state } };
            }
            else
            {
                if(!intervals.TryGetValue(interval, out state) || (state == null))
                {
                    state = new StockProcessingState();
                    state.DataSet = HistoricalData[symbol];
                    intervals[interval] = state;
                }
                else
                {
                    // Assume incrementing to the next data set
                    state.DataSetIndex++;

                    // Check if processing restarted
                    if(start < state.LastProcessedStartTime)
                    {
                        state = new StockProcessingState();
                        state.DataSet = HistoricalData[symbol];
                        intervals[interval] = state;
                    }
                }
            }
            state.LastProcessedStartTime = start;

            // Ensure the data set index is correct (should never evaluate to true)
            if(data != HistoricalData[symbol][state.DataSetIndex])
            {
                throw new Exception("Data set index mismatch");
            }

            return state;
        }

        /// <summary>
        /// Creates a new chart of the data loaded into the processor
        /// </summary>
        /// <returns>The data chart</returns>
        public DataChartGui CreateChart()
        {
            return new DataChartGui(DerivedData, Session);
        }
        #endregion
    }
}