using MapTestApp.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MapTestApp.Components.Handler;

namespace MapTestApp.Components.Pages
{
    public partial class WeatherReportForm
    {
        public bool IsLoading { get; set; } = false;
        private string? errorMessage;
    }
}