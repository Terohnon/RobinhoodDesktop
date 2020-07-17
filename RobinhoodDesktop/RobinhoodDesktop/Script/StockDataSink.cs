using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.CSharp;

namespace RobinhoodDesktop.Script
{
    public partial struct StockDataSource : StockData
    {

    }

    public partial struct StockDataSink : StockData
    {

    }

    public partial class StockProcessingState
    {

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct StockDataSink : StockData
    {
        #region Script Interface
        /// <summary>
        /// Creates a new instance of a stock data point from only a price
        /// <param name="price"> The price to use</param>
        /// </summary>
        public static StockDataSink CreateFromPrice(float price)
        {
            var data = new StockDataSink();
            data.Price = price;
            return data;
        }
        #endregion

        #region Variables
        /// <summary>
        /// The price the stock was last traded at during this time
        /// </summary>
        public float Price;

        ///= Members ///
        #endregion

        #region Prototypes
        ///= PartialPrototypes ///
        #endregion

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        public void Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex)
        {
            this.Price = data.SourceData.DataSet.InternalArray[data.GetSourceIndex(updateIndex)].Price;

            ///= PartialUpdates ///
        }

        /// <summary>
        /// Stores any extra metadata into the file
        /// </summary>
        /// <param name="file">The file to save the data to</param>
        public static void Save(System.IO.Stream file)
        {
            ///= PartialSaves ///
        }

        /// <summary>
        /// Loads any extra metadata from the file
        /// </summary>
        /// <param name="file">The file to load from</param>
        public static void Load(System.IO.Stream file)
        {
            ///= PartialLoads ///
        }
    }
}
