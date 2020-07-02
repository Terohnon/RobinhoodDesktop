using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSScriptLibrary;

namespace RobinhoodDesktop.Script
{
    public interface StockDataInterface
    {
        /// <summary>
        /// Outputs information about the data set
        /// </summary>
        /// <param name="symbol">The stock symbol the dataset is for</param>
        /// <param name="start">The start time of the dataset</param>
        /// <param name="interval">The interval between data points in the set</param>
        void GetInfo(out string symbol, out DateTime start, out TimeSpan interval);

        /// <summary>
        /// Sets the interval between points for the data set
        /// </summary>
        /// <param name="interval">The interval to set</param>
        void SetInterval(TimeSpan interval);

        /// <summary>
        /// Loads the data from the source file
        /// <param name="session">The session currently being processed</param>
        /// </summary>
        void Load(StockSession session);

        /// <summary>
        /// Returns the type held in the stock data set
        /// </summary>
        /// <returns>The data type</returns>
        Type GetDataType();

        /// <summary>
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <param name="expression">The expression to get a value from the dataset</param>
        /// <returns>The delegate used to get the desired value from a dataset</returns>
        Func<StockDataInterface, int, object> GetExpressionEvaluator(string expression);

        /// <summary>
        /// Returns the number of points in the dataset
        /// </summary>
        /// <returns>The number of points in the set</returns>
        int GetCount();
    }
}
