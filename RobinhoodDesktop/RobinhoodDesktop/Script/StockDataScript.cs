using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RobinhoodDesktop.Script
{
    public partial struct StockDataSource : StockData
    {

    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct StockDataScript : StockData
    {
        #region Script Interface
        /// <summary>
        /// Creates a new instance of a stock data point from only a price
        /// <param name="price"> The price to use</param>
        /// </summary>
        public static StockDataScript CreateFromPrice(float price)
        {
            var data = new StockDataScript();
            data.Price = price;
            return data;
        }
        #endregion

        #region Variables
        /// <summary>
        /// The price the stock was last traded at during this time
        /// </summary>
        public float Price;
        #endregion

        #region Prototypes
        ///= PartialPrototypes ///
        #endregion

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="dataSource">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        public void Update<T>(StockDataSet<T>.StockDataArray dataSource, int updateIndex) where T : struct, StockData
        {
            var data = dataSource as StockDataSet<StockDataSource>.StockDataArray;
            if(data != null)
            {
                this.Price = data.InternalArray[updateIndex].Price;

                ///= PartialUpdates ///
            }
        }
    }
}
