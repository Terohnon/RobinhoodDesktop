using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RobinhoodDesktop.MachineLearning;

namespace RobinhoodDesktop.Script
{
    /// <summary>
    /// Customize the default StockML class here. Could specify a different model architecture or data processing.
    /// </summary>
    class MLInstance : StockML
    {

    }

    class StockSessionMLScript
    {
        /// <summary>
        /// Pulls the processed stock data, and puts it into a format that can be fed into the ML model
        /// </summary>
        /// <param name="src">The processed stock data</param>
        /// <param name="dst">The array the features should be placed in</param>
        /// <param name="labels">The array the expected outputs should be placed in</param>
        /// <param name="dstIdx">Update the current postion within the arrays</param>
        /// <param name="numFeatures">Output the number of features</param>
        /// <param name="numLabels">Output the number of data labels</param>
        public static void PopulateData(StockDataSet<StockDataSink> src, float[,] dst, int[,] labels, ref int dstIdx, out int numFeatures, out int numLabels)
        {
            numFeatures = 0;
            numLabels = 0;
            for(int srcIdx = 0; (srcIdx < src.Count) && (dstIdx < dst.GetLength(0)); srcIdx++, dstIdx++)
            {
                // Output features for the data point
                numFeatures = 0;

                StockDataSink s = src[srcIdx];
                dst[dstIdx, numFeatures++] = s.Trend;

                // Output the labels for the data point
                numLabels = 0;

                var timeDays = (s.ChangeTimes[0].TotalHours / 6.5);
                labels[dstIdx, numLabels++] = ((timeDays > 0) && (timeDays < 12)) ? 1 : 0;
            }
        }

        /// <summary>
        /// Executes a run of processing the stock data
        /// </summary>
        /// <param name="session">The session configuration</param>
        public static void Run(StockSession session)
        {
            // Create the stock processor
            StockProcessor processor = new StockProcessor(session);

            // Get the sizes of the features and labels
            int dstIdx = 0;
            int numDataPoints = 0;
            int numFeatures, numLabels;
            float[,] tmpDst = new float[1, 1024];
            int[,] tmpLabels = new int[1, 1];
            var tmpDataPoint = processor.DerivedData.First().Value[0];
            tmpDataPoint.Load(session);
            PopulateData(tmpDataPoint, tmpDst, tmpLabels, ref dstIdx, out numFeatures, out numLabels);

            // Determine the total number of data points
            List<string> symbols = new List<string>() { "GNTX" };
            foreach(var s in symbols)
            {
                List<StockDataSet<StockDataSink>> sources;
                if(!processor.DerivedData.TryGetValue(s, out sources)) continue;
                for(int i = 0; i < sources.Count; i++)
                {
                    sources[i].Load(session);
                    numDataPoints += sources[i].Count;
                }
            }

            // Allocate the feature and label arrays
            float[,] features = new float[numDataPoints, numFeatures];
            int[,] labels = new int[numDataPoints, numLabels];

            // Load the data
            dstIdx = 0;
            foreach(var s in symbols)
            {
                List<StockDataSet<StockDataSink>> sources;
                if(!processor.DerivedData.TryGetValue(s, out sources)) continue;

                // Create a table of each data point in the specified range
                for(int i = 0; i < sources.Count; i++)
                {
                    sources[i].Load(session);
                    PopulateData(sources[i], features, labels, ref dstIdx, out numFeatures, out numLabels);
                }
            }

            // Create the Machine Learning instance
            MLInstance ml = new MLInstance();
            ml.BuildFullyConnectedGraph(new int[] { numFeatures, numFeatures, numLabels });
            ml.PrepareData(features, labels);
            ml.Train();
        }
    }
}
