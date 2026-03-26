# Roadmap

This document outlines the planned development trajectory for PORO-XR.

## Vision

Become the go-to open-source platform for immersive, multimodal exploration of materials-science imaging data, enabling researchers to gain insights that would be impossible with conventional 2-D viewers.

---

## Release Milestones

### v0.1 – Foundation *(in progress)*
- [x] Repository structure and documentation scaffold
- [ ] Core Unity XR project with OpenXR integration
- [ ] Basic volumetric rendering of XCT data (DICOM / TIFF stack)
- [ ] FastAPI backend with dataset listing and streaming endpoints
- [ ] Desktop XR simulation mode

### v0.2 – Multimodal Visualisation
- [ ] Metallography image overlay aligned to XCT cross-sections
- [ ] Transfer function editor with presets
- [ ] Clipping plane tools (up to 3 orthogonal planes)
- [ ] AI-assisted material segmentation (phase detection)

### v0.3 – Collaboration & Annotations
- [ ] Multi-user shared session support
- [ ] Point and ROI annotation creation and export (JSON / CSV)
- [ ] Session save / load
- [ ] User preference profiles

### v0.4 – Data Pipeline Enhancements
- [ ] Streaming level-of-detail (LOD) for large volumes (> 10 GB)
- [ ] Support for additional input formats (OME-Zarr, OpenVDB)
- [ ] Automated denoising and artefact-correction pipeline
- [ ] Cloud storage backend (S3-compatible)

### v1.0 – Stable Release
- [ ] Full test coverage (unit + integration)
- [ ] Comprehensive API documentation
- [ ] Performance benchmarks and optimisations
- [ ] Installer / deployment packages for common platforms
- [ ] Peer-reviewed publication and citation guide

---

## Feature Backlog

The following features are under consideration for future releases:

- Haptic feedback for volume interaction on supported controllers
- Machine-learning-based anomaly detection (porosity characterisation)
- Integration with materials databases (e.g., ICDD, COD)
- Python scripting API for custom analysis workflows
- Web-based lightweight viewer (WebXR / Three.js)
- Support for in-situ / 4-D time-series datasets

---

## Contributing

Contributions are welcome! Please open an issue to discuss a new feature before submitting a pull request. See the project [README](../README.md) for contribution guidelines.
