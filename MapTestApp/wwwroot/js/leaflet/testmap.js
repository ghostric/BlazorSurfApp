export function Display(MarkerList) {
    let map = L.map('map').setView([35.105779, -118.340030], 5);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    var swell = MarkerList[0];
    
    var marker = L.marker(swell.latlng).addTo(map);

    marker.bindPopup(swell.dateTime + "<br>" + swell.direction + "<br>" + swell.esh).openPopup();

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

    return "";
}

