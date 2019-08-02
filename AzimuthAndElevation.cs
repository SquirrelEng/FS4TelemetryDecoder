using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////////
/// <summary>
/// /// The following code has been adapted from javascript written by Don Cross
/// http://cosinekitty.com/compass.html
/// 
/// -- de KJ6FO D. Gibson
/// 
/// The various Classes below should be refactored and consolodated?
/// </summary>
////////////////////////////////////////////////////////////////////////////////////////////
///

namespace FS4TelemetryDecoder
{
    // Refactor classes. Too much overlap. Combine classes.

    class AZELDist
    {
        public double Azimuth { get; set; }
        public double ElevationAngle { get; set; }
        public double StraightLineDistanceKm { get; set; }
    }

    public class PointXYZNXNYNZR
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public double nx { get; set; }
        public double ny { get; set; }
        public double nz { get; set; }

        public double radius { get; set; }

    }

    public class PointXYZR
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

        public double radius { get; set; }

    }

    public class PointXYZ
    {
        public double x { get; set; }
        public double y { get; set; }
        public double z { get; set; }

    }

    class AzimuthAndElevation
    {
        /// <summary>
        /// Calculate Az , Elevation Angle and Distance (Km) from two 3d points

        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static AZELDist CalculateAZEL(Point3D a, Point3D b)
        {
            AZELDist rc = new AZELDist();

            var ap = LocationToPoint(a);
            var bp = LocationToPoint(b);
            var distKm = 0.001 * DistanceStraightLine(ap, bp);


            // Let's use a trick to calculate azimuth:
            // Rotate the globe so that point A looks like latitude 0, longitude 0.
            // We keep the actual radii calculated based on the oblate geoid,
            // but use angles based on subtraction.
            // Point A will be at x=radius, y=0, z=0.
            // Vector difference B-A will have dz = N/S component, dy = E/W component.                
            var br = RotateGlobe(b, a, bp.radius, ap.radius);
            if (br.z * br.z + br.y * br.y > 1.0e-6)
            {
                var theta = Math.Atan2(br.z, br.y) * 180.0 / Math.PI;
                rc.Azimuth = 90.0 - theta;
                if (rc.Azimuth < 0.0)
                {
                    rc.Azimuth += 360.0;
                }
                if (rc.Azimuth > 360.0)
                {
                    rc.Azimuth -= 360.0;
                }

            }

            var bma = NormalizeVectorDiff(bp, ap);
            if (bma != null)
            {
                // Calculate altitude, which is the angle above the horizon of B as seen from A.
                // Almost always, B will actually be below the horizon, so the altitude will be negative.
                // The dot product of bma and norm = cos(zenith_angle), and zenith_angle = (90 deg) - altitude.
                // So altitude = 90 - acos(dotprod).
                rc.ElevationAngle = 90.0 - (180.0 / Math.PI) * Math.Acos(bma.x * ap.nx + bma.y * ap.ny + bma.z * ap.nz);

            }

            rc.StraightLineDistanceKm = distKm;
            return rc;

        }

        public static double EarthRadiusInMeters(double latitudeRadians)      // latitude is geodetic, i.e. that reported by GPS
        {
            // http://en.wikipedia.org/wiki/Earth_radius
            var a = 6378137.0;  // equatorial radius in meters
            var b = 6356752.3;  // polar radius in meters
            var cos = Math.Cos(latitudeRadians);
            var sin = Math.Sin(latitudeRadians);
            var t1 = a * a * cos;
            var t2 = b * b * sin;
            var t3 = a * cos;
            var t4 = b * sin;
            return Math.Sqrt((t1 * t1 + t2 * t2) / (t3 * t3 + t4 * t4));
        }

        private static double GeocentricLatitude(double lat)
        {
            // Convert geodetic latitude 'lat' to a geocentric latitude 'clat'.
            // Geodetic latitude is the latitude as given by GPS.
            // Geocentric latitude is the angle measured from center of Earth between a point and the equator.
            // https://en.wikipedia.org/wiki/Latitude#Geocentric_latitude
            var e2 = 0.00669437999014;
            var clat = Math.Atan((1.0 - e2) * Math.Tan(lat));
            return clat;
        }

        private static PointXYZNXNYNZR LocationToPoint(Point3D c)
        {
            PointXYZNXNYNZR xyz = new PointXYZNXNYNZR();

            // Convert (lat, lon, elv) to (x, y, z).
            var lat = c.Lat * Math.PI / 180.0;
            var lon = c.Lon * Math.PI / 180.0;
            var radius = EarthRadiusInMeters(lat);
            var clat = GeocentricLatitude(lat);

            var cosLon = Math.Cos(lon);
            var sinLon = Math.Sin(lon);
            var cosLat = Math.Cos(clat);
            var sinLat = Math.Sin(clat);
            var x = radius * cosLon * cosLat;
            var y = radius * sinLon * cosLat;
            var z = radius * sinLat;

            // We used geocentric latitude to calculate (x,y,z) on the Earth's ellipsoid.
            // Now we use geodetic latitude to calculate normal vector from the surface, to correct for elevation.
            var cosGlat = Math.Cos(lat);
            var sinGlat = Math.Sin(lat);

            var nx = cosGlat * cosLon;
            var ny = cosGlat * sinLon;
            var nz = sinGlat;

            x += c.AltM * nx;
            y += c.AltM * ny;
            z += c.AltM * nz;

            xyz.x = x;
            xyz.y = y;
            xyz.z = z;
            xyz.radius = radius;
            xyz.nx = nx;
            xyz.ny = ny;
            xyz.nz = nz;


            return xyz;
        }

        private static double DistanceStraightLine(PointXYZNXNYNZR ap, PointXYZNXNYNZR bp)
        {
            var dx = ap.x - bp.x;
            var dy = ap.y - bp.y;
            var dz = ap.z - bp.z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static PointXYZR RotateGlobe(Point3D b, Point3D a, double bradius, double aradius)
        {
            // Get modified coordinates of 'b' by rotating the globe so that 'a' is at lat=0, lon=0.

            Point3D br = new Point3D(b.Lat, b.Lon - a.Lon, b.AltM);

            var brp = LocationToPoint(br);

            // Rotate brp cartesian coordinates around the z-axis by a.lon degrees,
            // then around the y-axis by a.lat degrees.
            // Though we are decreasing by a.lat degrees, as seen above the y-axis,
            // this is a positive (counterclockwise) rotation (if B's longitude is east of A's).
            // However, from this point of view the x-axis is pointing left.
            // So we will look the other way making the x-axis pointing right, the z-axis
            // pointing up, and the rotation treated as negative.

            var alat = -a.Lat * Math.PI / 180.0;
            if (true)
            {
                alat = GeocentricLatitude(alat);
            }
            var acos = Math.Cos(alat);
            var asin = Math.Sin(alat);

            var bx = (brp.x * acos) - (brp.z * asin);
            var by = brp.y;
            var bz = (brp.x * asin) + (brp.z * acos);

            PointXYZR rc = new PointXYZR();
            rc.x = bx;
            rc.y = by;
            rc.z = bz;
            rc.radius = bradius;
            return rc;

        }

        public static PointXYZR NormalizeVectorDiff(PointXYZNXNYNZR b, PointXYZNXNYNZR a)
        {
            // Calculate norm(b-a), where norm divides a vector by its length to produce a unit vector.
            var dx = b.x - a.x;
            var dy = b.y - a.y;
            var dz = b.z - a.z;
            var dist2 = dx * dx + dy * dy + dz * dz;
            if (dist2 == 0)
            {
                return null;
            }
            var dist = Math.Sqrt(dist2);

            PointXYZR rc = new PointXYZR();
            rc.x = dx / dist;
            rc.y = dy / dist;
            rc.z = dz / dist;
            rc.radius = 1.0;


            return rc;
        }

    }
}
