using HtmlAgilityPack;
using MapTestApp.Components.Models;
using MapTestApp.Components.Pages;
using System.Threading.RateLimiting;

namespace MapTestApp.Components.Handler
{
    public class NoaaBouyCallHandler
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public NoaaBouyCallHandler() { }

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
                        // Only add links for stations starting with "46"
                        if (href.StartsWith("station_page.php?station=46"))
                        {
                            // Complete the URL by appending the station URL
                            string stationUrl = "https://www.ndbc.noaa.gov/" + href;
                            stationLinks.Add(stationUrl);
                        }
                    }
                }
            }

            return stationLinks;
        }

        //public static async Task<List<string>> GetStationLinksForCaliforniaAsync(string url)
        //{
        //    var stationLinks = new List<string>();

        //    // Fetch the HTML content of the page
        //    var response = await httpClient.GetStringAsync(url);

        //    // Parse the HTML content
        //    var doc = new HtmlDocument();
        //    doc.LoadHtml(response);

        //    // Find all <h2> elements containing "California" (since there can be multiple)
        //    var californiaHeaders = doc.DocumentNode.Descendants("h2")
        //        .Where(node => node.InnerText.Contains("California"))
        //        .ToList();

        //    foreach (var californiaHeader in californiaHeaders)
        //    {
        //        // For each "California" header, find the corresponding <div> with class "station-links"
        //        var californiaDiv = californiaHeader
        //            .NextSibling // move to the next sibling, which could be the div
        //            .Descendants("div")
        //            .FirstOrDefault(div => div.GetAttributeValue("class", "").Contains("station-links"));

        //        if (californiaDiv != null)
        //        {
        //            // Now, extract all station links within this div
        //            var californiaStationLinks = californiaDiv
        //                .Descendants("a")
        //                .Where(a => a.GetAttributeValue("href", "").Contains("station_page.php"))
        //                .ToList();

        //            foreach (var link in californiaStationLinks)
        //            {
        //                string href = link.GetAttributeValue("href", string.Empty);
        //                if (!string.IsNullOrEmpty(href))
        //                {
        //                    // Complete the URL by appending the station URL base
        //                    string stationUrl = "https://www.ndbc.noaa.gov/" + href;
        //                    stationLinks.Add(stationUrl);
        //                }
        //            }
        //        }
        //    }

        //    return stationLinks;
        //}

        public static double[] ParseFirstTwoNumbers(string input)
        {
            // Extract all numbers from the string using a regular expression
            var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\d+(\.\d+)?");

            // Take the first two matches and convert them to doubles
            double[] result = matches
                .Cast<System.Text.RegularExpressions.Match>()
                .Take(2)  // Take only the first two numbers
                .Select(match => double.Parse(match.Value))
                .ToArray();

            return result;
        }

        private static double ParseDouble(string input)
        {
            // Remove non-numeric characters (e.g., 'ft', 'sec', '°F') and parse the remaining number
            var cleanedInput = new string(input.Where(c => char.IsDigit(c) || char.IsPunctuation(c)).ToArray());
            return double.TryParse(cleanedInput, out var result) ? result : 0;
        }

        // Function to get data for a specific station by its URL
        public static async Task<BouyMetaData> GetStationDataAsync(string stationUrl)
        {
            try
            {
                BouyConditions bouyConditions = new BouyConditions();
                BouyMetaData bouyMetaData = new BouyMetaData();

                // Fetch the HTML content of the station page
                var response = await httpClient.GetStringAsync(stationUrl);

                // Parse the HTML content
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                // Extract specific data from the page

                var stationNameNode = doc.DocumentNode.SelectSingleNode("//h1");
                string stationName = stationNameNode?.InnerText.Trim() ?? "Station name not found"; // extract station name from parsed string
                bool isCA = stationName.Contains("CA");
                if (!isCA) /* we only care about california bouys on this pass */
                {
                    return null;
                }

                var metaDataNode = doc.GetElementbyId("stn_metadata");

                if (metaDataNode != null)
                {
                    // Extract all metadata rows (each <tr> under the metadata section)
                    var row = metaDataNode.SelectSingleNode(".//p");
                    var columns = row.SelectNodes(".//b");
                        if (columns != null && columns.Count > 1)
                        {
                            bouyMetaData.infoLink = columns[0].InnerText.Trim();
                            bouyMetaData.bouyModel = columns[1].InnerText.Trim();
                            bouyMetaData.latlng = ParseFirstTwoNumbers(columns[2].InnerText.Trim()); // Parses out lat long data into array for plotting
                            bouyMetaData.stationID = stationName;
                            bouyMetaData.dateTime = DateTime.Now;
                        }
                }
                else
                {
                    throw new Exception($"No metadata found for {stationName}");
                }

                //if(bouyMetaData.bouyModel != "Waverider Buoy")
                //{
                //    Console.WriteLine("non wave-rider model");
                //    return bouyMetaData;
                //}

                // Find the table with the class "currentobs"
                var currentObsTable = doc.DocumentNode.SelectSingleNode("//table[@class='currentobs']");

                if (currentObsTable != null)
                {
                    // Extract Wave Height
                    var waveHeightNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Wave Height (WVHT)')]//following-sibling::td");
                    if (waveHeightNode != null)
                    {
                        bouyConditions.WaveHeight = ParseDouble(waveHeightNode.InnerText);
                    }

                    // Extract Dominant Wave Period
                    var dominantWavePeriodNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Dominant Wave Period (DPD)')]//following-sibling::td");
                    if (dominantWavePeriodNode != null)
                    {
                        bouyConditions.dominantWavePRD = ParseDouble(dominantWavePeriodNode.InnerText);
                    }

                    // Extract Average Wave Period
                    var averageWavePeriodNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Average Wave Period (APD)')]//following-sibling::td");
                    if (averageWavePeriodNode != null)
                    {
                        bouyConditions.avgWavePRD = ParseDouble(averageWavePeriodNode.InnerText);
                    }

                    // Extract Mean Wave Direction
                    var meanWaveDirectionNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Mean Wave Direction (MWD)')]//following-sibling::td");
                    if (meanWaveDirectionNode != null)
                    {
                        bouyConditions.meanWaveDR = meanWaveDirectionNode.InnerText.Trim();
                    }

                    // Extract Water Temperature
                    var waterTemperatureNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Water Temperature (WTMP)')]//following-sibling::td");
                    if (waterTemperatureNode != null)
                    {
                        bouyConditions.waterTemp = ParseDouble(waterTemperatureNode.InnerText);
                    }
                }

                bouyMetaData.conditions = bouyConditions;

                return bouyMetaData;
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                throw;
            }
            
        }

        // Main function to fetch and display data for all stations
        public async Task<List<BouyMetaData>> FetchDataForAllStations()
        {
            try
            {
                string baseStationPageUrl = "https://www.ndbc.noaa.gov/to_station.shtml";
                List<BouyMetaData> bouyMarkers = new List<BouyMetaData>();
                // Step 1: Get all the station links
                var stationLinks = await GetStationLinksAsync(baseStationPageUrl);

                // Step 2: For each station link, fetch and display its data
                foreach (var stationLink in stationLinks)
                {
                    var bouy = await GetStationDataAsync(stationLink);
                    if (bouy != null) {
                        bouyMarkers.Add(bouy);
                    }
                }
                return bouyMarkers;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
