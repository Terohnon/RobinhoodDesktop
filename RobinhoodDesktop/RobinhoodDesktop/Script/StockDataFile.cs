using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using CSScriptLibrary;
using NetSerializer;

namespace RobinhoodDesktop.Script
{
    [Serializable]
    public class StockDataFile
    {
        public StockDataFile(List<string> fields, List<string> sources)
        {
            this.Fields = fields;
            this.SourceCode = GenSourceCode(sources).ToArray();
        }

        private StockDataFile()
        {
            this.Fields = new List<string>();
            this.SourceCode = GenSourceCode(new List<string>()).ToArray();
        }

#region Stored Data
        /// <summary>
        /// The list of all of the stock symbols contained within the file
        /// </summary>
        public List<string> Symbols
        {
            get { return Segments.Keys.ToList(); }
        }

        /// <summary>
        /// The earliest time in the file
        /// </summary>
        public DateTime Start;

        /// <summary>
        /// The last time in the file
        /// </summary>
        public DateTime End;

        /// <summary>
        /// The amount of time between data points
        /// </summary>
        public TimeSpan Interval;

        /// <summary>
        /// The names of the source files that were used to build the StockData class
        /// </summary>
        public List<string> Fields;

        /// <summary>
        /// The concatinated source code files used to generate the data
        /// </summary>
        private char[] SourceCode;

        /// <summary>
        /// The actual data, divided into segments that can be loaded into memory
        /// </summary>
        public Dictionary<string, List<Tuple<DateTime, long>>> Segments = new Dictionary<string, List<Tuple<DateTime, long>>>();

        /// <summary>
        /// The address where the header is written to in the file
        /// </summary>
        [NonSerialized]
        private long HeaderAddress = -1;

        /// <summary>
        /// The hard copy of the data
        /// </summary>
        [NonSerialized]
        public Stream File;
        #endregion

        /// <summary>
        /// Loads a segment from the source stream into a usable object
        /// </summary>
        /// <typeparam name="T">The type of the data points in the segments</typeparam>
        /// <returns>The data segments</returns>
        public Dictionary<string, List<StockDataSet<T>>> GetSegments<T>() where T : StockData
        {
            /*
            var loader = CSScript.LoadCode(SourceCode).GetStaticMethod(".Load");
            var data = (Dictionary<string, List<T>>)loader(s, Segments[segmentIndex].Item2);
            var retVal = new Dictionary<string, StockDataSet<T>>();
            var startTime = Segments[segmentIndex].Item1;
            var endTime = startTime.AddTicks(Interval.Ticks * data.ElementAt(0).Value.Count);
            foreach(KeyValuePair<string, List<T>> pair in data)
            {
                var set = new StockDataSet<T>(pair.Key, startTime, endTime, Interval);
                set.DataSet = pair.Value;
            }; */
            Dictionary<string, List<StockDataSet<T>>> dataSets = new Dictionary<string, List<StockDataSet<T>>>();
            foreach(KeyValuePair<string, List<Tuple<DateTime, long>>> pair in Segments)
            {
                dataSets[pair.Key] = new List<StockDataSet<T>>();
                foreach(Tuple<DateTime, long> set in pair.Value)
                {
                    dataSets[pair.Key].Add(new StockDataSet<T>(pair.Key, set.Item1, this, set.Item2));
                }
            }
            return dataSets;
        }

        /// <summary>
        /// Saves the 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="segment"></param>
        public void SetSegments<T>(Dictionary<string, List<StockDataSet<T>>> segments) where T : StockData
        {
            this.Start = DateTime.MaxValue;
            this.End = DateTime.MinValue;
            this.Segments.Clear();
            foreach(KeyValuePair<string, List<StockDataSet<T>>> pair in segments)
            {
                this.Segments[pair.Key] = new List<Tuple<DateTime, long>>(pair.Value.Count);
                foreach(StockDataSet<T> set in pair.Value)
                {
                    this.Segments[pair.Key].Add(new Tuple<DateTime, long>(set.Start, long.MaxValue));
                    if(set.Start < Start) Start = set.Start;
                    if(set.End > End) End = set.End;
                }
            }
                
        }

        /// <summary>
        /// Loads the segment data from the source
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <param name="segment">The segment to populate</param>
        public void LoadSegment<T>(StockDataSet<T> segment) where T : StockData
        {
            var des = new Serializer(new List<Type>() { typeof(T), typeof(int) });
            foreach(Tuple<DateTime, long> t in this.Segments[segment.Symbol])
            {
                if(t.Item1 == segment.Start)
                {
                    File.Seek(t.Item2, SeekOrigin.Begin);
                    int count = (int)des.Deserialize(File);
                    for(int i = 0; i < count; i++) segment.DataSet.Add((T)des.Deserialize(File));
                    break;
                }
            }
        }

        /// <summary>
        /// Save this instance to the stream
        /// </summary>
        /// <param name="s">The stream to save this to</param>
        public void Save<T>(Stream s, Type dataType, Dictionary<string, List<StockDataSet<T>>> segments) where T : StockData
        {
            var headerSer = new Serializer(new List<Type>() { typeof(StockDataFile) });
            var dataSer = new Serializer(new List<Type>() { dataType, typeof(int) });
            this.File = s;


            // Serialize the segments
            s.Seek(0x10, SeekOrigin.End);
            foreach(KeyValuePair<string, List<StockDataSet<T>>> pair in segments)
            {
                List<Tuple<DateTime, long>> allSegments = this.Segments[pair.Key];
                int matchingIdx = 0;
                for(int segIdx = 0; segIdx < pair.Value.Count; segIdx++)
                {
                    StockDataSet<T> set = pair.Value[segIdx];
                    for(; matchingIdx < allSegments.Count; matchingIdx++)
                    {
                        if(set.Start == allSegments[matchingIdx].Item1)
                        {
                            allSegments[matchingIdx] = new Tuple<DateTime, long>(set.Start, s.Position);
                            set.StreamAddress = s.Position;
                            dataSer.Serialize(s, set.DataSet.Count);
                            foreach(var p in set.DataSet) dataSer.Serialize(s, p);
                            matchingIdx++;
                            break;
                        }
                    }
                }
            }

            // Re-serialize the header with the updated segment addresses
            this.HeaderAddress = s.Position;
            headerSer.Serialize(s, this);

            // Set the offset to the header
            s.Seek(0, SeekOrigin.Begin);
            headerSer.Serialize(s, this.HeaderAddress);
        }

        

        /// <summary>
        /// Reads the instance from the stream
        /// </summary>
        /// <param name="s">The stream to read from</param>
        /// <returns>The stock file instance</returns>
        public static StockDataFile Open(Stream s)
        {
            var des = new Serializer(new List<Type>() { typeof(StockDataFile) });
            long address = (long)des.Deserialize(s);
            s.Seek(address, SeekOrigin.Begin);
            var newFile = (StockDataFile)des.Deserialize(s);
            newFile.File = s;
            return newFile;
        }

        /// <summary>
        /// Returns the script source code using the specified class name
        /// </summary>
        /// <param name="className">The name of the class the script code should be set to</param>
        /// <returns>The script source code</returns>
        public string GetSourceCode(string className)
        {
            return new string(this.SourceCode).Replace("StockDataScript", className);
        }
        
        /// <summary>
        /// Combines the separate source files into a single script
        /// </summary>
        /// <param name="sources">The source file contents</param>
        /// <returns>The generated source code for the stock data file</returns>
        private string GenSourceCode(List<string> sources)
        {
            var code = "";
            var assembly = Assembly.GetExecutingAssembly();
            var scriptFilename = "RobinhoodDesktop.Script.StockDataScript.cs";

            using(Stream stream = assembly.GetManifestResourceStream(scriptFilename))
            using(StreamReader reader = new StreamReader(stream))
            {
                code = reader.ReadToEnd();
                string prototypes = "";
                string updates = "";
                foreach(string name in this.Fields)
                {
                    prototypes += "partial void " + name + "_Update(List<StockDataSource> data, int updateIndex);\n";
                    updates += name + "_Update(data, updateIndex);\n";
                }
                code = code.Replace("///= PartialPrototypes ///", prototypes.Replace("\n", "\n        "));
                code = code.Replace("///= PartialUpdates ///", updates.Replace("\n", "\n                "));
            }

            for(int srcIdx = 0; srcIdx < sources.Count; srcIdx++)
            {
                code += sources[srcIdx];
            }

            // Move all using statements to the top of the file
            var usingStr = new StringBuilder();
            var otherStr = new StringBuilder();
            using(StringReader reader = new StringReader(code))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    if(Regex.IsMatch(line, "^using .*;"))
                    {
                        if(!usingStr.ToString().Contains(line)) usingStr.AppendLine(line);
                    }
                    else
                    {
                        otherStr.AppendLine(line);
                    }
                }
            }

            return (usingStr.AppendLine(otherStr.ToString()).ToString());
        }

        #region Legacy File Interface
        /// <summary>
        /// Reads the specified data streams, and converts them into a basic stock data file
        /// </summary>
        /// <param name="sourceFiles">The legacy data files</param>
        /// <returns>The new data file</returns>
        public static StockDataFile Convert(List<string> sourceFiles, Stream destination)
        {
            StockDataFile newFile = new StockDataFile();
            newFile.Interval = new TimeSpan(0, 1, 0);
            Dictionary<string, List<StockDataSet<StockData>>> segments = new Dictionary<string, List<StockDataSet<StockData>>>();
            string src = newFile.GetSourceCode("StockDataSource");
            var scriptInstance = CSScript.LoadCode(src);
            var stockDataType = scriptInstance.GetType("RobinhoodDesktop.Script.StockDataSource");
            var creator = scriptInstance.GetStaticMethod("*.CreateFromPrice", 0.0f);

            var listType = typeof(List<>).MakeGenericType(typeof(StockDataSet<>).MakeGenericType(stockDataType));

            foreach(string filename in sourceFiles)
            {
                // Parse the date from the filename
                int year = int.Parse(filename.Substring(filename.Length - 12, 4));
                int month = int.Parse(filename.Substring(filename.Length - 8, 2));
                int day = int.Parse(filename.Substring(filename.Length - 6, 2));
                DateTime fileDate = new DateTime(year, month, day);
                DateTime fileStart = fileDate.AddHours(9.5);
                DateTime fileEnd = fileDate.AddHours(16);
                long delayedOffset = (filename.Contains("goog")) ? 0 : new TimeSpan(0, 15, 0).Ticks;

                // Read the initial line of the file to learn which stocks are in the file
                StreamReader s = new StreamReader(filename);
                List<StockDataSet<StockData>> fileData = new List<StockDataSet<StockData>>();
                string line = s.ReadLine();
                string[] stockNamesStr = line.Split(new char[] { ',' });
                for(int i = 1; i < stockNamesStr.Length; i++)
                {
                    string symbol = stockNamesStr[i].ToUpper();
                    List<StockDataSet<StockData>> sets;

                    // Check if a stock data set already exists for the symbol
                    if(!segments.TryGetValue(symbol, out sets))
                    {
                        sets = new List<StockDataSet<StockData>>();
                        segments.Add(symbol, sets);
                    }
                    foreach(StockDataSet<StockData> preExistingSet in sets)
                    {
                        // Add to an existing set if they are for the same month
                        if(preExistingSet.Start.Month == fileStart.Month)
                        {
                            fileData.Add(preExistingSet);
                            break;
                        }
                    }
                    if(fileData.Count <= (i - 1))
                    {
                        // No matching set was found, so create a new one
                        StockDataSet<StockData> newSet = new StockDataSet<StockData>(symbol, fileStart, newFile);
                        fileData.Add(newSet);
                        sets.Add(newSet);
                    }
                }

                DateTime lineTime = fileStart;
                while(!s.EndOfStream && (lineTime < fileEnd))
                {
                    line = s.ReadLine();
                    string[] stockPricesStr = line.Split(new char[] { ',' });

                    // Ensure the entry contains all of the stocks
                    if((stockPricesStr.Length - 1) != fileData.Count)
                    {
                        continue;
                    }

                    // Get the time of the entry
                    DateTime newTime = DateTime.Parse(stockPricesStr[0].Replace("\"", ""));
                    newTime = fileStart.AddTicks((newTime.TimeOfDay.Ticks - fileStart.TimeOfDay.Ticks) - delayedOffset);

                    // Check if this new entry is valid
                    if((newTime >= fileStart) && (newTime <= fileEnd))
                    {
                        // Update the prices of each of the stocks
                        for(int i = 0; i < (stockPricesStr.Length - 1); i++)
                        {
                            float price;
                            if(float.TryParse(stockPricesStr[i + 1], out price))
                            {
                                while(fileData[i].End <= newTime)
                                {
                                    StockData point = (StockData)creator(price);
                                    fileData[i].DataSet.Add(point);
                                }
                            }
                        }
                    }
                }
            }

            // Save the old file to disk
            newFile.SetSegments(segments);
            newFile.Save(destination, stockDataType, segments);

            return newFile;
        }
        #endregion
    }
}
