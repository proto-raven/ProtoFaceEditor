# ProtoFace / Toast Face Editor – Full Project Roadmap

This document defines the long-term vision for the ProtoFace ecosystem:
- The Unity-based ProtoFace Editor  
- The MatrixPortal S3 visor firmware  
- Two Waveshare ESP32-S3 cheek display boards  
- A unified export format enabling synchronized expressions across devices  

The guiding philosophy is:

**The editor describes WHAT emotions, animations, and behaviors exist.  
The firmware decides WHEN to activate them.  
All displays react in sync based on a shared emotional state.**

---

# 1. Project Goals

1. Create expressive, layered animations for the Toast 128×32 LED visor.  
2. Provide synchronized emotional output across:
   - Main visor (MatrixPortal S3 → 128×32 LED matrix)  
   - Left cheek 480×480 display  
   - Right cheek 480×480 display  
3. Support real-time emotional reactivity:
   - Temporary reactions (APDS nose boop, gestures, taps)  
   - Persistent base emotions (Bluetooth remote)  
   - Motion-reactive faces (tilt, nod, shake via accelerometer)  
   - Audio-reactive mouths (microphone amplitude)  
4. Export a stable data package (JSON + frame bank) describing all animations and metadata.  
5. Avoid major refactors later by defining expandable data structures from the start.  

---

# 2. Hardware Targets

## 2.1 Main Visor Controller – MatrixPortal S3
- ESP32-S3 with WiFi + BLE  
- Built-in LIS3DH accelerometer  
- STEMMA QT I²C port  
- Drives 128×32 RGB LED matrix  
- Runs the full emotional state machine  

**Connected Sensors**
- APDS-9960 nose sensor (proximity / gestures)  
- Microphone (analog or I²S)  
- BLE remote control  

---

## 2.2 Cheek Displays – Waveshare 2.8" ESP32-S3 Touch Round Displays
Product reference:  
https://thepihut.com/products/esp32-s3-development-board-with-2-8-ips-capacitive-touch-round-display-480-x-480

Capabilities:
- ESP32-S3 microcontroller  
- 480×480 IPS capacitive touchscreen  
- WiFi + BLE  
- PSRAM for framebuffer use  
- Ideal for expressive cheek animations  

Role:
- Each cheek is an **emotion client**, not a controller.  
- They receive symbolic emotion updates from MatrixPortal and render appropriate visuals.  

Communication options:
- BLE (default)  
- WiFi UDP (optional)  
- UART (fallback/debug)  

---

# 3. Unity Editor Roadmap

The Unity-based ProtoFace Editor evolves from a pure pixel editor into a full animation + metadata authoring tool.

## 3.1 Animation / Emotion Library

Introduce an animation library where each animation has:
- `id`, `displayName`  
- `fps`, `loopMode`  
- `region`: one of  
  - `fullFace`  
  - `eyes`  
  - `mouth`  
  - `fx`  
- `layer`: one of  
  - `base`  
  - `reaction`  
  - `mouth`  
  - `fx`  
- Frame range (`startIndex`, `frameCount`) into a global frame bank  

`ToastPixelEditor` remains the painting environment;  
higher-level scripts manage animation definitions and organization.

---

## 3.2 Layering System

Enable runtime composition by assigning animations to layers:
- **Base layer** — the persistent emotion set by BLE  
- **Motion layer** — variations based on tilt/nod/shake  
- **Reaction layer** — APDS triggers, gestures, taps  
- **Mouth layer** — audio-reactive mouth animation  

These are purely metadata in the editor; firmware performs runtime compositing.

---

## 3.3 Trigger Metadata

Each animation can define triggers in four categories.

### Base Emotion Trigger
- Enabled  
- `bleCode`: remote command ID  

### Reaction Trigger
- Enabled  
- `source`: `APDS`, `ACCEL`, or `CUSTOM`  
- `event`: e.g. `PROX_NEAR`, `GESTURE_LEFT`, `TAP`  
- `cooldownMs`  
- `priority`  
- `canInterruptBase`  

### Motion Trigger
- Enabled  
- Numeric pitch/yaw/roll ranges  
- `priority`  

### Audio Mouth Trigger
- Enabled  
- `volumeMap`: array of `{ maxLevel, frameIndex }`  
- `smoothingMs`  

The editor does not simulate these events; it only authorizes metadata.

---

# 4. Cheek Display Metadata

Cheek displays are independent ESP32-S3 boards that react to emotional changes.  
Each animation can define how the cheeks should behave during that animation.

### Cheek Metadata Block Example

"cheeks": {
"mode": "emoji", // "emoji" | "colorPulse" | "custom"
"emojiId": "happy", // symbolic ID understood by cheek firmware
"color": "#FFCC55", // optional: tint or base pulse color
"intensity": 0.8 // optional: affects animation strength or speed
}

markdown
Copy code

### Responsibilities

**Editor**
- Defines cheek behavior metadata.
- Does not edit cheek graphics.

**MatrixPortal S3**
- Determines active emotion.
- Sends cheek updates as small symbolic packets.

**Cheek Displays**
- Render emoji, color pulses, or custom animations.
- Do not interpret sensors or compute emotional logic.

### Optional Global Cheek Config

"cheekDisplayDefaults": {
"transport": "BLE",
"leftAddress": "CHEEK_L",
"rightAddress": "CHEEK_R",

"emojiMap": {
"happy": { "color": "#FFD966" },
"angry": { "color": "#FF4444" },
"sad": { "color": "#66A3FF" }
}
}

yaml
Copy code

This provides a standardized, scalable interface for cheek displays.

---

# 5. Export System — ProtoFace Export v1

## 5.1 Binary Frame Bank
A single RGB565 file containing all animation frames:
- `frames.rgb565`
- Indexed globally by animation metadata  
- Avoids per-animation file duplication  

## 5.2 JSON Metadata File
- `face_config.json`
Includes:
- Matrix resolution & frame format  
- Frame bank information  
- Complete animation library  
- Trigger metadata  
- Cheek display metadata  
- Default base emotion  

Designed to be forward-compatible and extensible.

---

# 6. Firmware Architecture

## 6.1 Input Sources (MatrixPortal S3)
- BLE → persistent base emotion  
- APDS-9960 → reactions (boop, swipe, proximity)  
- LIS3DH → pitch / yaw / roll for motion variants  
- Microphone → audio amplitude for mouth animation  

---

## 6.2 Emotion State Machine

MatrixPortal maintains:

- `baseEmotionId`  
- `reactionId` (time-limited)  
- Motion-matched animation variant  
- Mouth frame selected via audio amplitude  

The firmware:
1. Loads JSON + frame bank  
2. Determines current active animation layers  
3. Composes them into final 128×32 output  
4. Sends corresponding cheek metadata update  

---

# 7. Cheek Display Firmware

Each cheek ESP32-S3 board:

1. Connects to MatrixPortal via BLE/WiFi/UART  
2. Receives small symbolic updates:  
{
"emotionId": "happy_base",
"cheekMode": "emoji",
"emojiId": "happy",
"color": "#FFCC55",
"intensity": 0.8
}

markdown
Copy code
3. Renders the appropriate emoji or pulse effect  
4. Does not read sensors or compute emotional state  

This architecture keeps cheeks lightweight and easy to update.

---

# 8. Implementation Order

## Stage 1 — Core Editor Foundation
- Frame bank export  
- Basic JSON export  
- Animation library  

## Stage 2 — Metadata System
- Region/layer metadata  
- Trigger metadata  
- Cheek metadata  

## Stage 3 — Firmware v1 (Visor Core)
- Load export format  
- Base emotion playback  
- Reactions  
- Motion variants  
- Audio-reactive mouth  

## Stage 4 — Cheek Firmware
- BLE/WiFi connection  
- Emoji rendering  
- Synchronization with MatrixPortal  

## Stage 5 — Polish & Quality
- Editor preview improvements  
- Layer visualization tools  
- Export presets  
- Validation checks  

---

# 9. Development Checklist (Practical Next Steps)

This section lists concrete implementation tasks in recommended order, combining
the original feature roadmap with the updated multi-display architecture.

It is intended as the day-to-day development guide.

---

## 9.1 Core Infrastructure (Editor + Export)

### 1. Implement Frame Bank Export
- Export all frames into a contiguous RGB565 binary file.
- Add editor UI button: "Export Animation Package".
- Validate frame indexing logic.

### 2. Implement JSON Metadata Export
- Generate `face_config.json` with:
  - Animation definitions
  - Trigger metadata
  - Cheek metadata
  - Frame bank references
- Ensure this matches firmware expectations.

### 3. Build Animation / Emotion Library
- Add UI to create, rename, duplicate animations.
- Bind editor’s current frame list to a selected animation.
- Simple “Animation Browser” panel required.

---

## 9.2 Animation Metadata Systems

### 4. Add Region & Layer Selection
- Dropdowns: region (fullFace/eyes/mouth/fx)
- Dropdowns: layer (base/reaction/mouth/fx)
- Stored per-animation in metadata.

### 5. Add Trigger Panels
Implement inspector-style panels for:

**Base Emotion Trigger**
- Enable toggle
- BLE command ID

**Reaction Trigger**
- Enable toggle
- Source (APDS/ACCEL/CUSTOM)
- Event type
- Cooldown
- Priority
- Interruption rules

**Motion Trigger**
- Pitch/Yaw/Roll ranges
- Priority

**Audio Mouth Trigger**
- Volume → frame index mapping table
- Smoothing controls

---

## 9.3 Cheek Display Support

### 6. Add Cheek Metadata UI
For each animation:
- Mode selector (emoji/colorPulse/custom)
- Emoji ID field
- Color picker
- Intensity slider

### 7. Optional: Global Cheek Display Settings
- Preferred transport (BLE/WiFi/UART)
- Left/right address fields
- Default emoji/color map editor

---

## 9.4 Editor Feature Enhancements

### 8. Implement Undo/Redo System
- Track pixel-level changes
- Track frame-level operations (add/delete/duplicate)
- Rolling history stack

### 9. Additional Drawing Tools
- Line tool
- Rectangle (solid/outline)
- Ellipse/circle
- Flood fill
- Selection/move (optional stretch goal)

### 10. Layered Preview (Non-essential but valuable)
- Allow previewing:
  - Base + motion variant
  - Base + reaction
  - Base + mouth
- Visualize layering without full firmware simulation.

---

## 9.5 Firmware & Integration

### 11. MatrixPortal Firmware v1
- Load JSON + frame bank
- Implement animation playback (base only)
- Implement BLE-controlled emotion selection

### 12. Sensor Reactive Logic
- APDS-9960 handling → trigger reactions
- Motion processing → select motion variants
- Microphone amplitude → mouth frame mapping

### 13. Cheek Display Messaging
- Broadcast active emotion + cheek metadata
- Implement BLE or WiFi protocol
- Resync logic on reconnect

### 14. Cheek Display Firmware
- Receive and parse emotion packets
- Render emoji or colorPulse modes
- Implement small internal emoji library

---

## 9.6 Final Polish & Tools

### 15. Export Presets
- Export single animation
- Export all animations
- Export metadata only

### 16. Editor Quality-of-Life
- Zoom percentage indicator
- Improved icons/buttons
- Frame timeline polish
- Error/warning messages for invalid metadata

---

This checklist ensures the entire ProtoFace system can be built incrementally,
with no major refactors, and stays aligned with the multi-device emotional
architecture defined in this document.


# End of Roadmap

This roadmap defines a stable foundation for the ProtoFace system and ensures all future features—from cheek animations to advanced emotional responses—fit cleanly into the architecture without requiring major redesigns.
