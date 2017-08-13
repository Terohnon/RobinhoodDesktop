using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop
{
    public class Broker
    {

        #region Types
        public interface BrokerInterface
        {
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
            void SignIn(string username, string password);

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
        }
        #endregion

        #region Variables
        /// <summary>
        /// The instance of the broker being used
        /// </summary>
        private static BrokerInterface BrokerInstance;
        #endregion

        #region Static Interface
        /// <summary>
        /// Indicates if the interface is currently logged in to an account
        /// </summary>
        /// <returns>True if logged in to an account</returns>
        public static bool IsSignedIn()
        {
            return BrokerInstance.IsSignedIn();
        }

        /// <summary>
        /// Logs in to an account
        /// </summary>
        /// <param name="username">The account username (email address)</param>
        /// <param name="password">The account password</param>
        public static void SignIn(string username, string password)
        {
            BrokerInstance.SignIn(username, password);
        }

        /// <summary>
        /// Signs in to an account based on a stored token
        /// </summary>
        /// <param name="token">The account session token</param>
        public static void SignIn(string token)
        {
            BrokerInstance.SignIn(token);
        }

        /// <summary>
        /// Logs the user out of the brokerage account
        /// </summary>
        public static void SignOut()
        {
            BrokerInstance.SignOut();
        }

        /// <summary>
        /// Returns the authentication token associated with the current user's session
        /// </summary>
        /// <returns>The authentication token</returns>
        public static string GetAuthenticationToken()
        {
            return BrokerInstance.GetAuthenticationToken();
        }

        /// <summary>
        /// Returns the name of the currently authenticated user
        /// </summary>
        /// <returns>The current username</returns>
        public static string GetUsername()
        {
            return BrokerInstance.GetUsername();
        }
        #endregion

        /// <summary>
        /// Sets the instance to use
        /// </summary>
        /// <param name="instance">The instance</param>
        public static void SetBroker(BrokerInterface instance)
        {
            BrokerInstance = instance;
        }
    }
}
