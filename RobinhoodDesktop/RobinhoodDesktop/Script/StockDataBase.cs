using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RobinhoodDesktop.Script
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct StockDataBase : StockData
    {
        public StockDataBase(float price)
        {
            this.Price = price;
        }

        #region Variables
        /// <summary>
        /// The price the stock was last traded at during this time
        /// </summary>
        public float Price;
        #endregion

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="dataSource">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        public void Update<T>(StockDataSet<T>.StockDataArray dataSource, int updateIndex) where T : struct, StockData
        {
            var data = dataSource as StockDataSet<StockDataBase>.StockDataArray;
            if(data != null)
            {
                this.Price = data.InternalArray[updateIndex].Price;
            }
        }

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        public void Update(StockDataSet<StockDataBase>.StockDataArray data, int updateIndex)
        {
            this.Price = data.InternalArray[updateIndex].Price;
        }
    }
}
