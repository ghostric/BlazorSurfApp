namespace MapTestApp.Components.Models
{
    public class Swell
    {
        public double[] latlng { get; set; } // recording of swell location
        public int direction { get; set; }  // swell band height
        public double esh { get; set; }  // estimated surf height
        public DateTime dateTime { get; set; }
    }
}
