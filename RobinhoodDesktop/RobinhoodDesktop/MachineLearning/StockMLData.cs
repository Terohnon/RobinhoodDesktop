using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobinhoodDesktop.Script;

namespace RobinhoodDesktop.MachineLearning
{
    public class StockMLData
    {
        public class SourceList<T, U> where T : struct, StockData where U : struct
        {
            /// <summary>
            /// Callback to load data from a source
            /// </summary>
            /// <typeparam name="T">The type of object containing the data to be loaded from</typeparam>
            /// <param name="src">The source data object</param>
            /// <param name="features">The list of features being populated</param>
            /// <param name="labels">The list of labels being populated</param>
            /// <param name="sampleIdx">The current index into the list of features/labels at which to populate new data</param>
            /// <param name="numFeatures">The current index into the features (should be incremented as data is added)</param>
            /// <param name="numLabels">The current index into the labels (should be incremented as data is added)</param>
            public delegate void DataLoader(T src, float[,] features, U[,] labels, int sampleIdx, ref int numFeatures, ref int numLabels);

            /// <summary>
            /// The source data that should be loaded from
            /// </summary>
            public List<StockDataSet<T>> Source;

            /// <summary>
            /// The interval at which to sample the source
            /// </summary>
            public TimeSpan Interval;

            /// <summary>
            /// The callback that should be used to load data from this source
            /// </summary>
            public DataLoader Loader;
        }

        /// <summary>
        /// Pulls the processed stock data, and puts it into a format that can be fed into the ML model
        /// </summary>
        /// <param name="session">The session this processing is part of</param>
        /// <param name="sources">The sources to load the data from</param>
        /// <param name="features">The array the features should be placed in</param>
        /// <param name="labels">The array the expected outputs should be placed in</param>
        public static void PopulateData<T, U>(StockSession session, List<List<SourceList<T, U>>> sources, ref float[,] features, ref U[,] labels, MemoryScheme keep = MemoryScheme.MEM_KEEP_NONE) where T : struct, StockData where U : struct
        {
            List<DateTime> pointTimes = new List<DateTime>();
            List<int> skips = new List<int>();
            int numDataPoints = 0;
            int numFeatures = 0;
            int numLabels = 0;
            bool autoDetectFeaturesLabels = false;
            if(features == null)
            {
                features = new float[1, 1024];
                autoDetectFeaturesLabels = true;
            }
            if(labels == null)
            {
                labels = new U[1, 1024];
                autoDetectFeaturesLabels = true;
            }

            // Determine the size of the features and labels
            for(int srcIdx = 0; srcIdx < sources.Count; srcIdx++)
            {
                int maxPoints = 0;
                for(int mergeIdx = 0; mergeIdx < sources[srcIdx].Count; mergeIdx++)
                {
                    int points = 0;
                    int timeIdx = 0;
                    foreach(var s in sources[srcIdx][mergeIdx].Source)
                    {
                        // Ensure the desired interval is set for the source
                        s.Interval = sources[srcIdx][mergeIdx].Interval;

                        // Determine how many data points are in the source
                        int count = s.GetSourceCount();
                        points += count;

                        // Record the point times
                        for(int pnt = 0; pnt < count; pnt++)
                        {
                            DateTime t = (s.Start.AddSeconds(pnt * s.Interval.TotalSeconds));
                            if((timeIdx >= pointTimes.Count) || (t.AddSeconds(s.Interval.TotalSeconds) < pointTimes[timeIdx]))
                            {
                                pointTimes.Insert(timeIdx, t);
                            }
                            while((timeIdx < pointTimes.Count) && (t >= pointTimes[timeIdx])) timeIdx++;
                        }
                    }
                    if(points > maxPoints) maxPoints = points;

                    // Also get the number of features and labels that will be loaded
                    if((srcIdx == 0) && autoDetectFeaturesLabels)
                    {
                        sources[srcIdx][mergeIdx].Source[0].Load(session);
                        sources[srcIdx][mergeIdx].Loader(sources[srcIdx][mergeIdx].Source[0][0], features, labels, 0, ref numFeatures, ref numLabels);
                    }
                }
                numDataPoints += maxPoints;
            }

            // Create the data arrays to output
            if(features.GetLength(0) < numDataPoints) features = new float[numDataPoints, numFeatures];
            if(labels.GetLength(0) < numDataPoints) labels = new U[numDataPoints, numLabels];

            // Load the data
            numFeatures = 0;
            numLabels = 0;
            for(int srcIdx = 0; srcIdx < sources.Count; srcIdx++)
            {
                for(int mergeIdx = 0; mergeIdx < sources[srcIdx].Count; mergeIdx++)
                {
                    int mergeFeatures = numFeatures;
                    int mergeLabels = numLabels;
                    int timeIdx = 0;

                    // Load all of the data first
                    foreach(var s in sources[srcIdx][mergeIdx].Source)
                    {
                        // Load the source data
                        s.Load(session);
                    }

                    // Add the data to the features/labels
                    foreach(var s in sources[srcIdx][mergeIdx].Source)
                    {
                        for(int pnt = 0; pnt < s.Count; pnt++)
                        {
                            // Add the data multiple times if it has a slower interval
                            for(DateTime t = s.Time(pnt + 1); (timeIdx < pointTimes.Count) && (t > pointTimes[timeIdx]); timeIdx++)
                            {
                                mergeFeatures = numFeatures;
                                mergeLabels = numLabels;
                                sources[srcIdx][mergeIdx].Loader(s[pnt], features, labels, timeIdx, ref mergeFeatures, ref mergeLabels);

                                // Check if any values are invalid
                                for(int i = numFeatures; i < mergeFeatures; i++)
                                {
                                    if(float.IsNaN(features[timeIdx, i]))
                                    {
                                        if(!skips.Contains(timeIdx)) skips.Add(timeIdx);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Clear the data if desired
                    foreach(var s in sources[srcIdx][mergeIdx].Source)
                    {
                        s.Clear(keep);
                    }

                    // Advance to the next merge location in the features/labels arrays
                    numFeatures = mergeFeatures;
                    numLabels = mergeLabels;
                }
            }

            // Remove the invalid data
            if(skips.Count > 0)
            {
                skips.Sort();
                int skipped = 0;
                int src = skips[0];
                int dst;
                var oldFeatures = features;
                var oldLabels = labels;
                features = new float[numDataPoints - skips.Count, numFeatures];
                labels = new U[numDataPoints - skips.Count, numLabels];
                for(dst = skips[0]; src < numDataPoints; dst++, src++)
                {
                    // Check if the next copy location is skipped
                    while((src < numDataPoints) && (skipped  < skips.Count) && (src == skips[skipped]))
                    {
                        skipped++;
                        src++;
                    }

                    // Copy the source to the destination to remove any skipped entries
                    Array.Copy(oldFeatures, (src * numFeatures), features, (dst * numFeatures), numFeatures);
                    Array.Copy(oldLabels, (src * numLabels), labels, (dst * numLabels), numLabels);
                }
            }
        }

        public static List<List<StockMLData.SourceList<T, W>>> GenerateConfig<T, U, V, W>(StockSession session, List<string> symbols, List<Tuple<TimeSpan, StockMLData.SourceList<T, W>.DataLoader>> intervals, StockDataSetDerived<T, U, V>.StockDataCreator creator, StockDataSetDerived<T, U, V>.StockProcessingStateAccessor getState)
                where T : struct, StockData where U : struct, StockData where W : struct
        {
            var dataSets = new List<Dictionary<string, List<StockDataSet<T>>>>();
            foreach (var i in intervals)
            {
                var set = StockDataSetDerived<T, U, V>.Derive(session.SourceFile.GetSegments<U>(), session.SinkFile, creator, getState);
                dataSets.Add(StockDataSetDerived<T, U, V>.CastToBase(set));
            }
            var sources = new List<List<StockMLData.SourceList<T, W>>>();
            foreach (var s in symbols)
            {
                var l = new List<StockMLData.SourceList<T, W>>();
                for (int i = 0; i < intervals.Count; i++)
                {
                    l.Add(new StockMLData.SourceList<T, W>()
                    {
                        Source = dataSets[i][s],
                        Interval = intervals[i].Item1,
                        Loader = intervals[i].Item2
                    });
                }
                sources.Add(l);
            }

            return sources;
        }
    }
}
