ProtoFace / Toast Face Editor — Unified Design Specification
1. Overview

The Toast Face Editor (ProtoFace Editor) is a Unity-based pixel animation tool used to create expressive faces for Toast’s 128×32 LED visor and coordinated animations for two external 480×480 cheek displays.

The editor outputs both:

Pixel animations (used by the visor)

Metadata describing emotional behavior, triggers, and cheek display reactions

The final result is a consistent emotional system running across:

MatrixPortal S3 (main 128×32 LED visor)

Left cheek ESP32-S3 round display

Right cheek ESP32-S3 round display

The editor is the authoritative source of:

Animation frames

Frame order / FPS / loop mode

Emotional definitions

Sensor-trigger behavior

Cheek display behavior

The firmware interprets this exported data; the editor does not simulate sensors or runtime logic.

2. Display Hardware
2.1 Visor (Main Display)

MatrixPortal S3 controlling a 128×32 RGB LED matrix

Runs the emotional state machine

Reads:

APDS-9960 nose sensor

Microphone

Accelerometer (LIS3DH)

BLE remote commands

2.2 Cheek Displays

Two Waveshare ESP32-S3 2.8" IPS Round Displays (480×480)
(Example product: The Pi Hut ESP32-S3 Round Display board)

Capabilities:

ESP32-S3 with BLE/WiFi

480×480 IPS screen

PSRAM for smooth rendering

Capacitive touch (not required for core design)

Role:

Emotion clients that render cheek expressions

They do not compute emotion; they receive symbolic updates from MatrixPortal

3. Editor Architecture
3.1 Core Pixel Editing

The existing editor provides:

Pixel-level painting tools

Color wheel + brightness + recent swatches

Onion skinning

Zoom & pan

Grid overlay shader

Frame list with thumbnails

Playback controls

This remains the foundation.

4. Animation / Emotion Library

The editor introduces the ability to define multiple independent animations, each describing:

A unique emotion or behavior

A set of frames (subset of global frame bank)

Timing + looping info

Layering information

Trigger metadata

Cheek display metadata

Each animation includes:

id            (string)
displayName   (string)
fps           (number)
loopMode      ("loop" | "once" | "pingPong")

region        ("fullFace" | "eyes" | "mouth" | "fx")
layer         ("base" | "reaction" | "mouth" | "fx")

frames:
    startIndex
    frameCount

triggers:
    baseEmotion
    reaction
    motion
    audioMouth

cheeks:
    mode        ("emoji" | "colorPulse" | "custom")
    emojiId     (string)
    color       (hex string)
    intensity   (0–1)

5. Layering Model

Animations can occupy different compositing layers:

5.1 Base Layer

The persistent emotional state

Selected via BLE remote

Example: neutral, happy, angry, sad

5.2 Motion Layer

Variants determined by head tilt / orientation

Examples: look_left, look_right, look_up, look_down

5.3 Reaction Layer

Temporary animations triggered by sensors

Examples: boop_react, surprise_react, tap_react

5.4 Mouth Layer

Frames representing mouth openness

Controlled by microphone amplitude

The firmware blends these layers into one final 128×32 frame.

6. Trigger Metadata

Each animation may define one or more trigger categories.

6.1 Base Emotion Trigger

Used for persistent states via BLE remote.

baseEmotion:
    enabled
    bleCode      (integer)

6.2 Reaction Trigger

Triggered by APDS gestures or accelerometer taps.

reaction:
    enabled
    source       ("APDS" | "ACCEL" | "CUSTOM")
    event        ("PROX_NEAR" | "GESTURE_LEFT" | "TAP" ...)
    cooldownMs
    priority
    canInterruptBase

6.3 Motion Trigger

Selected automatically based on pitch, yaw, roll.

motion:
    enabled
    pitch: { min, max }
    yaw:   { min, max }
    roll:  { min, max }
    priority

6.4 Audio Mouth Trigger

Maps microphone amplitude to mouth frames.

audioMouth:
    enabled
    volumeMap[]: array of { maxLevel, frameIndex }
    smoothingMs

7. Cheek Display Metadata

Each animation includes a cheeks object that describes how cheek displays react.

Example:

cheeks:
    mode       ("emoji" | "colorPulse" | "custom")
    emojiId    (symbolic id used by cheek firmware)
    color      ("#RRGGBB")
    intensity  (0–1)


The editor defines this metadata; the cheeks render their own visuals.

Optional Global Cheek Config

The export may include configuration like:

cheekDisplayDefaults:
    transport     ("BLE" | "WiFiUDP" | "UART")
    leftAddress
    rightAddress

    emojiMap:
        happy: { color: "#FFD966" }
        angry: { color: "#FF4444" }
        sad:   { color: "#66A3FF" }

8. Export Pipeline

The ProtoFace Editor exports two files:

8.1 Frame Bank (Binary)

frames.rgb565

Contains all frames for all animations

Packed in contiguous RGB565 format

Indexed by startIndex + frameCount

8.2 Metadata File (JSON)

face_config.json

Contains:

Matrix resolution

Frame bank info

Animation library

Trigger metadata

Cheek display metadata

Defaults

Designed to be forward-compatible and extensible.

9. Firmware Architecture (MatrixPortal)

MatrixPortal S3:

Loads the frame bank + JSON

Reads all sensors:

BLE remote

APDS (boop/gesture)

Microphone

Accelerometer

Computes emotional state:

Base emotion

Active reaction

Motion variant

Mouth frame

Composes layers

Renders final visor frame

Sends emotion + cheek metadata to cheek displays

10. Cheek Display Architecture

Each cheek ESP32-S3 display:

Connects to MatrixPortal via BLE/WiFi/UART

Receives symbolic emotion updates:

emotionId
cheekMode
emojiId
color
intensity


Renders:

Emoji icons

Color pulses

Custom animations

Cheek displays do not handle sensors or emotional logic.

11. Editor UI Additions (Future Work)

The editor will gain:

An “Animation Library” panel

UI for region/layer selection

Trigger metadata editors

Cheek settings UI

Export window

The core painting interface remains unchanged.

12. Summary

The updated architecture ensures:

No major future refactors

Clear separation of concerns

Simple export pipeline

Multi-display emotional coherence

Scalable rule-based animation activation

Clean extensibility for new sensors or displays

The ProtoFace Editor becomes a full emotional animation authoring suite for a distributed face system.