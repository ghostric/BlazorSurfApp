using HtmlAgilityPack;
using MapTestApp.Components.Models;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MapTestApp.Components.Handler
{
    public class NoaaBouyCallHandler
    {
        private readonly HttpClient httpClient;
        private readonly IDistributedCache distributedCache;

        public NoaaBouyCallHandler(HttpClient httpClient, IDistributedCache distributedCache)
        {
            
            this.httpClient = httpClient;
            this.distributedCache = distributedCache;
        }

        public async Task<List<string>> GetStationLinksAsync(string url)
        {
            var cachedData = await distributedCache.GetStringAsync(url);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<List<string>>(cachedData);
            }

            var stationLinks = new List<string>();
            var response = await httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);
            var links = doc.DocumentNode.SelectNodes("//a[contains(@href, 'station_page.php')]");

            if (links != null)
            {
                foreach (var link in links)
                {
                    string href = link.GetAttributeValue("href", string.Empty);
                    if (!string.IsNullOrEmpty(href) && href.StartsWith("station_page.php?station=46"))
                    {
                        string stationUrl = "https://www.ndbc.noaa.gov/" + href;
                        stationLinks.Add(stationUrl);
                    }
                }
            }

            var cacheEntryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));
            await distributedCache.SetStringAsync(url, JsonSerializer.Serialize(stationLinks), cacheEntryOptions);

            return stationLinks;
        }

        public static double[] ParseFirstTwoNumbers(string input)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(input, @"(\d+(\.\d+)?)[°\s]*([NSEW])");
            double[] result = new double[2];

            for (int i = 0; i < matches.Count; i++)
            {
                double value = double.Parse(matches[i].Groups[1].Value);
                string direction = matches[i].Groups[3].Value;

                if (direction == "S" || direction == "W")
                {
                    value = -value;
                }

                result[i] = value;
            }

            return result;
        }

        private static double ParseDouble(string input)
        {
            var cleanedInput = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());
            return double.TryParse(cleanedInput, out var result) ? result : 0;
        }

        public async Task<BouyMetaData> GetStationDataAsync(string stationUrl)
        {
            var cachedData = await distributedCache.GetStringAsync(stationUrl);
            if (cachedData != null)
            {
                return JsonSerializer.Deserialize<BouyMetaData>(cachedData);
            }

            try
            {
                BouyConditions bouyConditions = new BouyConditions();
                BouyMetaData bouyMetaData = new BouyMetaData();
                var response = await httpClient.GetStringAsync(stationUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(response);

                var stationNameNode = doc.DocumentNode.SelectSingleNode("//h1");
                string stationName = stationNameNode?.InnerText.Trim() ?? "Station name not found";
                
                if (!stationName.Contains("CA")) return null;
                var reportsCheck = doc.DocumentNode.SelectSingleNode("//h3")?.InnerText.Trim();

                if (reportsCheck != null) { 
                    if(reportsCheck.Contains("No Recent Reports")) return null;
                } 

                var metaDataNode = doc.GetElementbyId("stn_metadata");
                if (metaDataNode != null)
                {
                    var row = metaDataNode.SelectSingleNode(".//p");
                    var columns = row.SelectNodes(".//b");
                    if (columns != null && columns.Count > 1)
                    {
                        bouyMetaData.infoLink = columns[0].InnerText.Trim();
                        bouyMetaData.bouyModel = columns[1].InnerText.Trim();
                        if (bouyMetaData.bouyModel.Contains("Data Provided by"))
                        {
                            bouyMetaData.infoLink += columns[1].InnerText.Trim();
                            bouyMetaData.bouyModel = columns[2].InnerText.Trim();
                            bouyMetaData.latlng = ParseFirstTwoNumbers(columns[3].InnerText.Trim());
                        }
                        else
                        {
                            bouyMetaData.latlng = ParseFirstTwoNumbers(columns[2].InnerText.Trim());
                        }
                        if (bouyMetaData.latlng[0] == 0 && bouyMetaData.latlng[1] == 0)
                        {
                            bouyMetaData.latlng = ParseFirstTwoNumbers(columns[3].InnerText.Trim());
                        }

                        bouyMetaData.stationID = stationName;
                        bouyMetaData.dateTime = DateTime.Now;
                    }
                }
                else
                {
                    throw new Exception($"No metadata found for {stationName}");
                }

                var currentObsTable = doc.DocumentNode.SelectSingleNode("//table[@class='currentobs']");
                if (currentObsTable != null)
                {
                    var waveHeightNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Wave Height (WVHT)')]//following-sibling::td");
                    if (waveHeightNode != null) bouyConditions.waveHeight = ParseDouble(waveHeightNode.InnerText);

                    var dominantWavePeriodNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Dominant Wave Period (DPD)')]//following-sibling::td");
                    if (dominantWavePeriodNode != null) bouyConditions.dominantWavePRD = ParseDouble(dominantWavePeriodNode.InnerText);

                    var averageWavePeriodNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Average Wave Period (APD)')]//following-sibling::td");
                    if (averageWavePeriodNode != null) bouyConditions.avgWavePRD = ParseDouble(averageWavePeriodNode.InnerText);

                    var meanWaveDirectionNode = currentObsTable.SelectSingleNode(".//td[contains(text(), 'Mean Wave Direction (MWD)')]//following-sibling::td");
                    if (meanWaveDirectionNode != null) bouyConditions.meanWaveDR = meanWaveDirectionNode.InnerText.Trim();

                    var waterTemperatureNode = currentObsTable.SelectSingleNode(".//td[contains(text(), ' Water Temperature (WTMP):')]//following-sibling::td");
                    if (waterTemperatureNode != null) bouyConditions.waterTemp = ParseDouble(waterTemperatureNode.InnerText);
                }

                bouyMetaData.conditions = bouyConditions;

                var cacheEntryOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));
                await distributedCache.SetStringAsync(stationUrl, JsonSerializer.Serialize(bouyMetaData), cacheEntryOptions);

                return bouyMetaData;
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
                throw;
            }
        }

        public async Task<List<BouyMetaData>> FetchDataForAllStations()
        {
            try
            {
                string baseStationPageUrl = "https://www.ndbc.noaa.gov/to_station.shtml";
                List<BouyMetaData> bouyMarkers = new List<BouyMetaData>();
                var stationLinks = await GetStationLinksAsync(baseStationPageUrl);
                var tasks = stationLinks.Select(GetStationDataAsync).ToList();
                var results = await Task.WhenAll(tasks);
                bouyMarkers.AddRange(results.Where(bouy => bouy != null));
                return bouyMarkers;
            }
            catch (Exception ex)
            {
               Log.Information(ex.ToString() );
                throw;
            }
        }
    }
}
