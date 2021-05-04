﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using Newtonsoft.Json;
using CSScriptLibrary;
using NetSerializer;

namespace RobinhoodDesktop.Script
{
    [Serializable]
    public class StockDataFile
    {
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
            newFile.LoadAddress = s.Position;
            newFile.FileMutex = new System.Threading.Mutex();
            return newFile;
        }

        /// <summary>
        /// Opens a group of files and agregates them into one source
        /// </summary>
        /// <param name="files">The stream of files to open</param>
        /// <returns>The agregate stock data file</returns>
        public static StockDataFile Open(List<Stream> files)
        {
            StockDataFile file;
            if(files.Count > 1) file = new AggregatorFile(files);
            else file = Open(files.First());
            return file;
        }

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

        /// <summary>
        /// Closes the file stream
        /// </summary>
        public virtual void Close()
        {
            if(this.File != null) this.File.Close();
            this.File = null;
        }

        /// <summary>
        /// Reloads any script references 
        /// </summary>
        public virtual void Reload()
        {
            
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
        /// The address where additional data can be loaded from in the file
        /// </summary>
        [NonSerialized]
        private long LoadAddress = -1;

        /// <summary>
        /// The hard copy of the data
        /// </summary>
        [NonSerialized]
        public Stream File;

        /// <summary>
        /// Controls access to the source file
        /// </summary>
        [NonSerialized]
        private System.Threading.Mutex FileMutex = new System.Threading.Mutex();

        /// <summary>
        /// A progress indicator [0 - UInt32.MaxValue] which can be used for long operations
        /// </summary>
        [NonSerialized]
        public UInt32 Progress;
        #endregion

        #region Aggregator Class
        public class AggregatorFile : StockDataFile
        {
            /// <summary>
            /// The source files being agregated
            /// </summary>
            [NonSerialized]
            public List<StockDataFile> Sources = new List<StockDataFile>();

            /// <summary>
            /// The method used to load a segment from the source file(s)
            /// </summary>
            [NonSerialized]
            private MethodDelegate LoadMethod;

            /// <summary>
            /// Constructor for a file agregator
            /// </summary>
            /// <param name="files">The streams containing the source files</param>
            public AggregatorFile(List<Stream> files)
            {
                // Open the source files
                int count = 0;
                List<FieldInfo> fields = new List<FieldInfo>();
                List<FieldInfo[]> sourceFields = new List<FieldInfo[]>();
                Interval = TimeSpan.MaxValue;

                foreach(var s in files)
                {
                    var f = StockDataFile.Open(s);
                    
                    try
                    {
                        // Get the members in the source
                        string typeName = "Source" + count.ToString();
                        var scriptInstance = CSScript.LoadCode(f.GetSourceCode(typeName).Replace("StockDataSource", typeName), null);
                        Type t = scriptInstance.DefinedTypes.Where((scriptType) => { return scriptType.Name.Equals(typeName); }).First();
                        var fieldList = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
                        sourceFields.Add(fieldList);
                        foreach(var field in fieldList)
                        {
                            if(!fields.Contains(field))
                            {
                                fields.Add(field);
                            }
                        }

                        // Check the date range included in the source
                        foreach(var pair in f.Segments)
                        {
                            List<Tuple<DateTime, long>> segList;
                            if(!this.Segments.TryGetValue(pair.Key, out segList))
                            {
                                segList = new List<Tuple<DateTime, long>>();
                                this.Segments.Add(pair.Key, segList);
                            }
                            foreach(var seg in pair.Value)
                            {
                                if(segList.Find((ele) => { return ele.Item1.Equals(seg.Item1); }) == null)
                                {
                                    segList.Add(new Tuple<DateTime, long>(seg.Item1, long.MaxValue));
                                }
                            }
                        }

                        // Use the smallest interval from among the sources
                        this.Interval = ((f.Interval < this.Interval) ? f.Interval : this.Interval);

                        // Remember the source file
                        Sources.Add(f);
                    }
                    catch(Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show(ex.ToString());
                    }
                }

                // Generate the source code based on the member list
                var code = GenSourceCode(fields, sourceFields).Replace("StockDataSink", "StockDataSource");
                this.SourceCode = code.ToArray();
            }

            /// <summary>
            /// Closes the file stream
            /// </summary>
            public override void Close()
            {
                foreach(var s in Sources)
                {
                    s.Close();
                }
            }

            /// <summary>
            /// Reloads any script references 
            /// </summary>
            public override void Reload()
            {
                this.LoadMethod = null;
            }

            /// <summary>
            /// Loads any additional static information from the file
            /// <param name="session">The session this is being loaded as part of</param>
            /// </summary>
            public override void LoadStaticData(StockSession session)
            {
                for(int idx = 0; idx < Sources.Count; idx++)
                {
                    var src = Sources[idx];
                    var loadMethod = session.Scripts[session].GetStaticMethod("RobinhoodDesktop.Script.Source" + idx + ".Load", src.File);
                    src.FileMutex.WaitOne();
                    src.File.Seek(src.LoadAddress, SeekOrigin.Begin);
                    loadMethod(src.File);
                    src.FileMutex.ReleaseMutex();
                }
            }

            /// <summary>
            /// Loads the segment data from the source
            /// </summary>
            /// <typeparam name="T">The data point type</typeparam>
            /// <param name="session">The session this is part of</param>
            /// <param name="segment">The segment to populate</param>
            public override void LoadSegment<T>(StockDataSet<T> segment, StockSession session = null)
            {
                if((this.LoadMethod == null) && (session != null))
                {
                    this.LoadMethod = session.Scripts[session].GetStaticMethod("*.Load", this, "", DateTime.Now);
                }
                segment.DataSet.Initialize((T[])LoadMethod(this, segment.Symbol, segment.Start));
            }

            /// <summary>
            /// Returns the number of data points in the given segment
            /// </summary>
            /// <typeparam name="T">The data point type</typeparam>
            /// <param name="segment">The segment to check</param>
            /// <returns>The number of data points in the segment when it is loaded</returns>
            public override int GetSegmentSize<T>(StockDataSet<T> segment)
            {
                int count = 0;
                for(int s = 0; s < Sources.Count; s++)
                {
                    if((Sources[s].Start <= segment.Start) && (Sources[s].End > segment.Start))
                    {
                        count = Sources[s].GetSegmentSize<T>(segment);
                        break;
                    }
                }
                return count;
            }

            /// <summary>
            /// Generates the source code for the agregator
            /// </summary>
            /// <param name="members">The list of members that should be present in the agregator</param>
            /// <param name="sourceMembers">The list of members owned by each source</param>
            /// <returns></returns>
            private String GenSourceCode(List<FieldInfo> fields, List<FieldInfo[]> sourceFields)
            {
                var code = "";
                var assembly = Assembly.GetExecutingAssembly();
                var scriptFilename = "RobinhoodDesktop.Script.StockDataSink.cs";

                using(Stream stream = assembly.GetManifestResourceStream(scriptFilename))
                using(StreamReader reader = new StreamReader(stream))
                {
                    code = reader.ReadToEnd();
                    string memberDecl = "";
                    foreach(var f in fields)
                    {
                        if(!f.Name.Equals("Price"))
                        {
                            memberDecl += "public " + f.FieldType.ToString() + " " + f.Name.ToString() + ";\n";
                        }
                    }
                    memberDecl = memberDecl.Replace("\n", "\n        ");

                    // Add the agregate file specific functionality
                    string loader =
                        "#region Loader\n" +
                        "public static StockDataSource[] Load(StockDataFile.AggregatorFile f, string symbol, DateTime start)\n" +
                        "{\n" +
                            "\tint maxCount = 0;\n";
                    for(int idx = 0; idx < Sources.Count; idx++)
                    {
                        string i = idx.ToString();
                        loader += "\tSource" + i + "[] s" + i + " = f.Sources[" + i + "].LoadData<Source" + i + ">(symbol, start);\n";
                        loader += "\tmaxCount = ((maxCount > s" + i + ".Length) ? maxCount : s" + i + ".Length);\n";
                    }
                    loader +=
                        "\tStockDataSource[] s = new StockDataSource[maxCount];\n" +
                        "\tfor(int i = 0; i < maxCount; i++)\n" +
                        "\t{\n";
                    for(int idx = 0; idx < Sources.Count; idx++)
                    {
                        string i = idx.ToString();
                        loader += "\t\tif(s" + i + ".Length > i) {\n";
                        foreach(var f in sourceFields[idx])
                        {
                            loader += "\t\t\ts[i]." + f.Name + " = s" + i + "[i]." + f.Name + ";\n";
                        }
                        loader += "\t\t}\n";
                    }
                    loader +=
                        "\t}\n" +
                        "\treturn s;\n" +
                        "}\n" +
                        "#endregion\n";

                    // Add the custom functionality to the script
                    memberDecl += loader.Replace("\n", "\n        ");
                    code = code.Replace("///= Members ///", memberDecl);
                    for(int idx = 0; idx < Sources.Count; idx++)
                    {
                        code += new string(Sources[idx].SourceCode).Replace("StockDataSink", "Source" + idx.ToString());
                    }
                }

                return FormatSource(code);
            }
        }
        #endregion

        /// <summary>
        /// Loads any additional static information from the file
        /// <param name="session">The session this is part of</param>
        /// </summary>
        public virtual void LoadStaticData(StockSession session)
        {
            var loadMethod = session.Scripts[session].GetStaticMethod("*.Load", File);
            FileMutex.WaitOne();
            File.Seek(LoadAddress, SeekOrigin.Begin);
            loadMethod(File);
            FileMutex.ReleaseMutex();
        }

        /// <summary>
        /// Loads a segment from the source stream into a usable object
        /// </summary>
        /// <typeparam name="T">The type of the data points in the segments</typeparam>
        /// <returns>The data segments</returns>
        public Dictionary<string, List<StockDataSet<T>>> GetSegments<T>() where T : struct, StockData
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
                StockDataSet<T> prevSet = null;
                dataSets[pair.Key] = new List<StockDataSet<T>>();
                foreach(Tuple<DateTime, long> set in pair.Value)
                {
                    var nextSet = new StockDataSet<T>(pair.Key, set.Item1, this, set.Item2);
                    nextSet.Previous = prevSet;
                    prevSet = nextSet;
                    dataSets[pair.Key].Add(nextSet);
                }
            }
            return dataSets;
        }

        /// <summary>
        /// Sets the stock data segments which are present in this file
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <param name="segments">The set of segments to specify for this file</param>
        public void SetSegments<T>(Dictionary<string, List<StockDataSet<T>>> segments) where T : struct, StockData
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
        /// <param name="session">The session this is part of</param>
        public virtual void LoadSegment<T>(StockDataSet<T> segment, StockSession session = null) where T : struct, StockData
        {
            FileMutex.WaitOne();
            segment.DataSet.Initialize(LoadData<T>(segment.StreamAddress));
            FileMutex.ReleaseMutex();
        }

        /// <summary>
        /// Utility to load an array of data points from the file
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <param name="address">The address in the file stream where the item is located</param>
        /// <returns>An array of loaded data points</returns>
        public T[] LoadData<T>(long address) where T : struct, StockData
        {
            T[] data = null;

            File.Seek(address, SeekOrigin.Begin);
            data = Load<T>(File);

            if(data == null) data = new T[0];
            return data;
        }

        /// <summary>
        /// Utility to load an array of data points from the file
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <param name="symbol">The stock symbol</param>
        /// <param name="start">The period start time</param>
        /// <returns>An array of loaded data points</returns>
        public T[] LoadData<T>(string symbol, DateTime start) where T : struct, StockData
        {
            T[] data = null;
            foreach(Tuple<DateTime, long> t in this.Segments[symbol])
            {
                if(t.Item1 == start)
                {
                    data = LoadData<T>(t.Item2);
                    break;
                }
            }

            if(data == null) data = new T[0];
            return data;
        }

        /// <summary>
        /// Returns the number of data points in the given segment
        /// </summary>
        /// <typeparam name="T">The data point type</typeparam>
        /// <param name="segment">The segment to check</param>
        /// <returns>The number of data points in the segment when it is loaded</returns>
        public virtual int GetSegmentSize<T>(StockDataSet<T> segment) where T : struct, StockData
        {
            int count = 0;
            foreach(Tuple<DateTime, long> t in this.Segments[segment.Symbol])
            {
                if(t.Item1 == segment.Start)
                {
                    File.Seek(t.Item2, SeekOrigin.Begin);
                    count = (File.ReadByte() << 8) | File.ReadByte();
                    break;
                }
            }
            return count;
        }

        /// <summary>
        /// Save this instance to the stream
        /// </summary>
        /// <param name="session">The session this is part of</param>
        /// <param name="s">The stream to save this to</param>
        public void Save<T>(Stream s, Type dataType, Dictionary<string, List<StockDataSet<T>>> segments, StockSession session = null) where T : struct, StockData
        {
            var headerSer = new Serializer(new List<Type>() { typeof(StockDataFile) });
            var dataSer = new Serializer(new List<Type>() { dataType.MakeArrayType() });
            this.File = s;


            // Serialize the segments
            s.Seek(0x10, SeekOrigin.End);   // Leave some space at the beginning of the stream to store the offset to the serialized stock file
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
                            if(typeof(T) == dataType)
                            {
                                //dataSer.Serialize(s, set.DataSet.InternalArray);
                                Store(s, set.DataSet.InternalArray, set.DataSet.Count);
                            }
                            else
                            {
                                var data_points = Array.CreateInstance(dataType, set.DataSet.Count);
                                for(int pntIdx = 0; pntIdx < set.DataSet.Count; pntIdx++) data_points.SetValue(set.DataSet.InternalArray[pntIdx], pntIdx);
                                dataSer.Serialize(s, data_points);
                            }
                            matchingIdx++;
                            break;
                        }
                    }
                }
            }

            // Serialize the header with the updated segment addresses
            long headerAddress = s.Position;
            headerSer.Serialize(s, this);

            // Save any script-specific data
            Assembly dataScript;
            if((session != null) && session.Scripts.TryGetValue(session, out dataScript))
            {
                var saveMethod = dataScript.GetStaticMethod("*.Save", s);
                saveMethod(s);
            }

            // Set the offset to the header
            s.Seek(0, SeekOrigin.Begin);
            headerSer.Serialize(s, headerAddress);
        }

        /// <summary>
        /// Returns the script source code using the specified class name
        /// </summary>
        /// <param name="className">The name of the class the script code should be set to</param>
        /// <returns>The script source code</returns>
        public string GetSourceCode(string className)
        {
            return new string(this.SourceCode).Replace("StockDataSink", className);
        }

        /// <summary>
        /// Generates the source code for the stock data sink corresponding to this file
        /// </summary>
        /// <returns>The stock data sink source code</returns>
        public string GenStockDataSink()
        {
            var code = "";
            var assembly = Assembly.GetExecutingAssembly();
            var scriptFilename = "RobinhoodDesktop.Script.StockDataSink.cs";

            using (Stream stream = assembly.GetManifestResourceStream(scriptFilename))
            using (StreamReader reader = new StreamReader(stream))
            {
                code = reader.ReadToEnd();
                string prototypes = "";
                string updates = "";
                string saves = "";
                string loads = "";
                foreach (string name in this.Fields)
                {
                    prototypes += "partial void " + name + "_Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex);\n";
                    prototypes += "static partial void " + name + "_Save(System.IO.Stream file);\n";
                    prototypes += "static partial void " + name + "_Load(System.IO.Stream file);\n";
                    updates += name + "_Update(data, updateIndex);\n";
                    saves += name + "_Save(file);\n";
                    loads += name + "_Load(file);\n";
                }
                code = code.Replace("///= PartialPrototypes ///", prototypes.Replace("\n", "\n        "));
                code = code.Replace("///= PartialUpdates ///", updates.Replace("\n", "\n                "));
                code = code.Replace("///= PartialSaves ///", saves.Replace("\n", "\n                "));
                code = code.Replace("///= PartialLoads ///", loads.Replace("\n", "\n                "));
            }

            return code;
        }


        /// <summary>
        /// Combines the separate source files into a single script
        /// </summary>
        /// <param name="sources">The source file contents</param>
        /// <returns>The generated source code for the stock data file</returns>
        private string GenSourceCode(List<string> sources)
        {
            var code = GenStockDataSink();

            for(int srcIdx = 0; srcIdx < sources.Count; srcIdx++)
            {
                code += sources[srcIdx];
            }

            return FormatSource(code);
        }

        /// <summary>
        /// Formats the source code per the C# compiler requirements.
        /// Specifically, moves all using statements to the top.
        /// </summary>
        /// <param name="code">The concatinated source code files</param>
        /// <returns>The formatted source code</returns>
        public static string FormatSource(string code)
        {
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
        private static DateTime GetDateFromFileName(string filename)
        {
            int year = int.Parse(filename.Substring(filename.Length - 12, 4));
            int month = int.Parse(filename.Substring(filename.Length - 8, 2));
            int day = int.Parse(filename.Substring(filename.Length - 6, 2));
            return new DateTime(year, month, day);
        }

        public static List<StockDataFile> ConvertByMonth(List<string> sourceFiles, string destDir, out List<string> filenames)
        {
            List<StockDataFile> files = new List<StockDataFile>();

            // Divide the source files into groups based on the month
            Dictionary<DateTime, List<string>> months = new Dictionary<DateTime, List<string>>();
            foreach(var f in sourceFiles)
            {
                DateTime fTime = GetDateFromFileName(f);
                DateTime fMonth = new DateTime(fTime.Year, fTime.Month, 1);
                List<string> monthFiles;
                if(!months.TryGetValue(fMonth, out monthFiles))
                {
                    monthFiles = new List<string>();
                    months[fMonth] = monthFiles;
                }
                monthFiles.Add(f);
            }

            // Save each file as a month to the destination directory
            filenames = new List<string>();
            foreach (var pair in months)
            {
                var fname = destDir + "\\" + $"{pair.Key:yyyyMM}.rbn";
                filenames.Add(fname);
                var f = Convert(pair.Value, new FileStream(fname, FileMode.Create));
                files.Add(f);
                f.Close();
            }

            return files;
        }

        /// <summary>
        /// Filters the datasets to remove large spikes due to incorrect prices reported by the data source
        /// </summary>
        /// <param name="segments">The data to be filtered</param>
        private static void FilterData(Dictionary<string, List<StockDataSet<StockDataBase>>> segments)
        {
            var transitions = new List<int>();
            var transitionVolatilities = new List<float>();
            foreach (var pair in segments)
            {
                foreach(var set in pair.Value)
                {
                    if(pair.Key == "AGNC" && set.Start.Year == 2012 && set.Start.Month == 3 && set.Start.Day == 1)
                    {

                    
                    }
                    float volatility = 0;
                    int volatilityPoints = 0;
                    transitions.Clear();
                    transitionVolatilities.Clear();
                    if(set.Previous == null)
                    {
                        transitions.Add(0);
                        transitionVolatilities.Add(0);
                    }
                    for (int i = (set.Previous != null) ? 0 : 1; i < set.Count; i++)
                    {
                        // Check for a large jump in price
                        float diff = Math.Abs((set[i].Price / set[i - 1].Price) - 1.0f);
                        if (diff > 0.025f)
                        {
                            // Mark the jump in price as a transition point, and record the volatility leading up to that point
                            transitions.Add(i);
                            transitionVolatilities.Add((volatilityPoints > 0) ? volatility / volatilityPoints : 0);
                            volatility = 0;
                            volatilityPoints = 0;
                        }
                        else
                        {
                            // Look for when the price starts changing again
                            volatility += diff;
                            volatilityPoints++;
                            if(((volatilityPoints > 20) && ((volatility / volatilityPoints) > 0.00025f)) ||
                                (i == set.Count - 1))
                            {
                                // Looks like things are changing normally, so check if there are any transitions that need to be filtered out
                                if(transitions.Count >= 1)
                                {
                                    int end = ((i == set.Count - 1) ? i + 1 : transitions[transitions.Count - 1]);
                                    int start = transitions[transitions.Count - 1];
                                    for(int t = transitions.Count - 1; t > 0; t--)
                                    {
                                        // Check for transitions which had a period of very low volatility
                                        if(transitionVolatilities[t] < 0.00025f)
                                        {
                                            start = transitions[t - 1];
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    float startPrice = ((start > 0 || set.Previous != null) ? set[start - 1].Price : set[end + 1].Price);
                                    float endPrice = ((i == set.Count - 1) ? startPrice : set[end + 1].Price);
                                    for(int j = start; (end > start) && (startPrice != 0) && (endPrice != 0) && (j < end); j++)
                                    {
                                        set.DataSet.InternalArray[j].Price = (startPrice + ((endPrice - startPrice) * ((float)((j - start) + 1) / ((end - start) + 1))));
                                    }
                                    transitions.Clear();
                                    transitionVolatilities.Clear();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads the specified data streams, and converts them into a basic stock data file
        /// </summary>
        /// <param name="sourceFiles">The legacy data files</param>
        /// <returns>The new data file</returns>
        public static StockDataFile Convert(List<string> sourceFiles, Stream destination)
        {
            StockDataFile newFile = new StockDataFile();
            newFile.Interval = new TimeSpan(0, 1, 0);
            Dictionary<string, List<StockDataSet<StockDataBase>>> segments = new Dictionary<string, List<StockDataSet<StockDataBase>>>();

            foreach(string filename in sourceFiles)
            {
                // Parse the date from the filename
                DateTime fileDate = GetDateFromFileName(filename);
                DateTime fileStart = fileDate.AddHours(9.5);
                DateTime fileEnd = fileDate.AddHours(16);
                long delayedOffset = (filename.Contains("goog")) ? 0 : new TimeSpan(0, 15, 0).Ticks;

                // Read the initial line of the file to learn which stocks are in the file
                StreamReader s = new StreamReader(filename);
                List<StockDataSet<StockDataBase>> fileData = new List<StockDataSet<StockDataBase>>();
                string line = s.ReadLine();
                string[] stockNamesStr = line.Split(new char[] { ',' });
                for(int i = 1; i < stockNamesStr.Length; i++)
                {
                    string symbol = stockNamesStr[i].ToUpper();
                    List<StockDataSet<StockDataBase>> sets;

                    // Check if a stock data set already exists for the symbol
                    if(!segments.TryGetValue(symbol, out sets))
                    {
                        sets = new List<StockDataSet<StockDataBase>>();
                        segments.Add(symbol, sets);
                    }
                    foreach(StockDataSet<StockDataBase> preExistingSet in sets)
                    {
                        // Add to an existing set if they are for the same day
                        if(preExistingSet.Start.Date == fileStart.Date)
                        {
                            fileData.Add(preExistingSet);
                            break;
                        }
                    }
                    if(fileData.Count <= (i - 1))
                    {
                        // No matching set was found, so create a new one
                        StockDataSet<StockDataBase> newSet = new StockDataSet<StockDataBase>(symbol, fileStart, newFile);
                        newSet.DataSet.Resize(391);
                        var dataForSym = segments[symbol];
                        fileData.Add(newSet);
                        if(sets.Count > 0) newSet.Previous = sets[sets.Count - 1];
                        sets.Add(newSet);
                    }
                }

                DateTime lineTime = fileStart;
                while(!s.EndOfStream && (lineTime < fileEnd))
                {
                    string[] stockPricesStr = s.ReadLine().Split(new char[] { ',' });

                    // Ensure the entry contains all of the stocks
                    if((stockPricesStr.Length - 1) != fileData.Count)
                    {
                        continue;
                    }

                    // Get the time of the entry
                    DateTime newTime;
                    if(!DateTime.TryParse(stockPricesStr[0].Replace("\"", ""), out newTime)) continue;
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
                                // Sometimes the first price is corrupted, so skip it
                                if((newTime == fileStart)
                                   //&& ((fileData[i].Previous != null) && (Math.Abs((price / fileData[i][-1].Price) - 1.0f) > 0.01f))
                                   )
                                {
                                    continue;
                                }

                                if((price == 0.0f) || (newTime == fileEnd))
                                {
                                    price = fileData[i][fileData[i].Count - 1].Price;
                                    if(price == 0.0f)
                                    {
                                        continue;
                                    }
                                }
                                while(fileData[i].End <= newTime)
                                {
                                    fileData[i].DataSet.Add(new StockDataBase(price));
                                }
                            }
                        }
                    }
                }
            }

            // Filter the data before saving it
            FilterData(segments);

            // Save the old file to disk
            newFile.SetSegments(segments);
            newFile.Save(destination, typeof(StockDataBase), segments);

            return newFile;
        }
        #endregion

        #region Private Utilities
        /// <summary>
        /// Stores the stock data array to the stream
        /// </summary>
        /// <typeparam name="T">The stock data point type</typeparam>
        /// <param name="s">The stream to store the data to</param>
        /// <param name="val">The array to store</param>
        /// <param name="count">The number of elements to store</param>
        private static void Store<T>(Stream s, T[] val, int count) where T : struct
        {
            int structSize = Marshal.SizeOf(val[0]);
            byte[] data = new byte[structSize * count];
            IntPtr ptr = Marshal.AllocHGlobal(structSize * count);
            for(int i = 0; i < count; i++) Marshal.StructureToPtr(val[i], ptr + (i * structSize), true);
            Marshal.Copy(ptr, data, 0, data.Length);
            Marshal.FreeHGlobal(ptr);

            s.WriteByte((byte)(count >> 8));
            s.WriteByte((byte)(count & 0xFF));
            s.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Loads an array of stock data from the specified stream
        /// </summary>
        /// <typeparam name="T">The stock data type</typeparam>
        /// <param name="s">The stream to load from</param>
        /// <returns>The retrieved array of stock data</returns>
        private static T[] Load<T>(Stream s) where T : struct
        {
            int count = (s.ReadByte() << 8) | s.ReadByte();
            T[] structs = new T[count];
            if(count > 0)
            {
                int structSize = Marshal.SizeOf(structs[0]);
                int size = structSize * count;
                IntPtr ptr = Marshal.AllocHGlobal(size);
                byte[] data = new byte[size];
                s.Read(data, 0, size);
                Marshal.Copy(data, 0, ptr, size);
                for(int i = 0; i < count; i++) structs[i] = Marshal.PtrToStructure<T>(ptr + (i * structSize));
                Marshal.FreeHGlobal(ptr);
            }

            return structs;
        }
        #endregion
    }
}
