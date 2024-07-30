export function Display(MarkerList) {
    let map = L.map('map').setView([35.105779, -118.340030], 5);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    var swell = MarkerList[0];
    
    var marker = L.marker(swell.latlng).addTo(map);

    marker.bindPopup(swell.dateTime + "<br>" + swell.direction + "<br>" + swell.esh).openPopup();

    return "";
}

