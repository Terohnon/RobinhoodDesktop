using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BasicallyMe.RobinhoodNet.DataTypes;

namespace RobinhoodDesktop
{
    public class Broker
    {

        #region Types
        public interface BrokerInterface
        {
            string GenerateDeviceToken();

            /// <summary>
            /// Indicates if the interface is currently logged in to an account
            /// </summary>
            /// <returns>True if logged in to an account</returns>
            bool IsSignedIn();

            /// <summary>
            /// Logs in to an account
            /// </summary>
            /// <param name="username">The account username (email address)</param>
            /// <param name="password">The account password</param>
            (bool, ChallengeInfo) SignIn(string username, string password, string deviceToken, string challengeID);

            /// <summary>
            /// Completes signin to an account based on a sms code sent to device
            /// </summary>
            /// <param name="id_Code">The six digit sms code sent to device</param>
            (bool, ChallengeInfo) ChallengeResponse(string id, string id_code);

            /// <summary>
            /// Signs in to an account based on a stored token
            /// </summary>
            /// <param name="token">The account session token</param>
            void SignIn(string token);

            /// <summary>
            /// Logs the user out of the brokerage account
            /// </summary>
            void SignOut();

            /// <summary>
            /// Returns the authentication token associated with the current user's session
            /// </summary>
            /// <returns>The authentication token</returns>
            string GetAuthenticationToken();

            /// <summary>
            /// Returns the name of the currently authenticated user
            /// </summary>
            /// <returns>The current username</returns>
            string GetUsername();

            /// <summary>
            /// Retrieves the account information
            /// </summary>
            /// <param name="callback">Callback executed after the information has been retrieved</param>
            void GetAccountInfo(AccountCallback callback); 

            /// <summary>
            /// Returns a list of all stocks currently owned
            /// </summary>
            /// <returns>A list of owned stocks</returns>
            Dictionary<string, decimal> GetPositions();

            /// <summary>
            /// Returns more information about a current position
            /// </summary>
            /// <param name="symbol">The symbol to request the position for</param>
            /// <param name="callback">The callback to execute with the requested data</param>
            void GetPositionInfo(string symbol, PositionCallback callback);

            /// <summary>
            /// Returns information about a current position whenever there is a change
            /// </summary>
            /// <param name="symbol">The symbol to request the position for</param>
            /// <returns>The subscription instance</returns>
            PositionSubscription SubscribeToPositionInfo(string symbol);

            /// <summary>
            /// Ends the specified subscription
            /// </summary>
            /// <param name="subscription">The subscription to stop</param>
            void UnsubscribePosition(PositionSubscription subscription);

            /// <summary>
            /// Retrives a list of the current orders
            /// </summary>
            /// <returns>The list of orders</returns>
            List<Order> GetOrders();

            /// <summary>
            /// Submits an order to be executed by the broker
            /// </summary>
            /// <param name="order">The order to submit</param>
            void SubmitOrder(Order order);

            /// <summary>
            /// Cancels an outstanding order for the specified symbol
            /// </summary>
            /// <param name="symbol">The symbol to cancel the order for</param>
            void CancelOrder(string symbol);

            /// <summary>
            /// Returns a list of stocks being watched
            /// </summary>
            /// <returns>A watchlist registered with the broker</returns>
            List<string> GetWatchlist();

            /// <summary>
            /// Modifies a watchlist
            /// </summary>
            /// <param name="stock">The stock symbol to modify</param>
            /// <param name="action">Add -> Adds the stock to the watchlist
            ///                      Remove -> Deletes the stock from the watchlist
            ///                      Move[index] -> Moves the stock to the specified index</param>
            void ModifyWatchlist(string stock, string action);
        }

        public class Account
        {
            public decimal Cash;
            public decimal TotalValue;
            public decimal BuyingPower;
            public decimal UnsettledFunds;
            public decimal CashHeldForOrders;
            public decimal UnclearedDeposits;
            public decimal CashAvailableForWithdrawal;
        }

        /// <summary>
        /// Callback executed after retrieving account information
        /// </summary>
        /// <param name="account">The account information</param>
        public delegate void AccountCallback(Account account);

        /// <summary>
        /// Callback executed after retrieving information about a position
        /// </summary>
        /// <param name="position">The position information</param>
        public delegate void PositionCallback(Position position);

        public struct Position
        {
            public string Symbol;
            public decimal AverageBuyPrice;
            public decimal Shares;
        }

        public class Order
        {
            public enum OrderStatus
            {
                PENDING,
                COMPLETE,
                FAILED,
                CANCELLED
            };
            public enum OrderType
            {
                MARKET,
                LIMIT,
                STOP,
                STOP_LIMIT
            };
            public enum BuySellType
            {
                BUY,
                SELL
            }


            public string Symbol;
            public OrderStatus Status;
            public OrderType Type;
            public BuySellType BuySell;
            public decimal Quantity;
            public decimal AveragePrice;
            public decimal StopPrice;
            public decimal LimitPrice;
        }

        public class PositionSubscription
        {
            public Position PositionInfo;
            public delegate void NotifyCallback(PositionSubscription subscription);
            public NotifyCallback Notify;
            public bool Dirty;
            public DateTime LastUpdated;

            public PositionSubscription(string symbol)
            {
                this.PositionInfo.Symbol = symbol;
                this.Dirty = true;
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The instance of the broker being used
        /// </summary>
        public static BrokerInterface Instance;
        #endregion

        /// <summary>
        /// Sets the instance to use
        /// </summary>
        /// <param name="instance">The instance</param>
        public static void SetBroker(BrokerInterface instance)
        {
            Instance = instance;
        }
    }
}
