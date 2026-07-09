
import { useState } from "react";
import { MapContainer, Marker, TileLayer, useMap } from "react-leaflet";

function ChangeMapView({ position }) {
  const map = useMap();

  if (position) {
    map.setView(position, 15);
  }

  return null;
}

function getCity(address) {
  return (
    address.city ||
    address.town ||
    address.village ||
    address.municipality ||
    address.county ||
    ""
  );
}

export default function StudioLocationPicker({ onLocationSelect }) {
  const [searchText, setSearchText] = useState("");
  const [results, setResults] = useState([]);
  const [selectedPosition, setSelectedPosition] = useState([42.6977, 23.3219]);
  const [selectedAddress, setSelectedAddress] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSearch() {
    setErrorMessage("");
    setResults([]);

    if (!searchText.trim()) {
      setErrorMessage("Please enter a studio address.");
      return;
    }

    try {
      setIsLoading(true);

      const response = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&addressdetails=1&limit=5&q=${encodeURIComponent(
          searchText
        )}`
      );

      if (!response.ok) {
        setErrorMessage("Could not search address.");
        return;
      }

      const data = await response.json();
      setResults(data);

      if (data.length === 0) {
        setErrorMessage("No addresses found.");
      }
    } catch {
      setErrorMessage("Address search failed.");
    } finally {
      setIsLoading(false);
    }
  }

  function handleSelectLocation(result) {
    const latitude = Number(result.lat);
    const longitude = Number(result.lon);
    const address = result.address || {};

    const location = {
      studioAddress: result.display_name,
      studioCity: getCity(address),
      studioCountry: address.country || "",
      studioLatitude: latitude,
      studioLongitude: longitude,
    };

    setSelectedPosition([latitude, longitude]);
    setSelectedAddress(result.display_name);
    setResults([]);

    onLocationSelect(location);
  }

  return (
    <div className="location-picker">
      <label>Studio location</label>

      <div className="location-search-row">
        <input
          type="text"
          placeholder="Search studio address..."
          value={searchText}
          onChange={(e) => setSearchText(e.target.value)}
        />

        <button type="button" onClick={handleSearch} disabled={isLoading}>
          {isLoading ? "Searching..." : "Search"}
        </button>
      </div>

      {errorMessage && <p className="form-error">{errorMessage}</p>}

      {results.length > 0 && (
        <div className="location-results">
          {results.map((result) => (
            <button
              key={`${result.lat}-${result.lon}`}
              type="button"
              className="location-result-item"
              onClick={() => handleSelectLocation(result)}
            >
              {result.display_name}
            </button>
          ))}
        </div>
      )}

      {selectedAddress && (
        <p className="selected-location">
          Selected location: <strong>{selectedAddress}</strong>
        </p>
      )}

      <div className="map-wrapper">
        <MapContainer
          center={selectedPosition}
          zoom={13}
          scrollWheelZoom={false}
          className="studio-map"
        >
          <ChangeMapView position={selectedPosition} />

          <TileLayer
            attribution='&copy; OpenStreetMap contributors'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />

          <Marker position={selectedPosition} />
        </MapContainer>
      </div>
    </div>
  );
}