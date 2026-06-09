# CleanMap

CleanMap is a dark, neon-accented pollution reporting map that runs entirely in the browser. Reports are stored in localStorage under the key `cleanmap_v1`.

## Features

- Map with red markers for dirty reports and green markers for cleaned reports
- In-app camera capture (rear camera preferred)
- Geolocation and map centering
- Waste type tags and report details
- Cleanup proof with an after photo
- Progress tracking for cleaned vs total reports

## Run locally

Camera and geolocation require a secure context. `localhost` is accepted by browsers, so use a local server.

```bash
cd frontend/cleanmap
python3 serve.py
```

Then open http://localhost:8000 in a browser.

## Optional C# API

The frontend will auto-detect the .NET API at `http://localhost:5210` or `https://localhost:7210`. When found, reports are saved to the API instead of localStorage.

```bash
cd server/API
dotnet run
```

The API persists reports in `cleanmap-db.json` alongside the API project.

## Data persistence

If the API is not running, all data is stored in localStorage. To reset, clear site data or remove the `cleanmap_v1` key in DevTools.
