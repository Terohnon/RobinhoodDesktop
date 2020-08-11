using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public partial struct StockDataSink
    {
        /// <summary>
        /// The average over 10 minutes
        /// </summary>
        public float Average10Min;

        /// <summary>
        /// The main update function for the class
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        partial void MovingAverage_Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex)
        {
            this.Average10Min = 0.0f;
            int startIdx = Math.Max(0, updateIndex - 9);
            for(int idx = startIdx; idx <= updateIndex; idx++)
            {
                this.Average10Min += data[idx].Price;
            }
            this.Average10Min /= ((updateIndex + 1) - startIdx);
        }
    }
}
