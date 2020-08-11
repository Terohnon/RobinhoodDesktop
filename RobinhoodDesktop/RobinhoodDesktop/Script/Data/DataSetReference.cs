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
        /// The dataset this belongs to
        /// </summary>
        [NonSerialized]
        public StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> DataSet;

        /// <summary>
        /// The index of the data set in the overall list
        /// </summary>
        [NonSerialized]
        public int DataSetIndex;


        /// <summary>
        /// The index in the data set
        /// </summary>
        [NonSerialized]
        public int DataPointIndex;

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        partial void DataSetReference_Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex)
        {
            this.DataSet = data;
            this.DataSetIndex = data.ProcessingState.DataSetIndex;
            this.DataPointIndex = updateIndex;
        }
    }
}
