using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    public class StockAction
    {
        #region Types
        /// <summary>
        /// Performs the action
        /// </summary>
        /// <param name="processor">The processor triggering the action</param>
        /// <param name="target">The processing target that triggered the action</param>
        /// <param name="time">The time at which the action was triggered</param>
        public delegate void ActionHandler(StockProcessor processor, StockProcessor.ProcessingTarget target, DateTime time);
        #endregion

        #region Variables
        /// <summary>
        /// Callback to execute when a StockEvaluator returns true
        /// </summary>
        public ActionHandler Do;
        #endregion

        #region Actions
        /// <summary>
        /// Buys shares of the stock
        /// </summary>
        /// <param name="processor">The processor triggering the action</param>
        /// <param name="target">The processing target that triggered the action</param>
        /// <param name="time">The time at which the action was triggered</param>
        public void Buy(StockProcessor processor, StockProcessor.ProcessingTarget target, DateTime time)
        {
            Broker.Instance.SubmitOrder(new Broker.Order()
            {
                Symbol = target.Symbol,
                BuySell = Broker.Order.BuySellType.BUY,
                Type = Broker.Order.OrderType.MARKET,
                Quantity = 1
            });
        }

        /// <summary>
        /// Buys shares of the stock
        /// </summary>
        /// <param name="processor">The processor triggering the action</param>
        /// <param name="target">The processing target that triggered the action</param>
        /// <param name="time">The time at which the action was triggered</param>
        public void Sell(StockProcessor processor, StockProcessor.ProcessingTarget target, DateTime time)
        {
            Broker.Instance.GetPositionInfo(target.Symbol, (position) =>
            {
                Broker.Instance.SubmitOrder(new Broker.Order()
                {
                    Symbol = target.Symbol,
                    BuySell = Broker.Order.BuySellType.SELL,
                    Type = Broker.Order.OrderType.MARKET,
                    Quantity = position.Shares
                });
            });
            
        }

        /// <summary>
        /// Buys shares of the stock
        /// </summary>
        /// <param name="processor">The processor triggering the action</param>
        /// <param name="target">The processing target that triggered the action</param>
        /// <param name="time">The time at which the action was triggered</param>
        public void Notify(StockProcessor processor, StockProcessor.ProcessingTarget target, DateTime time)
        {
            StockList.Instance.Add(StockList.NOTIFICATIONS, target.Symbol, new StockList.NotificationSummary(DataAccessor.Subscribe(target.Symbol, DataAccessor.SUBSCRIBE_ONE_SEC)));
        }
        #endregion
    }
}
