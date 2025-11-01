# WebGL Audio Filters Fix

**Date:** 2025-01-30
**Issue:** Audio filters (low-pass, high-pass, distortion) work in Editor/Desktop but not in WebGL builds

---

## The Problem

Unity's built-in audio filter components (`AudioLowPassFilter`, `AudioHighPassFilter`, `AudioDistortionFilter`) **do not work in WebGL builds**.

### Why?

- **Desktop/Editor:** Unity uses native audio processing with full support for AudioFilter components
- **WebGL:** Unity uses the browser's Web Audio API, which doesn't support Unity's AudioFilter components
- The filters are silently ignored in WebGL builds, resulting in unfiltered audio

---

## The Solution

Use **platform-dependent compilation** to apply filters differently based on the build target:

- **WebGL builds:** Use Web Audio API directly via JavaScript plugin (`.jslib` file)
- **Desktop/Editor builds:** Continue using Unity's AudioFilter components

---

## Files Added

### 1. JavaScript Plugin: `RadioAudioFilters.jslib`
**Location:** `Assets/EpsilonIV/Plugins/WebGL/RadioAudioFilters.jslib`

**Purpose:** Provides Web Audio API filter functionality for WebGL builds

**Functions:**
- `ApplyRadioFiltersWebGL()` - Creates and connects Web Audio filter nodes
- `RemoveRadioFiltersWebGL()` - Disconnects and removes filter nodes

**Filters Implemented:**
- **BiquadFilterNode (highpass)** - Removes low frequencies
- **BiquadFilterNode (lowpass)** - Removes high frequencies
- **WaveShaperNode** - Adds distortion/compression character

---

### 2. Updated C# Script: `RadioAudioPlayer.cs`
**Location:** `Assets/EpsilonIV/Scripts/Conversation/RadioAudioPlayer.cs`

**Changes:**
- Added `using System.Runtime.InteropServices;` for DllImport
- Added platform-dependent `[DllImport("__Internal")]` declarations for WebGL
- Updated `ApplyRadioEffects()` method with `#if UNITY_WEBGL` directives:
  - **WebGL path:** Calls JavaScript `ApplyRadioFiltersWebGL()`
  - **Desktop path:** Uses Unity's `AudioLowPassFilter`, `AudioHighPassFilter`, `AudioDistortionFilter`
- Updated `RemoveEffects()` method with platform-dependent cleanup

---

## How It Works

### WebGL Build Flow:

```
1. RadioAudioPlayer.ApplyRadioEffects() called
   ↓
2. Detects UNITY_WEBGL build (via #if directive)
   ↓
3. Calls ApplyRadioFiltersWebGL() JavaScript function
   ↓
4. JavaScript accesses Unity's Web Audio context (WEBAudio.audioContext)
   ↓
5. Creates BiquadFilterNode and WaveShaperNode
   ↓
6. Connects filter chain: AudioSource -> Filters -> Destination
   ↓
7. Filtered audio plays in browser
```

### Desktop/Editor Build Flow:

```
1. RadioAudioPlayer.ApplyRadioEffects() called
   ↓
2. Detects non-WebGL build (via #else directive)
   ↓
3. Adds AudioLowPassFilter component to GameObject
   ↓
4. Adds AudioHighPassFilter component to GameObject
   ↓
5. Adds AudioDistortionFilter component to GameObject
   ↓
6. Unity's audio engine applies filters natively
   ↓
7. Filtered audio plays
```

---

## Filter Parameters

All filter parameters are controlled from the Unity Inspector on the `RadioAudioPlayer` component:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `enableLowPassFilter` | bool | true | Enable low-pass filter (radio frequency range) |
| `lowPassCutoff` | float | 3000 Hz | Low-pass cutoff frequency |
| `enableHighPassFilter` | bool | true | Enable high-pass filter (remove low rumble) |
| `highPassCutoff` | float | 300 Hz | High-pass cutoff frequency |
| `enableDistortion` | bool | false | Enable distortion filter (analog character) |
| `distortionLevel` | float | 0.3 | Distortion amount (0-1) |

---

## Testing

### Desktop Build:
1. Build for **Windows/Mac/Linux**
2. Play the game and send messages to NPCs
3. Listen for radio effect on NPC responses
4. ✅ **Should work:** Muffled, filtered audio like a walkie-talkie

### WebGL Build:
1. Build for **WebGL**
2. Host on a web server or use Unity's local server
3. Play the game in a browser (Chrome, Firefox, Edge)
4. Send messages to NPCs
5. ✅ **Should work:** Same muffled, filtered audio effect
6. Open browser console (F12) to see debug logs:
   - `[RadioFilters] Applied high-pass filter: 300 Hz`
   - `[RadioFilters] Applied low-pass filter: 3000 Hz`
   - `[RadioFilters] Filter chain connected with 2 filters`

---

## Browser Compatibility

The Web Audio API is supported in all modern browsers:

- ✅ Chrome/Edge (Chromium) 14+
- ✅ Firefox 25+
- ✅ Safari 6+
- ✅ Opera 15+

**Note:** Internet Explorer does not support Web Audio API.

---

## Debugging

### If filters don't work in WebGL:

1. **Check browser console** (F12 → Console tab)
   - Look for `[RadioFilters]` debug messages
   - Check for JavaScript errors

2. **Verify plugin is included**
   - Check Build Settings → Player Settings → Publishing Settings
   - Ensure `.jslib` files are being included in build

3. **Check Web Audio context**
   - In browser console, type: `WEBAudio.audioContext`
   - Should return an AudioContext object, not undefined

4. **Verify AudioSource exists**
   - The JavaScript tries to find Unity's AudioSource
   - If NPC audio isn't playing at all, filters can't be applied

5. **Test with simple filter**
   - Disable all filters except low-pass
   - Set low-pass cutoff to 1000 Hz (very noticeable)
   - If you hear a difference, filters are working

---

## Performance Considerations

### WebGL:
- Web Audio API filters are hardware-accelerated in modern browsers
- Minimal performance impact
- Filter creation happens once per NPC response

### Desktop:
- Unity's AudioFilters are optimized and efficient
- Negligible CPU impact for 2-3 filters per AudioSource

---

## Future Enhancements

### Possible additions:
1. **Dynamic EQ adjustment** - Change filter cutoffs in real-time based on "signal strength"
2. **Reverb/Echo** - Add spatial effects using ConvolverNode (WebGL) or AudioReverbFilter (Desktop)
3. **Static overlay** - Mix in background static audio for more authentic radio feel
4. **Frequency analyzer** - Visualize audio spectrum on radio UI

---

## Technical Notes

### Why Platform-Dependent Code?

Unity's build system compiles different code for different platforms:
- `#if UNITY_WEBGL && !UNITY_EDITOR` - Only included in WebGL builds
- `#else` - Included in all other builds (Desktop, Mobile, Editor)

This allows the same source file to work correctly on all platforms without runtime checks.

### JavaScript Plugin Execution

`.jslib` files are:
1. Merged into Unity's generated JavaScript during build
2. Loaded before Unity's main game code
3. Accessible via `DllImport("__Internal")`
4. Only available in WebGL builds (ignored on other platforms)

---

**End of Documentation**
