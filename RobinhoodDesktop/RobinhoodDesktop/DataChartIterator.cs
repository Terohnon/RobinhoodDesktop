using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RobinhoodDesktop.Script;

namespace RobinhoodDesktop
{
    public struct DataChartIterator : StockData
    {
        /// <summary>
        /// The value of the iterator
        /// </summary>
        public float value;

        private DataChartIterator(float val)
        {
            this.value = val;
        }

        /// <summary>
        /// Iterates over a range of values
        /// </summary>
        /// <param name="startVal">The starting value</param>
        /// <param name="endVal">The ending value</param>
        /// <param name="numSteps">The number of steps in the iteration</param>
        /// <returns>The dataset containing the specified iteration</returns>
        public static List<StockDataInterface> Iterate(float startVal, float endVal, int numSteps)
        {
            StockDataSet<DataChartIterator> iterator = new StockDataSet<DataChartIterator>("", DateTime.Now, null);
            float step = (numSteps >= 2) ? (endVal - startVal) / (numSteps - 1) : 0.0f;
            for(int i = 0; i < numSteps; i++)
            {
                iterator.DataSet.Add(new DataChartIterator(startVal + (step * i)));
            }

            return new List<StockDataInterface>() { iterator };
        }

        /// <summary>
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <returns>The delegate used to get the desired value from a dataset</returns>
        public static Func<StockDataInterface, int, object> GetExpressionEvaluator()
        {
            return new Func<StockDataInterface, int, object>((data, index) =>
            {
                return ((StockDataSet<DataChartIterator>)data)[index].value;
            });
        }
    }
}
