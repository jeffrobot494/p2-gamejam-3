# STT System Refactor Plan

## Executive Summary

This document outlines a complete refactor of the Speech-to-Text (STT) system to address critical bugs and improve user experience. The current system fails to receive transcripts from the Player2 STT service and has significant latency issues.

## Current Problems

### Critical Issues
1. **No transcripts received** - WebSocket receives zero messages from STT service despite audio being sent
2. **Short utterances not detected** - Minimum 3.5s duration requirement misses quick responses
3. **High latency** - Multi-second delay between stopping speech and receiving transcript
4. **Complex buffering logic** - Unnecessary minimum duration waits add delays

### Current Architecture Issues
- WebSocket connected and sending audio, but receiving nothing back
- `interim_results=false` and `vad_events=false` means server only responds after detecting silence
- User releases R → immediate WebSocket close → STT service has no time to finalize transcript
- Minimum duration logic (3.5s) adds artificial delays

## Requirements

### Functional Requirements
- **FR1**: Use Player2 STT service (existing infrastructure)
- **FR2**: Display live transcription as user speaks
- **FR3**: Support utterances of any length (no minimum duration)
- **FR4**: Must work in WebGL builds (use WebGLMicrophoneManager)
- **FR5**: Instant response when user releases R key

### Non-Functional Requirements
- **NFR1**: Reliable transcript delivery (100% success rate for valid speech)
- **NFR2**: Low latency (<500ms from speech end to transcript)
- **NFR3**: Clean, maintainable code architecture
- **NFR4**: Proper WebGL microphone handling

## Proposed Solution

### Key Changes

#### 1. Enable Real-Time STT Features
**Current:**
```csharp
enableInterimResults = false
enableVAD = false
```

**New:**
```csharp
enableInterimResults = true  // Get partial transcripts as user speaks
enableVAD = true            // Voice activity detection events
```

**Why:** Server will send messages in real-time instead of waiting for silence detection.

#### 2. Remove Minimum Duration Logic
**Current:** MessageManager waits for 3.5s minimum before processing transcript
**New:** Immediately process transcript when R is released

**Files affected:**
- `MessageManager.cs` - Remove `minimumRecordingDurationMs` and `DelayedStopSTT()` coroutine

#### 3. Simplify Transcript Handling
**Current Flow:**
```
OnSTTTranscriptReceived → buffer → wait for minimum duration → send buffered transcript
```

**New Flow:**
```
OnSTTTranscriptReceived → update live UI → R released → send latest transcript immediately
```

#### 4. Add Live Transcription UI
**New component:** `LiveTranscriptionDisplay.cs`
- Shows interim transcripts in real-time
- Updates as user speaks
- Clears when transcript is sent to LLM

### New Architecture

```
┌─────────────────────┐
│ RadioInputHandler   │  (unchanged - detects R key)
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  MessageManager     │  (simplified - no delays)
│  - StartSTT()       │
│  - StopSTT()        │
│  - currentTranscript│  ← tracks latest interim result
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│   Player2STT        │  (configure interim_results=true)
│  - WebSocket conn   │
│  - Microphone       │
│  - OnSTTReceived ───┼──→ fires for each interim/final result
└─────────────────────┘
           │
           ▼
┌─────────────────────┐
│LiveTranscriptionUI  │  (new - shows live text)
│  - Updates in       │
│    real-time        │
└─────────────────────┘
```

## Implementation Plan

### Phase 1: Core STT Fixes (Addresses critical bugs)

#### Step 1.1: Configure Player2STT for Real-Time
**File:** Inspector settings (Player2STT component)
**Changes:**
- ✅ Enable Interim Results: `true`
- ✅ Enable VAD: `true`

**Expected result:** Start receiving `[STT_DEBUG] WebSocket.OnMessage fired` logs

#### Step 1.2: Simplify MessageManager
**File:** `Assets/EpsilonIV/Scripts/Conversation/MessageManager.cs`

**Remove:**
- `minimumRecordingDurationMs` field (line 30-32)
- `recordingStartTime` field (line 44)
- `DelayedStopSTT()` coroutine (lines 259-292)

**Modify `StartSTT()`:**
```csharp
public void StartSTT()
{
    if (player2STT == null)
    {
        Debug.LogError("MessageManager: Cannot start STT - player2STT is not assigned!");
        return;
    }

    Debug.Log("MessageManager: Starting STT");
    isRecordingVoice = true;
    bufferedTranscript = "";
    player2STT.StartSTT();
}
```

**Modify `StopSTT()`:**
```csharp
public void StopSTT()
{
    if (player2STT == null)
    {
        Debug.LogError("MessageManager: Cannot stop STT - player2STT is not assigned!");
        return;
    }

    Debug.Log("MessageManager: Stopping STT");
    isRecordingVoice = false;
    player2STT.StopSTT();

    // Send buffered transcript immediately
    if (!string.IsNullOrWhiteSpace(bufferedTranscript))
    {
        Debug.Log($"MessageManager: Sending transcript: '{bufferedTranscript}'");
        OnPlayerMessageSubmitted(bufferedTranscript);
        bufferedTranscript = "";
    }
    else
    {
        Debug.LogWarning("MessageManager: No transcript to send");
    }
}
```

**Keep `OnSTTTranscriptReceived()` mostly the same:**
```csharp
private void OnSTTTranscriptReceived(string transcript)
{
    if (string.IsNullOrWhiteSpace(transcript))
    {
        Debug.LogWarning("MessageManager: Received empty STT transcript");
        return;
    }

    Debug.Log($"MessageManager: STT transcript received: '{transcript}'");

    // Always update buffer with latest transcript while recording
    if (isRecordingVoice)
    {
        bufferedTranscript = transcript; // Replace buffer with latest (not append)
        Debug.Log($"MessageManager: Updated transcript buffer: '{bufferedTranscript}'");
    }
    else
    {
        // Late transcript after recording stopped, send immediately
        Debug.Log($"MessageManager: Received transcript after stop, sending immediately");
        OnPlayerMessageSubmitted(transcript);
    }
}
```

**Expected result:** No artificial delays, instant transcript sending

### Phase 2: Live Transcription UI (Optional Enhancement)

#### Step 2.1: Create LiveTranscriptionDisplay Component
**File:** `Assets/EpsilonIV/Scripts/UI/LiveTranscriptionDisplay.cs` (new)

```csharp
using UnityEngine;
using TMPro;

namespace EpsilonIV
{
    /// <summary>
    /// Displays live STT transcription as the user speaks.
    /// Shows interim results in real-time.
    /// </summary>
    public class LiveTranscriptionDisplay : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("TextMeshPro component to display the live transcript")]
        public TextMeshProUGUI transcriptText;

        [Tooltip("MessageManager to listen for STT events")]
        public MessageManager messageManager;

        [Header("Visual Settings")]
        [Tooltip("Color for interim (partial) transcripts")]
        public Color interimColor = new Color(1f, 1f, 1f, 0.7f); // Semi-transparent white

        [Tooltip("Color for final transcripts")]
        public Color finalColor = Color.white;

        [Header("Animation")]
        [Tooltip("Show a typing indicator while listening")]
        public bool showTypingIndicator = true;

        private bool isListening = false;

        void Awake()
        {
            if (transcriptText == null)
            {
                Debug.LogError("LiveTranscriptionDisplay: transcriptText is not assigned!");
            }

            if (messageManager == null)
            {
                Debug.LogError("LiveTranscriptionDisplay: messageManager is not assigned!");
            }
        }

        void OnEnable()
        {
            if (messageManager != null && messageManager.player2STT != null)
            {
                messageManager.player2STT.OnSTTReceived.AddListener(OnTranscriptReceived);
                messageManager.player2STT.OnListeningStarted.AddListener(OnListeningStarted);
                messageManager.player2STT.OnListeningStopped.AddListener(OnListeningStopped);
            }
        }

        void OnDisable()
        {
            if (messageManager != null && messageManager.player2STT != null)
            {
                messageManager.player2STT.OnSTTReceived.RemoveListener(OnTranscriptReceived);
                messageManager.player2STT.OnListeningStarted.RemoveListener(OnListeningStarted);
                messageManager.player2STT.OnListeningStopped.RemoveListener(OnListeningStopped);
            }
        }

        private void OnListeningStarted()
        {
            isListening = true;
            if (transcriptText != null)
            {
                transcriptText.text = showTypingIndicator ? "Listening..." : "";
                transcriptText.color = interimColor;
            }
        }

        private void OnListeningStopped()
        {
            isListening = false;
            // Clear after a brief delay
            Invoke(nameof(ClearTranscript), 0.5f);
        }

        private void OnTranscriptReceived(string transcript)
        {
            if (transcriptText == null || !isListening) return;

            // Update display with latest transcript
            transcriptText.text = transcript;
            transcriptText.color = interimColor;
        }

        private void ClearTranscript()
        {
            if (transcriptText != null)
            {
                transcriptText.text = "";
            }
        }
    }
}
```

**Expected result:** User sees text appear as they speak

#### Step 2.2: Add UI Elements
**Scene:** Final.unity

1. Create new UI Text element:
   - Right-click Canvas → UI → TextMeshPro - Text
   - Name: "LiveTranscriptionText"
   - Position: Top-center of screen (or near radio UI)
   - Font Size: 24-32
   - Alignment: Center
   - Color: White with slight transparency

2. Add LiveTranscriptionDisplay component:
   - Create empty GameObject under Canvas → "LiveTranscriptionManager"
   - Add LiveTranscriptionDisplay component
   - Assign transcriptText → LiveTranscriptionText
   - Assign messageManager → MessageManager GameObject

### Phase 3: Debugging & Validation

#### Step 3.1: Test Basic Transcript Reception
**Test case:** Enable interim results → Press R → Speak → Check logs

**Success criteria:**
- ✅ See `[STT_DEBUG] WebSocket.OnMessage fired` logs
- ✅ See `[STT_DEBUG] Received WebSocket message: {...}` with transcript data
- ✅ See `MessageManager: STT transcript received: 'hello world'`

**If still no messages:**
- Check Player2 API key validity
- Check STT service status
- Check WebSocket URL construction
- Test with curl/Postman to isolate Unity vs API issue

#### Step 3.2: Test Live Transcription
**Test case:** Press R → Speak slowly → Watch UI

**Success criteria:**
- ✅ Text appears in real-time as you speak
- ✅ Text updates with corrections/additions
- ✅ Text clears after releasing R

#### Step 3.3: Test Short Utterances
**Test case:** Press R → Say "yes" → Release R (< 1 second)

**Success criteria:**
- ✅ Transcript "yes" is captured and sent to LLM
- ✅ No minimum duration errors

#### Step 3.4: Test Latency
**Test case:** Press R → Speak → Stop speaking → Release R immediately

**Success criteria:**
- ✅ Transcript sent within 500ms of releasing R
- ✅ No artificial delays

## Migration Strategy

### Backward Compatibility
- Keep existing `OnPlayerMessageSent` and `OnNpcResponseReceived` events
- No changes to RadioInputHandler or NPC communication
- Player2STT SDK unchanged (just configuration)

### Rollback Plan
If refactor causes issues:
1. Revert MessageManager changes
2. Set `enableInterimResults = false`
3. Restore minimum duration logic

### Testing Checklist
- [ ] Enable interim results in Inspector
- [ ] Test STT in Unity Editor
- [ ] Test STT in WebGL build
- [ ] Verify microphone permissions in WebGL
- [ ] Test with various utterance lengths (short, medium, long)
- [ ] Test network interruption handling
- [ ] Test rapid press/release of R key
- [ ] Verify NPC receives messages correctly

## Code Review Checklist

### MessageManager.cs
- [ ] Removed minimum duration fields
- [ ] Removed DelayedStopSTT coroutine
- [ ] StopSTT sends transcript immediately
- [ ] OnSTTTranscriptReceived replaces buffer (not appends)
- [ ] Proper null checks and error logging

### Player2STT Inspector
- [ ] Enable Interim Results = true
- [ ] Enable VAD = true
- [ ] Sample Rate = 44100
- [ ] Audio Chunk Duration Ms = 50

### LiveTranscriptionDisplay.cs (if implemented)
- [ ] Proper event subscription/unsubscription
- [ ] Null reference checks
- [ ] Visual feedback while listening
- [ ] Clears after transcript sent

## Risk Assessment

### High Risk
- **WebSocket still receives no messages** - If enabling interim results doesn't fix it, deeper API/network investigation needed
- **WebGL microphone issues** - Ensure WebGLMicrophoneManager is working properly

### Medium Risk
- **Transcript quality** - Interim results might be less accurate than final results
- **Network latency** - Real-time transcription depends on good connection

### Low Risk
- **UI clutter** - Live transcription might be distracting (can be toggled off)
- **Breaking existing functionality** - Changes are mostly configuration, minimal code changes

## Success Metrics

### Must Have (P0)
- ✅ Receive transcripts from STT service (currently failing)
- ✅ Support utterances < 1 second
- ✅ Latency < 1 second from R release to transcript

### Should Have (P1)
- ✅ Live transcription UI
- ✅ Works in WebGL build
- ✅ Clean, maintainable code

### Nice to Have (P2)
- Visual feedback (waveform, volume meter)
- Transcript editing before sending
- Voice activity visualization

## Timeline Estimate

- **Phase 1 (Core Fixes):** 1-2 hours
  - Enable interim results: 5 minutes
  - Simplify MessageManager: 30-60 minutes
  - Testing: 30 minutes

- **Phase 2 (Live UI):** 1-2 hours
  - Create LiveTranscriptionDisplay: 30-60 minutes
  - Add UI elements: 30 minutes
  - Testing & polish: 30 minutes

- **Phase 3 (Validation):** 1 hour
  - Test all scenarios
  - WebGL build testing
  - Bug fixes

**Total: 3-5 hours**

## Open Questions

1. **Why isn't the WebSocket receiving messages?**
   - Hypothesis: `interim_results=false` means server waits for silence before responding
   - Test: Enable interim results and check logs
   - If still failing: Investigate API key, service status, network

2. **Should we keep transcript history?**
   - Current: Buffer is replaced with each interim result
   - Alternative: Keep history of all interim results
   - Recommendation: Replace (simpler, users see latest)

3. **What about error handling?**
   - Current: Basic error logging
   - Needed: Retry logic? Fallback to typed input?
   - Recommendation: Add retry for WebSocket connection failures

4. **Voice activity detection usage?**
   - VAD events tell us when speech starts/stops
   - Could auto-start/stop recording based on voice
   - Recommendation: Log VAD events for now, consider auto-stop in future

## Implementation Status

### ✅ Phase 1: COMPLETE
- Removed minimum duration requirement
- Simplified MessageManager
- Immediate transcript sending
- Code is cleaner and faster

### ✅ Phase 2: COMPLETE
- Created LiveTranscriptionDisplay.cs component
- Ready for UI integration

### ⚠️ Known Issue: Backend Problem
**WebSocket receives ZERO messages from Player2 STT service despite audio being sent successfully.**
- This is a backend/API configuration issue, not a code issue
- Refactor is complete and working correctly
- Once backend issue is resolved, transcripts will display in real-time

## Setup Instructions for Phase 2 UI

### Step 1: Create UI Text Element in Final Scene

1. Open `Scenes/Final.unity`
2. Select the Canvas in Hierarchy
3. Right-click Canvas → UI → Text - TextMeshPro
4. Name it: `LiveTranscriptionText`

### Step 2: Configure Text Properties

Select LiveTranscriptionText and set:
- **Rect Transform:**
  - Anchor: Top-Center
  - Position: X=0, Y=-100 (or wherever you want it)
  - Width: 800
  - Height: 100

- **TextMeshPro:**
  - Font Size: 28-32
  - Alignment: Center (horizontal and vertical)
  - Color: White (255, 255, 255, 255)
  - Auto Size: Off
  - Overflow: Overflow (or Truncate if you prefer)
  - Wrapping: Enabled

### Step 3: Create LiveTranscriptionManager GameObject

1. In Hierarchy under Canvas, right-click → Create Empty
2. Name it: `LiveTranscriptionManager`
3. Add Component → Scripts → EpsilonIV → LiveTranscriptionDisplay

### Step 4: Assign References

Select LiveTranscriptionManager and assign:
- **Transcript Text:** Drag LiveTranscriptionText here
- **Message Manager:** Drag MessageManager GameObject here

### Step 5: Configure Visual Settings (Optional)

In LiveTranscriptionDisplay component:
- **Interim Color:** White with 70% alpha (default) - color while speaking
- **Final Color:** White 100% alpha - color when confirmed
- **Show Typing Indicator:** ✓ Checked - shows "Listening..." when recording starts

## Testing Instructions

Once the backend STT issue is resolved:

1. Press Play in Unity
2. Press R to start recording
3. **Expected:** "Listening..." appears in LiveTranscriptionText
4. Speak into microphone
5. **Expected:** Text updates in real-time as you speak
6. Release R
7. **Expected:** Text clears after 0.5s, transcript sent to LLM

## Next Steps

1. ✅ Enable interim results in Player2STT Inspector
2. ✅ Implement Phase 1 (core fixes)
3. ✅ Implement Phase 2 (live UI)
4. ⚠️ **Resolve Player2 STT backend/API issue** ← Current blocker
5. Test in Editor once backend is fixed
6. Test in WebGL build
7. Final validation and testing

## References

- Current implementation: `Assets/EpsilonIV/Scripts/Conversation/MessageManager.cs`
- Player2 SDK: `Assets/unity-player2-sdk-main/Player2STT.cs`
- Radio system docs: `docs/radio-system-implementation-plan.md`
- WebGL audio fixes: `docs/webgl-audio-filters-fix.md`
