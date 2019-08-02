using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;



namespace FS4TelemetryDecoder
{
    public class APRSData
    {
        public APRSData()
        {
            TimeStampUTC = DateTime.MinValue;
            Position = new Point3D();
            OutsideTC = null;
            InsideTC = null;
            Volts = null;
        }
        public DateTime TimeStampUTC { get; set; }  
        public Point3D Position { get; set; }
        public double? OutsideTC { get; set; }
        public double? InsideTC { get; set; }
        public double? Volts { get; set; }
    }


    class APRSIS
    {
        const int RETRIES = 3;

        // Aync version of update call, use to avoid blocking the UI of the caller.
        public async static Task UpdateAsync(APRSData telem, string Message, string Callsign,String APRSUserCallSign, string APRSPassword, string SymbolTable, string Symbol)
        {
            await Task.Run(() =>
            {
                Update(telem, Message, Callsign, APRSUserCallSign,APRSPassword, SymbolTable, Symbol);
            });            
        }

        // Send an APRS update over APRS-IS
        // Password can be created here: http://n5dux.com/ham/aprs-passcode/
        public static void Update(APRSData telem,string Message,string Callsign,string APRSUserCallSign,string APRSPassword,string SymbolTable,string Symbol)
        {
            
            // Encode APRS Message
            string APRSMsg = EncodeAPRSMessage(telem, Message, SymbolTable, Symbol);
            Logger.Write("APRS Message={0}",APRSMsg);

            Logger.Write("Sending to server.");

            // Occasionally a server will not allow connections, Try up to three times.
            for (int i = 0; i < RETRIES; i++) //Make a number of  atttempts to connect to server
            {
                if (Send(APRSMsg,Callsign, APRSUserCallSign,APRSPassword))
                {
                    break;
                }

                Logger.Write("Retry.");
            }
        }


        // Encode the APRS Message to send
        private static string EncodeAPRSMessage(APRSData telem, string Comment, string SymbolTable, string Symbol)
        {
            string msg = "";
            
            string TimeStamp = string.Format("{0:ddHHmm}z", telem.TimeStampUTC);

            string NS = telem.Position.Lat > 0 ? "N" : "S";
            telem.Position.Lat = Math.Abs((double)telem.Position.Lat);
            double LatDegs = Math.Truncate((double)telem.Position.Lat);
            double LatRemainder = (double)telem.Position.Lat - LatDegs;
            double LatMins = LatRemainder * 60.0;

            string LatStr = string.Format("{0:00}{1:00.00}{2}", LatDegs, LatMins, NS);

            string EW = (double)telem.Position.Lon > 0 ? "E" : "W";
            telem.Position.Lon = Math.Abs((double)telem.Position.Lon);
            double LonDegs = Math.Truncate((double)telem.Position.Lon);
            double LonRemainder = (double)telem.Position.Lon - LonDegs;
            double LonMins = LonRemainder * 60.0;

            string LonStr = string.Format("{0:000}{1:00.00}{2}", LonDegs, LonMins, EW);


            string Position = string.Format("{0}{1}{2}{3}", LatStr, SymbolTable, LonStr, Symbol);

            // Create the minimum message
            msg = string.Format("@{0}{1}", TimeStamp, Position);

            uint AltFeet = (uint)(Conversions.Meters2Feet(telem.Position.AltM)); // Convert to feet
            String AltitudeStr = string.Format("/A={0:000000}", AltFeet); // Leading Zero Pad to 6 digits in feet
            msg = string.Format("{0}{1}", msg, AltitudeStr);
            
            if (telem.InsideTC != null) // Add InternalTemp C to comment section
            {
                msg = string.Format("{0} IT={1}c", msg, telem.InsideTC);
            }
            if (telem.OutsideTC != null) // Add External Temp C to comment section
            {
                msg = string.Format("{0} ET={1}c", msg, telem.OutsideTC);
            }

            if (telem.Volts != null) // Add volts to comment section
            {
                msg = string.Format("{0} B={1}v", msg, telem.Volts);
            }

            msg = string.Format("{0} {1}", msg, Comment);

            return msg;
        }


        // Send the message to the APRS-IS network
        private static bool Send(string msg,string Callsign,string APRSUserCallSign,string APRSPassword)
        {
            Logger.Write("Sending to APRS-IS");
            bool brc = false;
            string ServerName = "rotate.aprs.net"; //Load balanced domain name. Will give you various IPs addresses over time.
            int portNumber = 14580; // This is a filtered port and is available on (all?/most) servers. Will keep recieve rtraffic to a mimimum. We dont need to recieve anything.

            byte[] SendBuffer;  // Send buffer
            byte[] ReceiveBytes = new byte[1024]; // Receive buffer

            IPAddress destAddress = null;

            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(ServerName);  // Get the host details
                if (hostEntry.AddressList.Length > 0)
                {
                    destAddress = hostEntry.AddressList[0]; // Get the hosts IP address
                }
                else
                {
                    Logger.Write("Host not found - aborting");
                    return false;
                }

                Logger.Write("Connecting to {0}", destAddress.ToString());
                IPEndPoint remoteEP = new IPEndPoint(destAddress, portNumber); // Connect to host
                Socket sender = new Socket(destAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                sender.Connect(remoteEP);
                Logger.Write("Connected to {0}.", sender.RemoteEndPoint.ToString());

                string SendMessage = string.Format("user {0} pass {1} vers GroundSquirrel 2.0\n{2}>APRS,TCPIP*:{3}\n", APRSUserCallSign, APRSPassword,Callsign, msg);
                SendBuffer = Encoding.ASCII.GetBytes(SendMessage);
                int bytesSent = sender.Send(SendBuffer);

                Logger.Write("Sent {0} bytes to {1}", bytesSent, destAddress.ToString());

                // Receive the response from the remote device.  Just to show we did something, not really needed.
                int bytesRec = sender.Receive(ReceiveBytes);
                string recdata = Encoding.ASCII.GetString(ReceiveBytes, 0, bytesRec);
                recdata = recdata.Replace("\r", "").Replace("\n", ""); //Replace Newline and return (In any order) with nothing so log wont skip a line.
                Logger.Write("Received data = {0}", recdata);

                // Finish up
                Logger.Write("Closing the socket.");
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                brc = true;
            }
            catch (ArgumentNullException ane)
            {
                Logger.Write("ArgumentNullException : {0}", ane.Message);
            }
            catch (SocketException se)
            {
                Logger.Write("SocketException : {0}", se.Message);
            }
            catch (Exception e)
            {
                Logger.Write("Unexpected exception : {0}", e.Message);
                Logger.Write("Stack Trace : {0}", e.StackTrace);
            }

            return brc;
        }
    }
}
