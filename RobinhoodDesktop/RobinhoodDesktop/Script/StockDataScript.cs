using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public partial struct StockDataSource : StockData
    {

    }

    [Serializable]
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

        /// <summary>
        /// Creates a new instance of a stock data point from only a price
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        /// </summary>
        public static StockDataScript Create(List<StockDataSource> data, int updateIndex)
        {
            var newPoint = new StockDataScript();
            newPoint.Update(data, updateIndex);
            return newPoint;
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
        public void Update<T>(List<T> dataSource, int updateIndex) where T : StockData
        {
            var data = dataSource as List<StockDataSource>;
            if(data != null)
            {
                this.Price = data[updateIndex].Price;

                ///= PartialUpdates ///
            }
        }
    }
}
