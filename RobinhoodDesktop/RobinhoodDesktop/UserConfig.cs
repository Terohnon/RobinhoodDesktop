﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace RobinhoodDesktop
{
    [Serializable]
    public class UserConfig
    {
        #region Constants
        /// <summary>
        /// The path to the config file
        /// </summary>
        public static readonly string CONFIG_FILE = "Config.xml";
        #endregion

        #region Variables
        /// <summary>
        /// The username of the brokerage account
        /// </summary>
        public string Username;

        /// <summary>
        /// Indicates if the user wants their login information remembered
        /// </summary>
        public bool RememberLogin;

        /// <summary>
        /// The authentication token used to log in to the brokerage
        /// </summary>
        public string AuthenticationToken;

        /// <summary>
        /// The device token generated to identify device for future login without 2 factor auth
        /// </summary>
        public string DeviceToken;

        /// <summary>
        /// A list of stock symbols being watched. 
        /// This is kept local to this application.
        /// </summary>
        public List<string> LocalWatchlist = new List<string>();

        /// <summary>
        /// A list of the open stock charts, and the corresponding options.
        /// </summary>
        public List<StockChartPanel.StockUIConfig> StockCharts = new List<StockChartPanel.StockUIConfig>();

        /// <summary>
        /// The configuration for the algorithm screen
        /// </summary>
        public AlgorithmScreen.AlgorithmScreenConfig AlgorithmScreenConfig = new AlgorithmScreen.AlgorithmScreenConfig();
        #endregion

        /// <summary>
        /// Saves the config file
        /// </summary>
        /// <param name="filePath">The path to save the file to</param>
        public void Save(string filePath)
        {
            XmlSerializer xs = new XmlSerializer(typeof(UserConfig));

            using(var writer = new System.IO.StreamWriter(filePath))
            {
                var serializer = new XmlSerializer(this.GetType());
                serializer.Serialize(writer, this);
                writer.Flush();
            }
        }

        /// <summary>
        /// Loads a configuration from a file
        /// </summary>
        /// <param name="filePath">The path to save the file to</param>
        /// <returns>The loaded object</returns>
        public static UserConfig Load(string filePath)
        {
            UserConfig cfg = null;

            try
            {
                if(File.Exists(filePath))
                {
                    using(var stream = System.IO.File.OpenRead(filePath))
                    {
                        var serializer = new XmlSerializer(typeof(UserConfig));
                        cfg = serializer.Deserialize(stream) as UserConfig;
                    }
                }
            }
            catch(Exception ex) { }
            
            if(cfg == null)
            {
                cfg = new UserConfig();
            }

            return cfg;
        }
    }
}
