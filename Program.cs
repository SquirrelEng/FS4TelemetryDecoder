using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Configuration;

namespace FS4TelemetryDecoder
{

    public class Program
    {
        private FileSystemWatcher LogWatcher;
        private string LogDirectoryPath;


        private DateTime LastTelemetryUpdate;
        private DateTime LastTelemetryCheck;
        private DateTime FlightTimeStart;

        private Point3D ReferencePosition; // Position used to determine Downrange dist, AZ/EL etc.
        private DisplayUnitsEnum DisplayUnits;
        Dictionary<FS4MESSAGETYPE, FT8LogEntry> CurrentLogs;
        Dictionary<FS4MESSAGETYPE, FT8LogEntry> PrevLogs;

        private string StatusMessage { get; set; }

        private static int SchedulerLastMinute = -1;



        // Consts
        private const int Column0 = 0;
        private const int Column1 = 10;
        private const int Column2 = 38;
        private const int Column3 = 58;
        private const int Column4 = 74;

        private const int Type1Line = 2;
        private const int Type2Line = 7;
        private const int Type3Line = 14;
        private const int Type4Line = 21;

        private const int MessageLine = 25;
        private const int FunctionKeysLine = 26;

        private const string Type2LogFileName = "FS4_Position.csv";
        private const string Type3LogFileName = "FS4_Data.csv";

        public enum DisplayUnitsEnum
        {
            Metric,
            English
        };


        /// <summary>
        /// CTor
        /// </summary>
        public Program()
        {

            CurrentLogs = new Dictionary<FS4MESSAGETYPE, FT8LogEntry>();
            PrevLogs = new Dictionary<FS4MESSAGETYPE, FT8LogEntry>();

            var Lat = Properties.Settings.Default.ReferenceLat;
            var Lon = Properties.Settings.Default.ReferenceLon;
            var AltM = Properties.Settings.Default.ReferenceAltM;
            ReferencePosition = new Point3D(Lat,Lon,AltM);

            DisplayUnits = DisplayUnitsEnum.Metric;
            FlightTimeStart = Properties.Settings.Default.FlightTimeStart; // DateTime.MinValue;
        }


        /// <summary>
        /// Main startup.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            Logger.Write("FS4 Telemetry Decoder v1.0");

            FormatTime(DateTime.Now);
            if (args.Length == 0)
            {
                Logger.Write("Missing Log file path on command line.");
                return;
            }


            //TEST CASE REMOVE
            Point3D MtBaldy = new Point3D(34.28889, -117.6458, 3067.5072);
            Point3D MtWilson = new Point3D(34.22389, -118.0603, 1740.408);
            var azel = AzimuthAndElevation.CalculateAZEL(MtBaldy, MtWilson);
            //Mt.Baldy at(34.28889°; 117.6458°; 10,064') from Mt. Wilson at (34.22389°; 118.0603°; 5,710')
            // Expected Az el & Distance
            // Az = 259.4292;
            // El = −2.1299
            // Dist = 38.890 km

            var Dist2 = HaversineDistKm(MtBaldy, MtWilson);

            //TEST CASE REMOVE

            var pgm = new Program();
            pgm.Run(args);

        }


        /// <summary>
        /// Main line of program as an object instance.
        /// </summary>
        /// <param name="args"></param>
        private void Run(string[] args)
        {
            LogDirectoryPath = args[0];
            Logger.Write("Monitoring log file path: {0}", LogDirectoryPath);

            LogWatcher = new FileSystemWatcher();
            LogWatcher.Path = LogDirectoryPath;
            LogWatcher.NotifyFilter = NotifyFilters.LastWrite;
            LogWatcher.Filter = "*.TXT";
            LogWatcher.Created += new FileSystemEventHandler(OnChanged);
            LogWatcher.Changed += new FileSystemEventHandler(OnChanged);
            
            LogWatcher.EnableRaisingEvents = true;

            for (; ; ) // Forever loop
            {
                UpdateDisplay();
                Scheduler();
                Thread.Sleep(1000);
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo ch = Console.ReadKey(true);
                    switch (ch.Key)
                    {
                        case ConsoleKey.Escape: //Quit
                            {
                                // Wait a short bit
                                Thread.Sleep(500);
                                if (Console.KeyAvailable)
                                {
                                    if (ch.Key == ConsoleKey.Escape)
                                    {
                                        return;
                                    }
                                }
                            }
                            break;

                       

                        case ConsoleKey.F2: // Toggle Display Units
                            {
                                if(DisplayUnits == DisplayUnitsEnum.Metric)
                                {
                                    DisplayUnits = DisplayUnitsEnum.English;
                                }
                                else
                                {
                                    DisplayUnits = DisplayUnitsEnum.Metric;
                                }
                            }
                            break;

                        case ConsoleKey.F3: // Start Flight Clock
                            {
                                
                                if ((ch.Modifiers & ConsoleModifiers.Alt) != 0) // Reset Clock after it is running
                                {
                                    StartFlightClock();
                                    Message("Inflight clock started.");
                                }
                                else if ((ch.Modifiers & ConsoleModifiers.Control) != 0) // Clear Clock
                                {
                                    ClearFlightClock();
                                    Message("Inflight clock cleared.");
                                }
                                else if (FlightTimeStart == DateTime.MinValue) // Initial setting of flight clock
                                {
                                    StartFlightClock();
                                    Message("Inflight clock started.");
                                }
                                else // Nothing done
                                { 
                                    Console.Beep();
                                    Message("Inflight clock already started. Use Alt-F2 to reset.");
                                }
                               
                            }
                            break;

                        case ConsoleKey.F4: // Set Reference Position
                            {
                                if ((ch.Modifiers & ConsoleModifiers.Alt) != 0)  // Reset Reference Position after being previously established
                                {
                                    SetReferencePosition();
                                    Message("Reference point set");
                                }
                                else if ((ch.Modifiers & ConsoleModifiers.Control) != 0) // Clear ref position
                                {
                                    ClearReferencePosition();
                                    Message("Reference point cleared");
                                }
                                else if (!ReferencePosition.IsValid) // Initial setting of reference clock
                                {
                                    SetReferencePosition();
                                    Message("Reference point set");
                                }
                                else // Nothing done
                                {
                                    Console.Beep();
                                    Message("Reference not set. No data yet.");
                                }
                            }
                            break;


                        case ConsoleKey.F12: // Clear All Settings
                            {
                                if ((ch.Modifiers & ConsoleModifiers.Control) != 0 &&
                                    (ch.Modifiers & ConsoleModifiers.Alt) != 0)
                                {
                                    ClearReferencePosition();
                                    ClearFlightClock();
                                    File.Delete(Type2LogFileName);
                                    File.Delete(Type3LogFileName);
                                    Message("Data Cleared");
                                }
                            }
                            break;
                    }



                }
            }
        }

        private void Scheduler()
        {
            

            DateTime now = DateTime.UtcNow; //What time is now?
            int Min = now.Minute;
                        
            // In this program there is only one item to schedule so this is a very simple scheduler.
            // It only runs the APRS call one time every odd minute. This avoids missing a schedule as seconds could creep
            // to the point where a second could get skipped and thus a schule could be missed.

            if(Min % 2 == 1) // Odd minute is the time to do stuff.
            {
                if(Min != SchedulerLastMinute)
                {
                    SchedulerLastMinute = Min; // Save the minute so this will only execute once per minute.
                    if (Properties.Settings.Default.APRS_Enable)
                    {
                        SendAPRSAsync();
                    }
                }                    
            }
            
        }


        // Are we flying yet? Use the Flight Clock to determine.
        bool InFlight
        {
            get
            {
                return  FlightTimeStart == DateTime.MinValue ? false : true;
            }
        }

        private void StartFlightClock()
        {
            FlightTimeStart = DateTime.UtcNow;
            Properties.Settings.Default.FlightTimeStart = FlightTimeStart;
            Properties.Settings.Default.Save();
            Logger.Write("Flight Clock Started {0:HH:mm:ss}", FlightTimeStart);
        }

        private void ClearFlightClock()
        {
            FlightTimeStart = DateTime.MinValue;
            Properties.Settings.Default.FlightTimeStart = FlightTimeStart;
            Properties.Settings.Default.Save();
            Logger.Write("Flight Clock Cleared.");
        }



        /// <summary>
        /// Fires when a log file changes. Only responds to ALL.TXT changes
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.Name.ToUpper() == "ALL.TXT")
            {
                ProcessTelemetry(e.FullPath);
                Logger.Write("Telemetry proccessed.");
            }          
        }


        private void SetReferencePosition()
        {
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type2))
            {
                var log = CurrentLogs[FS4MESSAGETYPE.Type2];
                //double Lat = 0.0;
                //double Lon = 0.0;
                //uint AltM = 0;
                Point3D pt = new Point3D();
                DecodeType2Telemetry(log.Message, pt);

                ReferencePosition = pt;

                // Save Reference Point
                Properties.Settings.Default.ReferenceLat = ReferencePosition.Lat;
                Properties.Settings.Default.ReferenceLon = ReferencePosition.Lon;
                Properties.Settings.Default.ReferenceAltM = ReferencePosition.AltM;
                Properties.Settings.Default.Save();

                Logger.Write("Reference Position Set {0}", ReferencePosition);

            }
            else // No reference available yet, set to empty point so it will be ignored.
            {
                ReferencePosition = new Point3D();
                Console.Beep();
            }

        }

        
        private void ClearReferencePosition()
        {
            ReferencePosition = new Point3D();
            Properties.Settings.Default.ReferenceLat = ReferencePosition.Lat;
            Properties.Settings.Default.ReferenceLon = ReferencePosition.Lon;
            Properties.Settings.Default.ReferenceAltM = ReferencePosition.AltM;
            Properties.Settings.Default.Save();
            Logger.Write("Reference Position Cleared.");
        }


        // Put message on the display.
        public void Message(string msg)
        {
            StatusMessage = msg;
        }


        /// <summary>
        /// Draws the display with current data
        /// </summary>
        private void UpdateDisplay()
        {

            NormalColor();
            TopLine();
            DisplayType1Data();
            DisplayType2Data();
            DisplayType3Data();
            DisplayType4Data();

            Console.SetCursorPosition(Column0, MessageLine);
            Console.Write("Message: {0}",StatusMessage);

            FunctionKeysDisplay();

        }

        private void FunctionKeysDisplay()
        {
            NormalColor();

            Console.SetCursorPosition(Column0, FunctionKeysLine);
            InverseColor();
            Console.Write("F2 - Units");
            NormalColor();
            Console.Write("  ");


            InverseColor();
            Console.Write("F3 - Flight Timer");
            NormalColor();
            Console.Write("  ");

            InverseColor();
            Console.Write("F4 - Ref. Point");
            NormalColor();
            Console.Write("  ");

            InverseColor();
            Console.Write("Ctrl+=Clear");
            NormalColor();
            Console.Write("  ");

            InverseColor();
            Console.Write("Alt+=Reset");
            NormalColor();
            Console.Write("  ");

            InverseColor();
            Console.Write("Ctrl-Alt-F12 - Reset All");
            NormalColor();
            Console.Write("  ");

        }

        private static void InverseColor()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }

        private static void NormalColor()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        private void TopLine()
        {
            Console.Clear();
           
            Console.Write("UTC: {0:HH:mm:ss yyyy-MM-dd}", DateTime.UtcNow);
            //Console.Write("\t");
            //Console.Write("Local:{0:hh:mm:sst yyyy-MM-dd}", DateTime.Now);
            //Console.Write("\t");
            Console.SetCursorPosition(30, 0);
            Console.Write("Flight Time: ");
            if (FlightTimeStart > DateTime.MinValue)
            {
                TimeSpan ts = DateTime.UtcNow - FlightTimeStart;
                Console.Write("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
            }
            else
            {
                Console.Write("Not Set");
            }
            Console.SetCursorPosition(56, 0);
            Console.Write("Next: {0:HH:mm:ss} ({1}s)", NextTimeSlot(), SecondsToGo(NextTimeSlot()));

            Console.SetCursorPosition(80, 0);
            Console.Write("Last Data: ");
            Console.Write(FormatTime(LastTelemetryUpdate));
            Console.Write(" ");
            Console.Write(FormatTimeAgo(LastTelemetryUpdate));
        }


        private void DisplayType1Data()
        {
            int Line = Type1Line;
            Console.SetCursorPosition(0,Line);
            Console.Write("Msg Type 1 Station ID: ");
            
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type1))
            {
                Line++;
                var log = CurrentLogs[FS4MESSAGETYPE.Type1];
                DisplayHeader(Line,log);              
            }
            else
            {
                Console.Write("No Data");
            }
        }

        private void DisplayType2Data()
        {
            int Line = Type2Line;
            Console.SetCursorPosition(0, Line);
            Console.Write("Msg Type 2 Position & Alt: ");
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type2))
            {
                Line++;
                var log = CurrentLogs[FS4MESSAGETYPE.Type2];
                var prevlog = PrevLogs[FS4MESSAGETYPE.Type2];
                DisplayHeader(Line,log);
                Line += 2;

                Point3D CurrPos = new Point3D();
                DecodeType2Telemetry(log.Message, CurrPos);

                Point3D PrevPos = new Point3D();
                DecodeType2Telemetry(prevlog.Message, PrevPos);

                int DeltaAltM = (int)CurrPos.AltM - (int)PrevPos.AltM;
                var ts = log.LogDateTime - prevlog.LogDateTime;

                double mps = 0.0; // Meters per sec
                double fps = 0.0; // Feet per sec
                if (ts.TotalSeconds > 1) // Avoid a divide by Zero. Seconds should be 2 mins apart
                {
                    mps = DeltaAltM / ts.TotalSeconds;
                    fps = Conversions.Meters2Feet(DeltaAltM) / ts.TotalSeconds;
                }

                if (DisplayUnits == DisplayUnitsEnum.Metric)
                {
                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("Pos: {0:0.00000},{1:0.00000}",CurrPos.Lat, CurrPos.Lon);

                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("Alt: {0:#,##0}m",CurrPos.AltM);

                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("\u03B4Alt: {0:#,##0}m", DeltaAltM); // \u0394 Should be correct

                    Console.SetCursorPosition(Column4, Line);
                    Console.Write("VRate: {0:0.00}m/s", mps);

                }
                else if (DisplayUnits == DisplayUnitsEnum.English)
                {

                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("Pos: {0:0.00000},{1:0.00000}", CurrPos.Lat, CurrPos.Lon);

                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("Alt: {0:#,##0}ft", Conversions.Meters2Feet(CurrPos.AltM));

                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("Alt\u0394: {0:#,##0}ft", Conversions.Meters2Feet(DeltaAltM));

                    Console.SetCursorPosition(Column4, Line);
                    Console.Write("VRate: {0:0.00}ft/s", fps);

                }

                Line++;
                double GCDistKM = 0;
                if (ReferencePosition.IsValid)
                {
                    var azel = AzimuthAndElevation.CalculateAZEL(ReferencePosition, CurrPos);
                    GCDistKM = HaversineDistKm(ReferencePosition, CurrPos);
                    if (GCDistKM < 0.5) // Less thah 1/2 Km, switch to Meters
                    {
                        double Meters = GCDistKM * 1000.0;

                        Console.SetCursorPosition(Column1, Line);
                        if (DisplayUnits == DisplayUnitsEnum.Metric)
                        {                            
                            Console.Write("Down Range: {0:0.0}m ", Meters);
                        }
                        else if (DisplayUnits == DisplayUnitsEnum.English)
                        {
                            double Feet = Conversions.Meters2Feet(Meters);
                            Console.Write("Down Range: {0:0.0}ft ", Feet);
                        }                            
                    }
                    else
                    {
                        Console.SetCursorPosition(Column1, Line);
                        if (DisplayUnits == DisplayUnitsEnum.Metric)
                        {                            
                            Console.Write("Down Range: {0:0.0}km", GCDistKM);
                        }
                        else if (DisplayUnits == DisplayUnitsEnum.English)
                        {
                            double Miles = Conversions.Kilometers2Miles(GCDistKM);
                            Console.Write("Down Range: {0:0.0}mi", Miles);
                        }                       
                    }

                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("Az: {0:0.0}°", azel.Azimuth);

                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("El: {0:0.0}°", azel.ElevationAngle);
                }
                else
                {
                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("Down Range: Reference point not set.");
                }
            }
            else
            {
                Console.Write("No Data");
            }
        }

       

        private void DisplayType3Data()
        {
            int Line = Type3Line;
            Console.SetCursorPosition(0, Line);
            Console.Write("Msg Type 3 Payload Data: ");
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type3))
            {
                Line++;
                var log = CurrentLogs[FS4MESSAGETYPE.Type3];
                DisplayHeader(Line,log);
                Line += 2;

                double dOutsideT = 0.0;
                double dInsideT = 0.0;                
                double dVolts = 0.0;
                double dhAcc = 0.0;
                double dvAcc = 0.0;
                double dGroundSpeedMPH = 0.0;

                DecodeType3Telemetry(log.Message, out dOutsideT, out dInsideT, out dVolts,
                    out dhAcc, out dvAcc, out dGroundSpeedMPH);

                if (DisplayUnits == DisplayUnitsEnum.Metric)
                {
                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("OutT: {0:0.0}°C", dOutsideT);
                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("InT: {0:0.0}°C", dInsideT);
                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("Volts: {0:0.0}v",  dVolts);

                    Line++;

                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("Speed: {0:0.0}Kph", Conversions.Miles2Kilometers(dGroundSpeedMPH));
                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("hAcc: {0:0.0}m", dhAcc);
                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("vAcc: {0:0.0}m", dvAcc);

                }
                else if (DisplayUnits == DisplayUnitsEnum.English)
                {
                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("OutT: {0:0.0}°F", Conversions.C2F(dOutsideT));
                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("InT: {0:0.0}°F", Conversions.C2F(dInsideT));
                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("Volts: {0:0.0}v", dVolts);

                    Line++;

                    Console.SetCursorPosition(Column1, Line);
                    Console.Write("Speed: {0:0.0}Mph", dGroundSpeedMPH);
                    Console.SetCursorPosition(Column2, Line);
                    Console.Write("hAcc: {0:0.0}ft", Conversions.Meters2Feet(dhAcc));
                    Console.SetCursorPosition(Column3, Line);
                    Console.Write("vAcc: {0:0.0}ft", Conversions.Meters2Feet(dvAcc));
                }
            }
            else
            {
                Console.Write("No Data");
            }
        }

        private void DisplayType4Data()
        {
            int Line = Type4Line;
            Console.SetCursorPosition(0, Line);
            Console.Write("Msg Type 4 Message: ");
            Line++;
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type4))
            {
                var log = CurrentLogs[FS4MESSAGETYPE.Type4];
                DisplayHeader(Line,log);
            }
            else
            {
                Console.Write("No Data");
            }
        }

        private static void DisplayHeader(int Line,FT8LogEntry log)
        {
            Console.SetCursorPosition(Column1, Line);
            Console.Write("At:{0}", FormatTime(log.DecodeTimeStamp));
            Console.SetCursorPosition(Column2, Line);
            Console.Write("{0}", FormatTimeAgo(log.DecodeTimeStamp));

            Line++;
            Console.SetCursorPosition(Column1, Line);
            Console.Write("Time: {0}", log.LogTime);
            Console.SetCursorPosition(Column2, Line);
            Console.Write("Sig: {0}dB", log.DB);
            Console.SetCursorPosition(Column3, Line);
            Console.Write("DT: {0}", log.DT);
            Console.SetCursorPosition(Column4, Line);
            Console.Write("Freq: {0}", log.Freq);           
        }

       
        public DateTime NextTimeSlot()
        {
            DateTime dt = DateTime.UtcNow;
            int NewSecs = dt.Second;
            int Min = dt.Minute;
            int AddMinutes = 0;

            if (Min % 2 == 1) // Odd minute  
            {
                AddMinutes = 1; // Skip to the next even minute
                NewSecs = 0;
            }
            // Even minutes
            else if (NewSecs < 15)
            {
                NewSecs = 15;
            }
            else if (NewSecs < 30)
            {
                NewSecs = 30;
            }
            else if (NewSecs < 45)
            {
                NewSecs = 45;
            }
            else // Skip to the next even minute
            {
                AddMinutes = 2;
                NewSecs = 0;
            }

            dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, Min, NewSecs, DateTimeKind.Utc);
            dt = dt.AddMinutes(AddMinutes);

            return dt;
        }

        

        /// <summary>
        /// Process Telemetry file update
        /// </summary>
        /// <param name="FileName"></param>
        private void ProcessTelemetry(string FileName)
        {
            LastTelemetryCheck = DateTime.UtcNow;

            Thread.Sleep(1000); // Wait a sec and let WSJT finish logging. Have been running into file conflicts.

            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(FileName);
            }
            catch (Exception ex)
            {
                Logger.Write("Process Telemetery Exception: {0}",ex.Message);
                Logger.Write("Process Telemetery Stack Trace: {0}", ex.StackTrace);
                return;
            }

            // Look for FS4 Telem - Working Backwards
            int n = lines.Count();
            for (int i = n; i > 0; i--)
            {
                string str = lines[i-1];

                FT8LogEntry logentry = new FT8LogEntry();
                bool b = IsFS4Telemetry(str, logentry);  
                if (b)
                {
                    // Check to see if this is the same as the previous logentry item. This could happen if there is no FS4 telemtery decoded, but
                    // another signal is logged. In that case the first logentry we would find is the prior one already logged. Check for this
                    // condition.
                    if (CurrentLogs.ContainsKey(logentry.Type)) // if not the first of this type 
                    {
                        var priorlog = CurrentLogs[logentry.Type];
                        if(priorlog.LogDateTime == logentry.LogDateTime) // if the most current log is the same as we just decoded, its a dupe, move on..
                        {
                            // Nothing new to see, bail out. We did not find a new log entry.
                            break;
                        }
                    }

                    // We have a new Logentry, so log it into the system.
                    if (CurrentLogs.ContainsKey(logentry.Type)) // if not the first one of this type
                    {
                        PrevLogs[logentry.Type] = CurrentLogs[logentry.Type]; // Move current to prev                        
                        CurrentLogs[logentry.Type] = logentry; // Update current entry
                    }
                    else // 1st time, add log entry to both dictionaries.
                    {
                        //CurrentLogs.Add(logentry.Type, logentry);
                        CurrentLogs[logentry.Type] = logentry;
                        PrevLogs[logentry.Type] = logentry; // Prev is same as current 1st time
                    }
                    LastTelemetryUpdate = DateTime.UtcNow; // Insert Type entry


                    // Write Type2 & Type3 info to log files.
                    if(logentry.Type == FS4MESSAGETYPE.Type2)
                    {
                        WriteType2Log(logentry);
                        Logger.Write("Type2 data saved to CSV file.");
                    }
                    else if (logentry.Type == FS4MESSAGETYPE.Type3)
                    {
                        WriteType3Log(logentry);
                        Logger.Write("Type3 data saved to CSV file.");
                    }

                    break; // We found it, so quit looking.
                }
            }

            return;

        }

        private async Task SendAPRSAsync()
        {
            if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type2)) // If we have a Type2 Log entry
            {
                APRSData telem = new APRSData();
                var LogType2 = CurrentLogs[FS4MESSAGETYPE.Type2];
                if(LogType2.SentToAPRS==true) // Was this log already sent to APRS?
                {                    
                    // Yes it was, then bail out, nothing new to do.
                    return;
                }

                //Point3D CurrPos = new Point3D();
                DecodeType2Telemetry(LogType2.Message, telem.Position);
                telem.TimeStampUTC = LogType2.LogDateTime;

                // Now look for Type 3 data which is optional.                           
                if (CurrentLogs.ContainsKey(FS4MESSAGETYPE.Type3))
                {
                    double dOutsideT = 0.0;
                    double dInsideT = 0.0;
                    double dVolts = 0.0;
                    double dhAcc = 0.0;
                    double dvAcc = 0.0;
                    double dGroundSpeedMPH = 0.0;
                    var LogType3 = CurrentLogs[FS4MESSAGETYPE.Type3];
                    DecodeType3Telemetry(LogType3.Message, out dOutsideT, out dInsideT, out dVolts,
                        out dhAcc, out dvAcc, out dGroundSpeedMPH);

                    telem.OutsideTC = dOutsideT;
                    telem.InsideTC = dInsideT;
                    telem.Volts = dVolts;

                    string Message;
                    string SymbolTable;
                    string Symbol;                     
                    string Callsign;
                    string APRSUserCallSign = Properties.Settings.Default.APRS_UserCallsign;
                    string APRSPassword = Properties.Settings.Default.APRS_Password; // PW is same for all SSIDs of callsign http://n5dux.com/ham/aprs-passcode/
                    if (InFlight)
                    {
                        Callsign = Properties.Settings.Default.APRS_FlightCallsignSSID; // ex. "KJ6FO-11"    
                        Message = Properties.Settings.Default.APRS_FlightMessage;  // ex."Flying Squirrel 4 HAB"                        
                        SymbolTable = Properties.Settings.Default.APRS_FlightSymbolTable; // ex. "/"
                        Symbol = Properties.Settings.Default.APRS_FlightSymbol; // ex. "O" Balloon

                    }
                    else
                    {
                        Callsign = Properties.Settings.Default.APRS_GroundCallsignSSID; // ex. "KJ6FO-10"
                        Message = Properties.Settings.Default.APRS_GroundMessage; // ex. "Ground Station."
                        SymbolTable = Properties.Settings.Default.APRS_GroundSymbolTable; // ex. "/" 
                        Symbol = Properties.Settings.Default.APRS_GroundSymbol; // ex. "k" Pickup truck                     
                    }

                    await APRSIS.UpdateAsync(telem, Message, Callsign, APRSUserCallSign,APRSPassword, SymbolTable, Symbol);

                    LogType2.SentToAPRS = true; // This log was sent to APRS.
                   
                    StatusMessage = string.Format("APRS Sent @ {0:HH:mm:ss}", DateTime.UtcNow);
                }
            }
            else
            {
                // Nothing to do until we get a position report.
            }
        }


        // Write Position data to CSV file
        private void WriteType2Log(FT8LogEntry logentry)
        {
            Point3D pt = new Point3D();
            DecodeType2Telemetry(logentry.Message, pt);

            string logline = string.Format("{0:dd/MM/yyyy HH:mm:ss},{1:0.00000},{2:0.00000},{3}", logentry.LogDateTime, pt.Lat, pt.Lon, pt.AltM);

            bool bFileExists = File.Exists(Type2LogFileName);
            using (StreamWriter w = File.AppendText(Type2LogFileName))
            {
                if(!bFileExists) // New file so add a header line first
                {
                    w.WriteLine("TimeUTC,Lattitude,Longitude,AltitudeM");
                }
                w.WriteLine(logline);
            }
        }

        // Write Payload data to CSV file
        private void WriteType3Log(FT8LogEntry logentry)
        {            
            double dOutsideT = 0.0;
            double dInsideT = 0.0;
            double dVolts = 0.0;
            double dhAcc = 0.0;
            double dvAcc = 0.0;
            double dGroundSpeedMPH = 0.0;

            DecodeType3Telemetry(logentry.Message,  out dOutsideT, out dInsideT, out dVolts,
                out dhAcc, out dvAcc, out dGroundSpeedMPH);

            string logline = string.Format("{0:yyyy/MM/dd HH:mm:ss},{1},{2:0.0},{3:0.0},{4:0.0},{5:0.0},{6:0.0},{7:0.0}", logentry.LogDateTime,
                logentry.DB,dOutsideT, dInsideT, dVolts, dhAcc, dvAcc, dGroundSpeedMPH);

            bool bFileExists = File.Exists(Type3LogFileName);
            using (StreamWriter w = File.AppendText(Type3LogFileName))
            {
                if (!bFileExists) // New file so add a header line first
                {
                    w.WriteLine("TimeUTC,SignalDB,OutsideTempC,InsideTempC,Volts,hAcc,vAcc,GroundSpeedMPH");
                }
                w.WriteLine(logline);
            }
        }


        private bool IsFS4Telemetry(string str, FT8LogEntry logentry)
        {
            bool brc = false; // Assume str is FS4 Telem.

            // Array Indexes
            const int TIME = 0;
            const int DB = 4;
            const int DT = 5;
            const int FREQ = 6;
            const int PROTOCOL = 3;
            const int MESSAGE = 7;

            try // Put into a try block because odd things may happen in the log file and we dont want to crash onthis.
            {
                var Fields = str.Split(new char[] { ' ' }, 8, StringSplitOptions.RemoveEmptyEntries);
                if (Fields.Count() < 8)// Not a log entry, so not FS4 Data.
                {
                    return false;
                }

                if (Fields[PROTOCOL] == "FT8")  // Has to be FT8 Protocol
                {
                    FS4MESSAGETYPE Type = GetFS4MessageType(Fields[MESSAGE]);
                    logentry.Type = Type;
                    logentry.DecodeTimeStamp = DateTime.UtcNow;
                    logentry.LogTimeStamp = Fields[TIME];
                    logentry.DB = Fields[DB];
                    logentry.DT = Fields[DT];
                    logentry.Freq = Fields[FREQ];
                    logentry.Message = Fields[MESSAGE];

                    if (Type == FS4MESSAGETYPE.None)
                    {
                        brc = false; // Not an FS4 Message
                    }
                    else
                    {
                        brc = true; // It is FS4 telemetry
                    }
                }
            }
            catch(Exception ex) // Log the error and move on.
            {
                Logger.Write("IsFS4Telemetry Exception {0}. str={1}.",ex.Message,str);
                Logger.Write("Stack Trace.");
                Logger.Write(ex.StackTrace);
            }
            
            return brc;
        }


        /// <summary>
        /// Check if this is a FS4 Message and return its message type.
        /// If this is not an FS4 message, it will return type None.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private FS4MESSAGETYPE GetFS4MessageType(string msg)
        {
            FS4MESSAGETYPE rc = FS4MESSAGETYPE.None;

            if (msg == Properties.Settings.Default.Type1Message) // Fixed message string Type1
            {
                rc = FS4MESSAGETYPE.Type1;
            }
            else if (msg == Properties.Settings.Default.Type4Message) // Fixed message string Type4
            {
                rc = FS4MESSAGETYPE.Type4;
            }
            else // Check for Hex Telemtry
            {
                if (msg.Length == 18) // FS4 Hex Telemetry is always 18 digits long
                {
                    if (msg[0] == '2') // Type 2 Message
                    {
                        rc = FS4MESSAGETYPE.Type2;
                    }
                    else if (msg[0] == '3') // Type 2 Message
                    {
                        rc = FS4MESSAGETYPE.Type3;
                    }
                }
            }

            return rc;
        }

        /// <summary>
        /// Formats the time and "ago" time or N/A is not valid.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static string FormatTime(DateTime dt)
        {
            string rc;

            if (dt != DateTime.MinValue)
            {
                rc = string.Format("{0:HH:mm:ss}", dt);
            }
            else
            {
                rc = "N/A";
            }
            return rc;
        }

        private static string FormatTimeAgo(DateTime dt)
        {
            string rc = "";

            if (dt != DateTime.MinValue)
            {
                TimeSpan ts = DateTime.UtcNow - dt;
                rc = string.Format("{0:00}h:{1:00}m:{2:00}s ago",
                       ts.Hours, ts.Minutes, ts.Seconds);
            }
            return rc;
        }


        private int SecondsToGo(DateTime dt)
        {
            TimeSpan ts = dt - DateTime.UtcNow;
            return (int)Math.Round(ts.TotalSeconds, 0, MidpointRounding.AwayFromZero);
        }


        private void DecodeType2Telemetry(string HexStr, Point3D pt)
        {          
            string Type = HexStr.Substring(0, 1); // First digit represent telemetry message type
            string HexStr1 = HexStr.Substring(1, 1); // 4 bits of data
            string HexStr2 = HexStr.Substring(2); // 64 Bits of data

            byte Bits4 = byte.Parse(HexStr1, System.Globalization.NumberStyles.HexNumber);
            ulong Bits64 = ulong.Parse(HexStr2, System.Globalization.NumberStyles.HexNumber);

            pt.AltM = (uint)(Bits64 & 0x000000000000FFFF);  //16 Bits
            int Lon = (int)((Bits64 & 0x000003FFFFFF0000) >> 16); //26 Bits
            int Lat = (int)((Bits64 & 0xFFFFFC0000000000) >> 42); // 22 bits
            Lat |= (int)((Bits4 & 0x7) << 22); // 3 Bits

            // Unshift lat & lon
            Lat -= 9000000;
            Lon -= 18000000;

            // Convert Lat/Lon to doubles 5 digit precision.
            pt.Lat = Lat / 1e5;
            pt.Lon = Lon / 1e5;
        }


        private void DecodeType3Telemetry(string HexStr,  out double dOutsideT, out double dInsideT, out double dVolts,
            out double dhAcc, out double dvAcc, out double dGroundSpeed)
        {

            string Type = HexStr.Substring(0, 1); // First digit represent telemetry message type
            string HexStr1 = HexStr.Substring(1, 1); // 4 bits of data
            string HexStr2 = HexStr.Substring(2); // 64 Bits of data

            byte Bits4 = byte.Parse(HexStr1, System.Globalization.NumberStyles.HexNumber);
            ulong Bits64 = ulong.Parse(HexStr2, System.Globalization.NumberStyles.HexNumber);

            long InsideT = (long)(Bits64 & 0x000000000000FFFF);
            long OutsideT = (long)((Bits64 & 0x00000000FFFF0000) >> 16);
            byte Volts = (byte)((Bits64 & 0x000000FF00000000) >> 32);
            byte hAcc = (byte)((Bits64 & 0x0000FF0000000000) >> 40);
            byte vAcc = (byte)((Bits64 & 0x00FF000000000000) >> 48);
            uint GSpeed = (uint)((Bits64 & 0xFF00000000000000) >> 56);
            GSpeed |= (uint)((Bits4 & 0x7) << 8);

            // Covert from 10ths to normal units
            dInsideT = InsideT / 10.0;
            dOutsideT = OutsideT / 10.0;
            dVolts = Volts / 10.0;
            dhAcc = hAcc / 10.0;
            dvAcc = vAcc / 10.0;
            dGroundSpeed = GSpeed / 10.0;
        }


        /// <summary>
        /// Haversine Great Circle calculation of distance between two points.
        ///  Returns: The distance between coordinates 36.12,-86.67 and 33.94,-118.4 is: 
        ///     2887.25995060711
        /// </summary>        
        /// <returns>Distance in Km</returns>

        public static double HaversineDistKm(Point3D pt1, Point3D pt2)
        {
            return HaversineDistKm(pt1.Lat, pt1.Lon, pt2.Lat, pt2.Lon);
        }


        public static double HaversineDistKm(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6372.8; // In kilometers
            var dLat = toRadians(lat2 - lat1);
            var dLon = toRadians(lon2 - lon1);
            lat1 = toRadians(lat1);
            lat2 = toRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
            var c = 2 * Math.Asin(Math.Sqrt(a));
            return R * 2 * Math.Asin(Math.Sqrt(a));
        }


        public static double toRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}
