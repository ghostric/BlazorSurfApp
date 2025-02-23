namespace MapTestApp.Components.Models
{
    public class BouyMetaData
    {
        public string infoLink { get; set; }
        public double[] latlng { get; set; } // bouy location
        public string stationID { get; set; }  // ID of bouy
        public string bouyModel { get; set; }  // identify bouy type for data model
        public DateTime dateTime { get; set; }
        public BouyConditions conditions { get; set; } // station data
    }
}
