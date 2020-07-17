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
        /// The data set this point is part of
        /// </summary>
        [NonSerialized]
        public StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> DataSet;

        /// <summary>
        /// The index in the data set
        /// </summary>
        [NonSerialized]
        public int DataIndex;

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        partial void DataSetReference_Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex)
        {
            this.DataSet = data;
            this.DataIndex = updateIndex;
        }
    }
}
