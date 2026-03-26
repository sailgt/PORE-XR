# User Guide

This guide explains how to use PORO-XR to explore XCT and metallography datasets in Extended Reality.

## Getting Started

1. Ensure the backend server is running (see [setup.md](setup.md)).
2. Ensure your dataset has been processed (see [data-pipeline.md](data-pipeline.md)).
3. Put on your XR headset (or launch the Unity Editor in desktop mode) and open the PORO-XR application.

---

## Interface Overview

```
┌────────────────────────────────────────────────────────┐
│  [Dataset Browser]   [Clipping Tools]   [Annotations]  │  ← Toolbar
├────────────────────────────────────────────────────────┤
│                                                        │
│              3-D Volume / XR Scene                     │
│                                                        │
├────────────────────────────────────────────────────────┤
│  Transfer Function Editor  │  Metallography Panel       │  ← Side panels
└────────────────────────────────────────────────────────┘
```

### Dataset Browser
Lists all datasets available on the connected backend server. Select a dataset and press **Load** to stream it into the scene.

### Transfer Function Editor
Controls how voxel intensities are mapped to colour and opacity. Use the curve editor to highlight specific material phases or suppress background noise.

### Metallography Panel
Displays registered 2-D microscopy images that correspond to the currently active XCT cross-section plane.

### Annotations Panel
Create, edit, and export point or region-of-interest (ROI) annotations on the volume.

---

## Navigation Controls

### Desktop XR Mode

| Action | Control |
|---|---|
| Look around | Mouse drag |
| Move | `W A S D` |
| Elevate / descend | `E` / `Q` |
| Interact with panel | Left-click |
| Grab / rotate volume | `Right-click + drag` |

### XR Headset (6-DOF Controllers)

| Action | Control |
|---|---|
| Teleport | Point + press trigger |
| Grab volume | Grip button |
| Rotate volume | Grip + move controller |
| Scale volume | Two-hand pinch |
| Open menu | Menu button (left controller) |
| Interact with panel | Point ray + trigger |

---

## Working with Volumes

### Clipping Planes
1. Select **Clipping Tools** from the toolbar.
2. Drag the slider or grab the plane handle in the scene to move the clip plane.
3. Up to three orthogonal clip planes can be active simultaneously.

### Adjusting the Transfer Function
1. Open the **Transfer Function Editor** side panel.
2. Click on the colour bar to add control points.
3. Drag control points horizontally to change the intensity range and vertically to adjust opacity.
4. Use the **Preset** dropdown for common material-contrast settings.

### Multimodal Correlation
1. Ensure the dataset was processed with the `--register` flag (see [data-pipeline.md](data-pipeline.md)).
2. Open the **Metallography Panel**.
3. As you move a clip plane, the panel automatically updates to show the corresponding microscopy image.
4. Toggle **Side-by-Side** mode to view both modalities simultaneously.

---

## Annotations

### Creating an Annotation
1. Open the **Annotations Panel**.
2. Select **Point** or **ROI** annotation type.
3. In the scene, point at the target location and press the trigger (XR) or left-click (desktop).
4. Enter a label in the text field that appears.

### Exporting Annotations
1. In the **Annotations Panel**, press **Export**.
2. Choose **JSON** or **CSV** format.
3. The file is saved to the `exports/` folder in your data root directory.

---

## Sharing a Session

PORO-XR supports collaborative multi-user sessions over a local network.

1. The session host opens the **Session** menu and selects **Host Session**.
2. Other users select **Join Session** and enter the host's IP address.
3. All participants share the same scene state (volume position, clip planes, annotations).

---

## Keyboard Shortcuts (Desktop)

| Shortcut | Action |
|---|---|
| `F` | Focus view on volume |
| `R` | Reset volume transform |
| `C` | Toggle clip plane visibility |
| `Ctrl + Z` | Undo last annotation |
| `Ctrl + S` | Save session |
| `Escape` | Close active panel |
