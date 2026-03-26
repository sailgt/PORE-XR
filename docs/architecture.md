# Architecture

## Overview

PORO-XR is an interactive Extended Reality (XR) platform designed for multimodal visualization of X-ray Computed Tomography (XCT) and metallography data in materials research. The platform bridges the gap between complex volumetric imaging data and intuitive, immersive exploration tools.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      Client (XR Device)                  │
│  ┌──────────────┐   ┌──────────────┐  ┌──────────────┐  │
│  │  XR Renderer │   │  UI Layer    │  │  Input/       │  │
│  │  (Volumetric)│   │  (Panels,    │  │  Interaction  │  │
│  │              │   │   Overlays)  │  │  Manager      │  │
│  └──────┬───────┘   └──────┬───────┘  └──────┬───────┘  │
│         └──────────────────┴─────────────────┘          │
│                        XR Core Engine                    │
└───────────────────────────┬─────────────────────────────┘
                            │ API / WebSocket
┌───────────────────────────▼─────────────────────────────┐
│                      Backend Server                       │
│  ┌──────────────┐   ┌──────────────┐  ┌──────────────┐  │
│  │  Data        │   │  Processing  │  │  Session     │  │
│  │  Ingestion   │   │  Pipeline    │  │  Manager     │  │
│  └──────┬───────┘   └──────┬───────┘  └──────────────┘  │
└─────────┼─────────────────┼───────────────────────────--┘
          │                 │
┌─────────▼─────────────────▼────────────────────────────┐
│                      Data Storage                        │
│     XCT Volumes  │  Metallography Images  │  Metadata   │
└────────────────────────────────────────────────────────-┘
```

## Components

### XR Core Engine
Handles rendering, scene management, and device abstraction. Built to support head-mounted displays (HMDs) and desktop XR modes.

### Volumetric Renderer
Converts XCT scan data (3-D voxel grids) into real-time volume renderings inside the XR scene. Supports adjustable transfer functions for material segmentation and opacity mapping.

### Metallography Overlay
Registers and displays 2-D optical/electron microscopy images alongside the corresponding XCT cross-section, enabling multimodal correlation.

### Data Processing Pipeline
Pre-processes raw scan files (see [data-pipeline.md](data-pipeline.md)) into optimised tile-based formats for streaming to the XR client.

### Backend Server
Provides REST and WebSocket APIs for data retrieval, session state synchronisation, and user annotation storage.

### Data Storage
Supports local filesystems, network-attached storage (NAS), and cloud object storage for raw and processed datasets.

## Technology Stack

| Layer | Technology |
|---|---|
| XR Runtime | OpenXR / Unity XR Toolkit |
| 3-D Rendering | Unity HDRP / custom volume shaders |
| Backend | Python (FastAPI) |
| Data Formats | DICOM, NIfTI, TIFF stack, HDF5 |
| Communication | REST, WebSocket |
| Storage | Local FS / S3-compatible object storage |
