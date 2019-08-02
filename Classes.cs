using System;

namespace FS4TelemetryDecoder
{
    // Enums
    public enum FS4MESSAGETYPE
    {
        None,
        Type1,
        Type2,
        Type3,
        Type4
    };


    public class Point2D
    {
        public Point2D(double Lat, double Lon)
        {
            this.Lat = Lat;
            this.Lon = Lon;
        }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }

    public class Point3D
    {
        public Point3D()
        {
            Lat = 0.0;
            Lon = 0.0;
            AltM = double.MinValue; // Set to minimum value, used to detect if point is initialized and valid
        }

        public override string ToString()
        {
            string rc = "Invalid";
            if (IsValid)
            {
                rc = string.Format("{0},{1} {2}M", Lat, Lon, AltM);
            }

            return rc;
        }

        public Point3D(double Lat, double Lon, double AltM)
        {
            this.Lat = Lat;
            this.Lon = Lon;
            this.AltM = AltM;
        }

        public Point2D ToPoint2D()
        {
            return new Point2D(Lat, Lon);
        }

        public double Lat { get; set; }
        public double Lon { get; set; }
        public double AltM { get; set; } // Altitude in Meters.

        public bool IsValid
        {
            get
            {
                // Minvalue is not convenient to store in a settings file so use the threshold value of -1000 as a test.
                return AltM <= -1000 ? false : true;
            }
        }
    }

    public class FT8LogEntry
    {
        public FT8LogEntry()
        {
            SentToAPRS = false;
        }
        public FS4MESSAGETYPE Type { get; set; }
        public DateTime DecodeTimeStamp { get; set; } // Time message was decoded.. after completion of reception.
        public string LogTimeStamp { get; set; } // The ALL.TXT time (i.e. the start of the period when the xmit starts)
        public string LogTime // Time only HH:MM:SS format
        {
            get
            {
                string rc = "";
                int pos = LogTimeStamp.IndexOf("_");
                if (pos >= 0)
                {
                    rc = LogTimeStamp.Substring(pos + 1);
                    rc = rc.Insert(2, ":");
                    rc = rc.Insert(5, ":");

                }
                return rc;
            }
        }
        public DateTime LogDateTime  // LogTimeStamp as a DateTime type.
        {
            get
            {
                DateTime Now = System.DateTime.Now;
                int Hr = Convert.ToInt32(LogTimeStamp.Substring(7, 2));
                int Min = Convert.ToInt32(LogTimeStamp.Substring(9, 2));
                int Sec = Convert.ToInt32(LogTimeStamp.Substring(11, 2));
                DateTime rc = new DateTime(Now.Year, Now.Month, Now.Day, Hr, Min, Sec); 
                return rc;

            }
        }
        public string DB { get; set; }
        public string DT { get; set; }
        public string Freq { get; set; }
        public string Message { get; set; }

        //Indicates this Log Was used to send to APRS - To avoid duplication of sending. Only set on type2 message logs. because that
        // is the main telemetry message used in APRS. (Without a new type2 there is nothing to update APRS with.
        public bool SentToAPRS { get; set; }

    }

    public class Conversions
    {
        //////////////////////////////////////////////////////
        ///  Conversions
        ///  /////////////////////////////////////////////////
        public static double Kilometers2Miles(double GCDistKM)
        {
            return GCDistKM * 0.62137d;
        }

        public static double Miles2Kilometers(double Miles)
        {
            return Miles / 0.62137d;
        }

        public static double Meters2Feet(double Meters)
        {
            return Meters * 3.2808d;
        }

        //private static double Feet2Meters(double Feet)
        //{
        //    return Feet / 3.2808d;
        //}

        public static double C2F(double C)
        {
            double rc = 0;
            rc = C * 1.8000 + 32;
            return rc;
        }
    }
}
