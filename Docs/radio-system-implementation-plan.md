# Radio System Implementation Plan
**Project:** Epsilon IV Survivor Radio Communication
**Date:** 2025-01-30
**Status:** In Progress

---

## System Architecture Overview

### Component Hierarchy
```
RadioSystem (Root GameObject)
├─ MessageManager
│  └─ References: NPCManager, Player2STT, RadioAudioPlayer
├─ NPCManager (existing - manages NPC array)
├─ Player2STT (Player2 SDK component)
├─ RadioAudioPlayer
└─ RadioUI
   ├─ RadioInputHandler
   ├─ InputFieldController
   ├─ ChatView
   └─ RadioModelController
```

---

## Component Responsibilities

### **RadioInputHandler**
**Purpose:** Detect all player input related to radio communication

**Responsibilities:**
- Detect Enter key press → notify InputFieldController
- Detect R key press/hold → notify MessageManager to start STT
- Detect R key release → notify MessageManager to stop STT
- Get text from input field → pass to MessageManager
- Fire event: `OnMessageSubmitted(string message)`

---

### **InputFieldController**
**Purpose:** Manage text input field UI state

**Responsibilities:**
- Focus/unfocus input field when requested
- Lock/unlock cursor appropriately
- Prevent player movement while typing (disable player action map)
- Clear input field after sending
- Handle Escape to cancel typing

---

### **MessageManager**
**Purpose:** Core message orchestration and API communication

**Responsibilities:**
- Get active NPC from NPCManager
- Receive text from RadioInputHandler via `OnMessageSubmitted`
- Validate message isn't empty
- Get full prompt context from SurvivorProfile (future)
- Send message to Player2Npc API via `SendChatMessageAsync()`
- Subscribe to `Player2Npc.OnNpcResponse` event
- Extract audio from NPC response
- Send text response to ChatView
- Send audio response to RadioAudioPlayer
- **STT Management:**
  - Start STT via `Player2STT.StartSTT()`
  - Subscribe to `Player2STT.OnSTTReceived` event
  - Stop STT via `Player2STT.StopSTT()`
  - Treat STT transcript as text input (same flow)

---

### **NPCManager** (existing)
**Purpose:** NPC state management

**Responsibilities:**
- Maintain array of NPC GameObjects (npc_array)
- Track currently active NPC (currentIndex)
- Switch active NPC (SwitchToNextNpc, DeactivateCurrentNpc)
- Notify listeners when active NPC changes (future)

---

### **ChatView**
**Purpose:** Display conversation history

**Responsibilities:**
- Show player's messages ("Player: [message]")
- Show NPC responses ("[NPC Name]: [response]")
- Show active NPC name
- Display conversation history with scrolling
- Clear chat functionality (future)

---

### **RadioAudioPlayer**
**Purpose:** Play NPC voice with radio effects

**Responsibilities:**
- Receive TTS audio from MessageManager
- Apply radio static overlay
- Apply band-pass filter (radio frequency range)
- Play audio through AudioSource
- Handle audio interruptions (new message cuts off old)
- Control volume/muting

---

### **RadioModelController**
**Purpose:** Visual feedback for radio device

**Responsibilities:**
- Flash lights when sending/receiving transmissions
- Animate decibel meter during STT recording
- Animate decibel meter during audio playback
- Listen to MessageManager events for timing

---

### **SurvivorProfile** (ScriptableObject - existing)
**Purpose:** Store survivor data

**Properties:**
- survivorID
- displayName
- characterDescription (for LLM)
- systemPrompt (for LLM)
- voiceId (for TTS)
- knowledgeBase (context string)
- hidingSpotPosition
- rescueState

---

## Data Flow Diagrams

### **Text Message Flow**
```
1. Player types and presses Enter
   └─> RadioInputHandler detects Enter
       └─> InputFieldController focuses field

2. Player types message and presses Enter again
   └─> RadioInputHandler.OnMessageSubmitted fires
       └─> MessageManager receives message
           └─> Gets active NPC from NPCManager
           └─> Calls Player2Npc.SendChatMessageAsync()
           └─> ChatView displays "Player: [message]"

3. API responds
   └─> Player2Npc.OnNpcResponse fires
       └─> MessageManager receives response
           └─> ChatView displays "[NPC]: [response]"
           └─> RadioAudioPlayer plays audio with effects
```

### **Voice (STT) Message Flow**
```
1. Player presses R
   └─> RadioInputHandler detects R down
       └─> MessageManager.StartSTT()
           └─> Player2STT.StartSTT()
               └─> RadioModelController animates meter

2. Player speaks (STT listening)
   └─> Player2STT streams audio to API

3. Player releases R
   └─> RadioInputHandler detects R up
       └─> MessageManager.StopSTT()
           └─> Player2STT.StopSTT()
               └─> Player2STT.OnSTTReceived fires with transcript
                   └─> MessageManager receives transcript
                       └─> [Same flow as text message from here]
```

---

## Implementation Phases

### **Phase 1: Refactor Input System** ⏳ IN PROGRESS
**Goal:** Split RadioInputManager into two focused components

**Status:** RadioInputManager exists but needs refactoring

**Tasks:**
- [ ] Create `RadioInputHandler.cs` component
  - Detect Enter key press
  - Get text from InputField
  - Fire `OnMessageSubmitted(string)` event
- [ ] Refactor `RadioInputManager.cs` → `InputFieldController.cs`
  - Remove input detection logic
  - Keep only focus/unfocus and cursor management
  - Expose public methods: `FocusInput()`, `UnfocusInput()`, `ClearInput()`
- [ ] Wire them together
  - RadioInputHandler calls InputFieldController methods
  - RadioInputHandler fires event with message text

**Test:**
- Press Enter → field focuses
- Type message → press Enter
- Console log: "Message submitted: [text]"
- Input field clears and unfocuses

**Files to create/modify:**
- `Assets/EpsilonIV/Scripts/Conversation/RadioInputHandler.cs` (NEW)
- `Assets/EpsilonIV/Scripts/Conversation/RadioInputManager.cs` → `InputFieldController.cs` (REFACTOR)

---

### **Phase 2: Basic Chat Display**
**Goal:** See typed messages immediately for visual feedback

**Tasks:**
- [ ] Create `ChatView.cs` component
- [ ] Add UI elements:
  - TextMeshProUGUI for chat history (with ScrollRect)
  - TextMeshProUGUI for active NPC name label
- [ ] Subscribe to `RadioInputHandler.OnMessageSubmitted`
- [ ] Display "Player: [message]" in chat history
- [ ] Auto-scroll to bottom when new message added

**Test:**
- Type message → press Enter
- See "Player: [message]" appear in chat view
- Multiple messages stack vertically

**Files to create:**
- `Assets/EpsilonIV/Scripts/Conversation/ChatView.cs` (NEW)

---

### **Phase 3: MessageManager + NPC Connection**
**Goal:** Actually send messages to API and receive responses

**Tasks:**
- [ ] Create `MessageManager.cs` component
- [ ] Get reference to NPCManager
- [ ] Method: `GetActiveNPC()` - finds first active GameObject in npc_array
- [ ] Subscribe to `RadioInputHandler.OnMessageSubmitted`
- [ ] When message received:
  - Validate not empty
  - Get active NPC
  - Get `Player2Npc` component from NPC GameObject
  - Call `Player2Npc.SendChatMessageAsync("Player", message, "")`
  - Log: "Sent to NPC: [npcName]"
- [ ] Subscribe to `Player2Npc.OnNpcResponse` event
- [ ] When response received:
  - Log: "Received response: [message]"

**Test:**
1. Ensure one NPC in NPCManager.npc_array is SetActive(true)
2. Type message → press Enter
3. Console shows: "Sent to NPC: Victor J. Johnson"
4. Wait 2-5 seconds
5. Console shows: "Received response: [NPC's reply]"

**Files to create:**
- `Assets/EpsilonIV/Scripts/Conversation/MessageManager.cs` (NEW)

---

### **Phase 4: Display NPC Responses**
**Goal:** Complete the visual conversation loop

**Tasks:**
- [ ] MessageManager fires event: `OnNpcResponseReceived(string npcName, string message)`
- [ ] ChatView subscribes to this event
- [ ] Display "[NPC Name]: [response]" in chat history
- [ ] Update active NPC name label

**Test:**
- Full conversation visible: Player messages and NPC responses
- NPC name appears correctly

**Files to modify:**
- `MessageManager.cs` - add event
- `ChatView.cs` - subscribe and display

---

### **Phase 5: Add RadioAudioPlayer**
**Goal:** Play NPC speech with radio effects

**Tasks:**
- [ ] Create `RadioAudioPlayer.cs` component
- [ ] Add AudioSource component
- [ ] Load radio static audio clip (asset)
- [ ] Method: `PlayWithEffects(AudioClip voiceClip)`
  - Mix voice with static overlay
  - Apply AudioLowPassFilter (simulate radio frequency)
  - Apply AudioHighPassFilter (remove low rumble)
  - Play through AudioSource
- [ ] MessageManager extracts audio from `NpcApiChatResponse`
- [ ] MessageManager calls `RadioAudioPlayer.PlayWithEffects(audio)`

**Test:**
- Send message to NPC
- Hear response with radio static effect
- Audio quality sounds like walkie-talkie

**Files to create:**
- `Assets/EpsilonIV/Scripts/Conversation/RadioAudioPlayer.cs` (NEW)

**Assets needed:**
- Radio static audio loop (found/created separately)

---

### **Phase 6: Add STT Support**
**Goal:** Voice input works the same as text input

**Tasks:**
- [ ] Add `Player2STT` component to RadioSystem GameObject
- [ ] Assign NpcManager reference in inspector
- [ ] Configure STT settings (sample rate, VAD, etc.)
- [ ] RadioInputHandler: Detect R key press/release
- [ ] MessageManager: Add methods `StartSTT()` and `StopSTT()`
- [ ] MessageManager subscribes to `Player2STT.OnSTTReceived`
- [ ] When transcript received, treat as text message (call same flow)
- [ ] RadioInputHandler calls MessageManager STT methods

**Test:**
- Hold R key
- Speak: "Hello, are you there?"
- Release R
- See transcript appear in chat as player message
- Get NPC response

**Files to modify:**
- `RadioInputHandler.cs` - add R key detection
- `MessageManager.cs` - add STT methods

---

### **Phase 7: SurvivorProfiles Integration**
**Goal:** Custom NPCs with personalities and knowledge

**Tasks:**
- [ ] Add SurvivorProfile reference to NPC GameObjects
- [ ] MessageManager: Get SurvivorProfile from active NPC
- [ ] Build context string from `SurvivorProfile.knowledgeBase`
- [ ] Pass context to `SendChatMessageAsync()` 3rd parameter:
  ```csharp
  string context = survivorProfile.knowledgeBase;
  npc.SendChatMessageAsync("Player", message, context);
  ```
- [ ] Create test SurvivorProfile ScriptableObject for one NPC
  - Fill in knowledgeBase with test information
  - Set characterDescription and systemPrompt

**Test:**
- Ask NPC: "What do you know about the greenhouse?"
- NPC responds with information from their knowledgeBase
- Personality reflects characterDescription

**Files to modify:**
- `MessageManager.cs` - add SurvivorProfile logic

**Assets to create:**
- Test SurvivorProfile ScriptableObject

---

### **Phase 8: Polish & Additional Features**
**Goal:** Make the system feel polished and complete

**Tasks:**
- [ ] Create `RadioModelController.cs`
  - Flash transmission lights
  - Animate decibel meter during STT
  - Animate decibel meter during audio playback
- [ ] ChatView improvements:
  - Better text formatting
  - Timestamps
  - Clear chat button
  - Max message history limit
- [ ] RadioAudioPlayer improvements:
  - Volume control slider
  - Interrupt old audio when new message arrives
  - Adjustable static intensity
- [ ] NPCManager improvements:
  - Visual indicator of active NPC
  - Better NPC switching UI
- [ ] Input improvements:
  - Visual indicator when input field is focused
  - Push-to-talk indicator for STT
  - Cooldown between messages

**Files to create:**
- `Assets/EpsilonIV/Scripts/Conversation/RadioModelController.cs` (NEW)

---

## Testing Strategy

### Unit Testing
Each phase has specific tests defined above.

### Integration Testing
After Phase 4, full conversation flow should work:
1. Player types message
2. Message appears in chat
3. NPC responds
4. Response appears in chat

After Phase 5, audio playback added to flow.

After Phase 6, voice input alternative to typing.

### Edge Cases to Test
- [ ] No active NPC (should show warning)
- [ ] Multiple NPCs active (should pick first one)
- [ ] Empty message submission (should be blocked)
- [ ] Very long messages (UI scrolling)
- [ ] Rapid message sending (rate limiting?)
- [ ] Network disconnection during message
- [ ] STT permission denied
- [ ] Audio playback interruption

---

## Current Status

**Last Updated:** 2025-01-30

**Phase 1:** ⏳ In Progress
- RadioInputManager exists, needs refactoring into RadioInputHandler + InputFieldController

**Next Steps:**
1. Complete Phase 1 refactoring
2. Test Enter key flow
3. Move to Phase 2

---

## File Structure
```
Assets/EpsilonIV/Scripts/Conversation/
├── RadioInputHandler.cs          (NEW - Phase 1)
├── InputFieldController.cs        (REFACTOR - Phase 1)
├── ChatView.cs                    (NEW - Phase 2)
├── MessageManager.cs              (NEW - Phase 3)
├── RadioAudioPlayer.cs            (NEW - Phase 5)
└── RadioModelController.cs        (NEW - Phase 8)

Assets/EpsilonIV/Scripts/ScriptableObjects/
└── SurvivorProfile.cs             (EXISTS)
```

---

## Dependencies

### Player2 SDK Components Used
- `NpcManager` - API authentication and NPC registry
- `Player2Npc` - NPC spawning and chat messaging
- `Player2STT` - Speech-to-text streaming
- `Player2NpcResponseListener` - WebSocket response handling

### Unity Input System
- InputActionAsset for key detection
- InputAction for Enter and R keys

### TextMeshPro
- TMP_InputField for text input
- TextMeshProUGUI for chat display

---

## Notes

### Design Decisions
1. **MessageManager as orchestrator** - Central point for all message logic makes debugging easier
2. **Separate audio player** - Allows for complex audio effects without cluttering MessageManager
3. **STT treated as text input** - Unified message flow regardless of input method
4. **Events for communication** - Loose coupling between components
5. **Incremental testing** - Each phase has clear, testable goals

### Future Enhancements (Post-MVP)
- Multiple active NPCs (split-screen chat)
- Radio signal strength (distance-based audio quality)
- Message replay/history
- Save conversation transcripts
- Custom audio filters per NPC
- Animated radio UI (spectrum analyzer, signal bars)
- Radio interference (environmental hazards)

---

**End of Implementation Plan**
