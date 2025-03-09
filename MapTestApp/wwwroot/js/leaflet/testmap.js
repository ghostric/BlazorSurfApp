export function Display(MarkerList) {
    // Initialize the map
    let map = L.map('map').setView([35.105779, -118.340030], 5);

    // Add OpenStreetMap tile layer
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    // Loop through all markers in MarkerList
    MarkerList.forEach(swell => {
        if (swell.latlng && swell.latlng.length === 2) {
            // Add marker to the map
            var marker = L.marker(swell.latlng).addTo(map);

            // Bind popup to the marker
            marker.bindPopup(`
                <b>Station ID:</b> ${swell.stationID}<br>
                <b>DateTime:</b> ${new Date(swell.dateTime).toLocaleString()}<br>
                <b>Wave Height:</b> ${swell.conditions.waveHeight ? swell.conditions.waveHeight : "N/A" } ft<br>
                <b>Dominant Wave Period:</b> ${swell.conditions.dominantWavePRD} sec<br>
                <b>Average Wave Period:</b> ${swell.conditions.avgWavePRD} sec<br>
                <b>Mean Wave Direction:</b> ${swell.conditions.meanWaveDR}<br>
                <b>Water Temperature:</b> ${swell.conditions.waterTemp} °F
            `).openPopup();
        } else {
            console.error("Invalid latlng data for swell:", swell);
        }
    });

    // Add video overlay
    var videoUrls = [
        'https://cdn.star.nesdis.noaa.gov/GOES18/GLM/SECTOR/wus/EXTENT3/GOES18-WUS-EXTENT3-1000x1000.mp4'
    ];

    var errorOverlayUrl = 'https://cdn-icons-png.flaticon.com/512/110/110686.png';
    var latLngBounds = L.latLngBounds([34, -118]);

    var videoOverlay = L.videoOverlay(videoUrls, latLngBounds, {
        opacity: 0.6,
        errorOverlayUrl: errorOverlayUrl,
        interactive: true,
        autoplay: true,
        muted: true,
        playsInline: true
    }).addTo(map);

    videoOverlay.getElement().pause();

    // Add custom controls for video overlay
    videoOverlay.on('load', function () {
        var MyPauseControl = L.Control.extend({
            onAdd: function () {
                var button = L.DomUtil.create('button');
                button.title = 'Pause';
                button.innerHTML = '<span aria-hidden="true">⏸</span>';
                L.DomEvent.on(button, 'click', function () {
                    videoOverlay.getElement().pause();
                });
                return button;
            }
        });
        var MyPlayControl = L.Control.extend({
            onAdd: function () {
                var button = L.DomUtil.create('button');
                button.title = 'Play';
                button.innerHTML = '<span aria-hidden="true">▶️</span>';
                L.DomEvent.on(button, 'click', function () {
                    videoOverlay.getElement().play();
                });
                return button;
            }
        });

        var pauseControl = (new MyPauseControl()).addTo(map);
        var playControl = (new MyPlayControl()).addTo(map);
    });
}