# Setup Guide

## Prerequisites

| Requirement | Version |
|---|---|
| Unity | 2022.3 LTS or later |
| Python | 3.10 or later |
| Node.js (optional, tooling) | 18 LTS or later |
| Git LFS | 3.x |

### Supported XR Devices
- Meta Quest 2 / 3 / Pro
- HTC Vive / Vive Pro
- Microsoft HoloLens 2
- Desktop XR (mouse + keyboard simulation)

---

## 1. Clone the Repository

```bash
git clone https://github.com/sailgt/PORO-XR.git
cd PORO-XR
git lfs pull   # pull large data assets tracked by Git LFS
```

## 2. Backend Setup

```bash
cd backend
python -m venv .venv
source .venv/bin/activate        # Windows: .venv\Scripts\activate
pip install -r requirements.txt
```

Copy the example configuration file and edit it for your environment:

```bash
cp config/config.example.yaml config/config.yaml
```

Key settings in `config.yaml`:

| Key | Description |
|---|---|
| `data.root_dir` | Absolute path to the dataset storage directory |
| `server.host` | Host address for the API server (default `0.0.0.0`) |
| `server.port` | Port number (default `8000`) |

Start the backend server:

```bash
uvicorn app.main:app --reload
```

The API will be available at `http://localhost:8000`. Interactive docs are served at `http://localhost:8000/docs`.

## 3. Unity Client Setup

1. Open **Unity Hub** and click **Add project from disk**.
2. Select the `client/` folder.
3. Unity will import packages automatically; this may take several minutes on first open.
4. In **Edit → Project Settings → XR Plug-in Management**, enable the plug-in for your target platform.
5. Set the backend URL in `Assets/Settings/AppConfig.asset` → **Api Base Url**.

### Running in the Editor (Desktop XR Mode)

Press **Play** in the Unity Editor. Use the mouse to look around and `WASD` to move.

### Building for a Device

1. Go to **File → Build Settings**.
2. Select your target platform (e.g., *Android* for Meta Quest).
3. Click **Switch Platform**, then **Build and Run**.

## 4. Loading Data

See [data-pipeline.md](data-pipeline.md) for instructions on ingesting and pre-processing XCT and metallography datasets before loading them in the XR client.

## 5. Troubleshooting

| Symptom | Resolution |
|---|---|
| Unity cannot connect to backend | Ensure the backend server is running and the URL in `AppConfig.asset` is correct. |
| Blank volume in XR scene | Confirm that the dataset has been processed through the pipeline (see data-pipeline.md). |
| XR device not detected | Check that the correct XR plug-in is enabled in Project Settings. |
| `ModuleNotFoundError` on startup | Run `pip install -r requirements.txt` inside the activated virtual environment. |
