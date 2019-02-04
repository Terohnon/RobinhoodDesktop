using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public interface StockData
    {
        void Update<T>(List<T> data, int index) where T : StockData;
    }
}
