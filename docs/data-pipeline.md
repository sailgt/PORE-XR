# Data Pipeline

This document describes how raw materials-science imaging data is ingested, pre-processed, and made available for real-time XR visualisation in PORO-XR.

## Supported Input Formats

| Modality | Formats |
|---|---|
| X-ray Computed Tomography (XCT) | DICOM (`.dcm`), NIfTI (`.nii`, `.nii.gz`), TIFF stack, HDF5 (`.h5`, `.hdf5`), raw binary |
| Metallography / Optical Microscopy | TIFF, PNG, JPEG, SVS (whole-slide) |
| Metadata / Annotations | JSON, CSV |

---

## Pipeline Overview

```
Raw Data (XCT / Metallography)
        │
        ▼
1. Validation & Format Normalisation
        │
        ▼
2. Pre-processing
   ├── Denoising / artefact correction
   ├── Segmentation (optional, AI-assisted)
   └── Registration (multimodal alignment)
        │
        ▼
3. Tile & Pyramid Generation
   ├── 3-D: Octree / brick-based chunking
   └── 2-D: Deep-zoom tile pyramid
        │
        ▼
4. Metadata Indexing
        │
        ▼
5. Storage & Serving
   └── Dataset available via Backend API
```

---

## Step-by-Step Usage

### 1. Place Raw Data

Copy your dataset into the data root directory configured in `config.yaml` (key: `data.root_dir`).  
Recommended layout:

```
<data_root>/
└── <dataset_id>/
    ├── xct/          # XCT volume files
    └── metallography/ # 2-D microscopy images
```

### 2. Run the Pipeline

```bash
cd backend
source .venv/bin/activate
python -m pipeline.run --dataset <dataset_id>
```

Optional flags:

| Flag | Description |
|---|---|
| `--denoise` | Apply noise-reduction filter to XCT volume |
| `--segment` | Run AI-assisted material segmentation |
| `--register` | Register metallography images to XCT cross-sections |
| `--force` | Re-process even if output already exists |

### 3. Verify Output

Processed data is written to `<data_root>/<dataset_id>/processed/`.  
A summary report is printed on completion:

```
✓ Normalisation:     done (1.2 GB → 1.1 GB)
✓ Brick generation:  done (128³ bricks, 4 LOD levels)
✓ Tile pyramid:      done (metallography, 5 zoom levels)
✓ Metadata index:    done
Dataset <dataset_id> ready for streaming.
```

### 4. Load in the XR Client

In the Unity client, open the **Dataset Browser** panel, select your dataset from the list, and press **Load**. The volume will stream progressively to the XR scene.

---

## Configuration

Pipeline parameters can be tuned in `config.yaml` under the `pipeline` key:

```yaml
pipeline:
  brick_size: 128          # Voxels per brick edge (power of 2)
  lod_levels: 4            # Number of level-of-detail levels
  denoise_sigma: 1.0       # Gaussian sigma for denoising (0 = disabled)
  tile_size: 256            # Pixel size for 2-D tiles
  max_workers: 4           # Parallel worker processes
```

---

## Adding New Format Support

Implement a reader class in `backend/pipeline/readers/` that inherits from `BaseReader` and register it in `backend/pipeline/readers/__init__.py`. The reader must implement:

- `can_read(path: Path) -> bool`
- `read(path: Path) -> VolumeData`

See `backend/pipeline/readers/dicom_reader.py` for a reference implementation.
