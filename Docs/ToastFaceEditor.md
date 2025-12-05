Toast Face Editor – Design Document (Current State)

128×32 Protogen Visor Animation Tool for Toast

Overview

The Toast Face Editor is a Unity-based pixel-art and animation tool designed specifically for Toast’s 128×32 LED visor.
It allows creating facial expressions, animations, and exporting them to formats readable by the MatrixPortal S3 visor hardware.

The editor intentionally mimics tools like Aseprite, but specialized for the visor resolution, workflow, and export needs.

Core Features (Fully Implemented)
1. Pixel Drawing System

128×32 resolution (Toast visor size)

Draw pixels with mouse (left click only)

Eraser tool

Horizontal mirror mode

Color picker:

Color wheel (HSV hue & saturation)

Brightness slider (value)

Recent color swatches

Current color display

2. Animation System

Multiple frames

Add / Delete / Duplicate frame

Frame navigation (prev/next)

Playback with FPS slider

Onion skin:

Shows previous frame

Moves + zooms with canvas

Adjustable opacity (in code)

3. Thumbnail Timeline

Displays one thumbnail per frame

Click to jump to frame

Drag & drop to reorder frames

Highlights active frame

4. Zoom & Pan

Scroll wheel = zoom

Right mouse drag = pan

Clamped movement inside workspace

Uses UI masking to prevent overflow

5. Per-Pixel Shader Grid Overlay

Fully maskable (plays nice with WorkspacePanel)

Ultra-thin cell borders around every pixel

White/dark adaptive color depending on underlying brightness

Zoom-safe, crisp at any scale

Toggleable in UI

Technical Architecture
Major Scripts
Script	Purpose
ToastPixelEditor.cs	Core animation data, drawing logic, frame management, playback, onion skin, color application
FrameThumbnailStrip.cs	Build and manage horizontally scrolling thumbnail bar
FrameThumbnailItem.cs	Individual thumbnail + drag-to-reorder handling
ColorWheelPicker.cs	HSV color wheel + brightness slider
RecentColorSwatches.cs	List of last-used colors including UI buttons
PixelZoomPan.cs	Handles zoom, pan, clamping, and synchronizing movement for PixelDisplay, OnionPrev, GridOverlay
PixelGridOverlay.cs + Toast/PixelGridOverlay.shader	Per-pixel outlines using shader, mask-compatible
WorkspacePanel	UI container with Mask to clip drawing region
Unity UI Structure
Canvas
 ├── AppBackground              (Panel / Image, full-screen background)
 │
 ├── TopBar                     (Panel / Image + HorizontalLayoutGroup)
 │     ├── BtnNewFrame          (Button)
 │     ├── BtnPrevFrame         (Button)
 │     ├── BtnNextFrame         (Button)
 │     ├── BtnDuplicateFrame    (Button)
 │     ├── BtnDeleteFrame       (Button)
 │     └── BtnPlayPause         (Button + Text/TMP)
 │           └── SliderFPS      (Slider)    if this is visually grouped with play
 │
 ├── WorkspacePanel             (Panel / Image + Mask)
 │     ├── PixelDisplay         (RawImage + ToastPixelEditor + PixelZoomPan)
 │     ├── OnionPrev            (RawImage)
 │     └── GridOverlay          (RawImage + PixelGridOverlay, shader material)
 │
 ├── BottomBar                  (Panel / Image)
 │     ├── ToggleMirrorX        (Toggle)
 │     ├── ToggleOnion          (Toggle)
 │     └── ToggleGrid           (Toggle)
 │
 ├── ThumbnailBar               (Panel / Image + HorizontalLayoutGroup)
 │     └── ThumbnailTemplate    (Button + Image + RawImage [ThumbImage])
 │          (disabled; used as prefab for runtime thumbnails)
 │
 └── ColorPanel                 (Panel / Image)
       ├── LeftPanel            (Layout container)
       │     ├── ColorWheelImage   (RawImage + ColorWheelPicker)
       │     └── BrightnessSlider  (Slider)
       │
       └── RightPanel           (Layout container)
             ├── CurrentColorDisplay (RawImage / Image)
             ├── ToggleEraser        (Toggle)
             └── SwatchPanel         (Layout container)
                   ├── Swatch0       (Button)
                   ├── Swatch1       (Button)
                   ├── Swatch2       (Button)
                   ├── Swatch3       (Button)
                   ├── Swatch4       (Button)
                   ├── Swatch5       (Button)
                   ├── Swatch6       (Button)
                   ├── Swatch7       (Button)
                   ├── Swatch8       (Button)
                   └── Swatch9       (Button)

Known Good State

Everything below is confirmed working together smoothly:

Drawing, erasing, mirroring
Color wheel, brightness, recent colors
Onion skin synchronized with zoom/pan
Playback
Thumbnails with drag-to-reorder
UI masking: canvas, onion, and grid do not escape workspace
Shader-driven pixel grid
Zoom & pan smooth and well-behaved

This is our “stable foundation” for new features.

Next Features (Planned & Prioritized)
Tier 1: High Priority
1. Undo / Redo Stack

Required for safety during drawing

Records pixel operations + frame changes

Probably using a command-based approach

2. Export System (for MatrixPortal)

Export one animation as:

RAW frame sequence

RGB565 binary

JSON metadata:

FPS

Frame count

Emotion tag

(Optional) GIF export for PC preview

3. Template / Guide Layer

Permanent visor outline

Optional centerlines or expression landmarks

Togglable, non-exported

Moves with zoom/pan

Tier 2: Medium Priority
4. Additional Drawing Tools

Line tool

Rectangle / filled rectangle

Circle tool

Flood fill

Select/move tool (optional)

5. Workspace Polish

Tool icons

Clearer docking areas

Proper button styles

Zoom percentage indicator

6. Multi-Onion Skin

Show next frame + previous frame

Different tint colors

Tier 3: Future Extensions
7. Emotion Manager

Define animations by emotion name

Auto-generate JSON file for MatrixPortal

Support Bluetooth emotion switching

8. Live Preview Mode

Simulate visor curvature

Side LCD eye preview (future hardware)

9. Export Presets

“Export all animations”

“Export selected emotion”

“Export JSON only”

How to Resume Development With a New Chat

Whenever you start a new ChatGPT session:

Paste only this:

This is continuing the Toast Face Editor Unity project.

Here is the design doc summary:
[Paste Tier 1 + Tier 2 sections only]

Current task:
<what we are building next>


That is all I need to be instantly back up to full context.

End of Design Doc