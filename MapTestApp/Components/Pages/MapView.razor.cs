using MapTestApp.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MapTestApp.Components.Handler;
using Serilog;

namespace MapTestApp.Components.Pages
{
    public partial class MapView
    {
        private IJSObjectReference? mapModule;
        public List<BouyMetaData> Swells = new List<BouyMetaData>();
        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        [Inject]
        public NoaaBouyCallHandler BouyCallHandler { get; set; }
        public bool IsLoading { get; set; } = false;
        private string? errorMessage;

        public MapView()
        {
        }

        public async Task PageLoading()
        {
            IsLoading = true;
            await Task.Delay(1);
            try
            {
                Swells = await BouyCallHandler.FetchDataForAllStations();
            }
            catch (Exception ex)
            {
                errorMessage = ex.ToString();
                Log.Error(ex.ToString());
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await PageLoading();
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    mapModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/leaflet/testmap.js");
                    if (mapModule != null && Swells != null)
                    {
                        await mapModule.InvokeAsync<string>("Display", Swells);
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = ex.ToString();
                    Log.Error(ex.ToString());
                }
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (mapModule is not null)
            {
                try
                {
                    await mapModule.DisposeAsync();
                }
                catch (Exception ex)
                {
                    errorMessage = ex.ToString();
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}
