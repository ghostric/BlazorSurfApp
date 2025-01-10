using HtmlAgilityPack;
using MapTestApp.Components.Models;
using System.Threading.RateLimiting;

namespace MapTestApp.Components.Handler
{
    public class NoaaBouyCallHandler
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // Function to get station links from the NDBC station page
        public static async Task<List<string>> GetStationLinksAsync(string url)
        {
            var stationLinks = new List<string>();

            // Fetch the HTML content of the page
            var response = await httpClient.GetStringAsync(url);

            // Parse the HTML content
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Extract all links to individual station pages (assuming station links contain "station_page.php")
            var links = doc.DocumentNode.SelectNodes("//a[contains(@href, 'station_page.php')]");

            if (links != null)
            {
                foreach (var link in links)
                {
                    string href = link.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href))
                    {
                        // Complete the URL by appending the station URL
                        string stationUrl = "https://www.ndbc.noaa.gov/" + href;
                        stationLinks.Add(stationUrl);
                    }
                }
            }

            return stationLinks;
        }

        // Function to get data for a specific station by its URL
        public static async Task GetStationDataAsync(string stationUrl)
        {
            // Fetch the HTML content of the station page
            var response = await httpClient.GetStringAsync(stationUrl);

            // Parse the HTML content
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Extract specific data from the page
            
            var stationNameNode = doc.DocumentNode.SelectSingleNode("//h1");
            string stationName = stationNameNode?.InnerText.Trim() ?? "Station name not found"; // extract station name from parsed string

            var metaDataNode = doc.GetElementbyId("stn_metadata");

            if (metaDataNode != null)
            {
                //Extract the text inside the metadata section
                string metaDataText = metaDataNode.InnerText.Trim();
                Console.WriteLine($"Metadata for {stationName}: {metaDataText}");
                // Utilize xpath to further parse into idividual parts from metadata
                /*
                 * 
                 */
            }
            else
            {
                Console.WriteLine($"No metadata found for {stationName}");
            }

            Console.WriteLine($"Station Data for {stationNameNode?.InnerText}");

            
        }

        // Main function to fetch and display data for all stations
        public static async Task FetchDataForAllStations()
        {
            string baseStationPageUrl = "https://www.ndbc.noaa.gov/to_station.shtml";

            // Step 1: Get all the station links
            var stationLinks = await GetStationLinksAsync(baseStationPageUrl);

            // Step 2: For each station link, fetch and display its data
            foreach (var stationLink in stationLinks)
            {
                await GetStationDataAsync(stationLink);
                Console.WriteLine();
            }
        }
    }
}
