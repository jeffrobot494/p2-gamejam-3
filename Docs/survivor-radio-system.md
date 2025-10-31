# Survivor Radio Communication System
**Design Document - Final Draft**

---

## Table of Contents
1. [System Overview](#system-overview)
2. [Game Design](#game-design)
3. [Technical Architecture](#technical-architecture)
4. [Data Structure](#data-structure)
5. [Implementation Components](#implementation-components)
6. [Player2 API Integration](#player2-api-integration)
7. [Implementation Roadmap](#implementation-roadmap)
8. [Testing Plan](#testing-plan)

---

## System Overview

### Concept
The player must locate and rescue survivors hiding throughout the space station. Each survivor has a two-way radio and can communicate with the player via AI-powered voice/text conversations. The player must extract information from survivors about their locations and navigate the station to find them.

### Core Gameplay Loop
1. **Radio Contact Opens** - Player gains communication with a survivor
2. **Information Gathering** - Player asks questions to learn where the survivor is hiding
3. **Navigation** - Player uses clues to navigate the station
4. **Discovery** - Player enters the correct location and finds the survivor
5. **Rescue** - Survivor is marked as rescued, next radio channel opens

### Key Challenge
Survivors don't volunteer all information at once. The player must think critically about what questions to ask. Survivors may have imperfect knowledge, be disoriented, or have personality quirks that affect communication.

---

## Game Design

### Example Scenario: John, Greenhouse Manager

**Setup:**
- John is hiding in the Greenhouse, second floor, west side, third garden from the ladder
- He's in the Giant Peppers garden
- He ran through the greenhouse in complete darkness
- He remembers climbing a ladder and hiding in a garden
- He knows the station layout and which plants grow where

**Player Conversation Flow:**

```
Player: "John, where are you?"
John: "I'm in the greenhouse. Ran here when the aliens showed up."

Player: "How did you get there?"
John: "Climbed a ladder in complete darkness. I'm on the second floor now."

Player: "East or west side?"
John: "I... I think I'm on the west side? The ladder I climbed was on the left when I entered."

Player: "What's growing in your garden?"
John: "Giant Peppers. Why, you hungry? *scoffs*"

Player: "Where do the Giant Peppers grow?"
John: "Second floor, west side, third garden down from the ladder, if you must know."

[Player navigates to greenhouse, climbs west ladder, finds third garden]

Player: "I'm on the second floor west. Which garden are you in?"
John: "Third down the row from the ladder."

[Player enters garden #3, trigger activates, John is rescued]
```

### Survivor Personality Types

Survivors have different communication styles:

- **Know-it-all** (John): Condescending but helpful, won't admit fear
- **Injured**: Slow responses, may pass out mid-conversation
- **Terrified**: One-word answers, needs calming
- **PTSD**: Confused, thinks they're somewhere else
- **Panicked**: Rapid, incoherent speech
- **Newcomer**: Doesn't know station layout well

### Station Layout (Example)

```
Main Hub (Central)
├── Corridor A → Hangar
├── Corridor B → Mine
├── Corridor C → Greenhouse
│   └── Corridor E → Communications Array
└── Corridor D → Dormitories
```

**Greenhouse Layout:**
- Ground floor: 24 enclosed gardens
- Second floor: West side (6 gardens) | East side (6 gardens)
- Access: Separate ladders to each second-floor section
- Gardens grow different plants (John knows which plants are where)

---

## Technical Architecture

### System Components

```
Player2 API (Cloud)
    ↕ WebSocket/HTTP
NpcManager (Unity)
    ↕
SurvivorController (Per Survivor)
    ├── Player2Npc (SDK Component)
    ├── Player2STT (SDK Component - Voice Input)
    ├── SurvivorProfile (ScriptableObject - Data)
    └── RescueTrigger (Collider - Detection)

RadioManager (Game System)
    ├── Active Channel Management
    ├── UI Display
    └── Channel Switching Logic
```

### Communication Flow

```
1. PLAYER SPEAKS/TYPES
   └→ Player2STT (voice) OR InputField (text)

2. BUILD CONTEXT
   └→ SurvivorController.BuildGameStateContext()
       - Adds player location
       - Injects survivor's knowledge base
       - Returns formatted context string

3. SEND TO API
   └→ Player2Npc.SendChatMessageAsync()
       - sender_name: "Player"
       - sender_message: "[player's question]"
       - game_state_info: "[context from step 2]"

4. LLM PROCESSES
   └→ Player2 API
       - Uses character_description
       - Uses system_prompt
       - Uses game_state_info (survivor's knowledge)
       - Generates contextual response

5. RECEIVE RESPONSE
   └→ Player2NpcResponseListener (WebSocket)
       - Receives text response
       - Receives TTS audio (if enabled)

6. DISPLAY/PLAYBACK
   └→ UI displays text
   └→ AudioSource plays voice
```

---

## Data Structure

### SurvivorProfile (ScriptableObject)

**File:** `Assets/EpsilonIV/Scripts/SurvivorProfile.cs`

```csharp
public class SurvivorProfile : ScriptableObject
{
    // Identity
    public string survivorID;              // "john_greenhouse"
    public string displayName;             // "John"

    // Player2 API Configuration
    public string characterDescription;    // First-person: "I am John..."
    public string systemPrompt;            // Third-person: "You are John..."
    public string voiceId;                 // TTS voice ID

    // Survivor's Knowledge
    [TextArea(15, 50)]
    public string knowledgeBase;           // Everything they know (see below)

    // Physical Setup
    public Vector3 hidingSpotPosition;     // World position for trigger
    public string triggerID;               // "greenhouse_2f_west_3"

    // Mission Status
    public RescueState rescueState;        // Undiscovered/Active/Located/Rescued
}

public enum RescueState
{
    Undiscovered,  // Radio not yet open
    Active,        // Can communicate
    Located,       // Player found them
    Rescued        // Led back to ship
}
```

### Knowledge Base Format

All of a survivor's factual knowledge is stored as a single formatted text string:

```
SURVIVOR'S LOCATION & MEMORY:
- John is hiding in the Greenhouse, second floor, west side, third garden from the ladder
- He remembers: running in darkness, finding a ladder, climbing up, hiding in a garden
- He's currently in the Giant Peppers garden

GREENHOUSE LAYOUT:
- Large two-story building connected to Main Hub via Corridor C
- Ground floor: 24 enclosed gardens
- Second floor: Split into west and east sides (not connected to each other)
- West side: 6 gardens, accessible via ladder from ground floor
- East side: 6 gardens, accessible via separate ladder from ground floor

PLANT LOCATIONS (John knows all of these):
- Giant Peppers: Second floor, west side, third garden from the ladder
- Cherry Tomatoes: Ground floor, northwest section
- Lettuce: Ground floor, north central
- Carrots: Ground floor, northeast
- Potatoes: Ground floor, east section
- Strawberries: Second floor, east side, first from ladder

NAVIGATION FROM PLAYER'S LOCATION:
- Main Hub connects to: Hangar (Corridor A), Dormitories (Corridor D), Mine (Corridor B), Greenhouse (Corridor C)
- Greenhouse has side access to Communications Array via Corridor E
- From Hangar to Greenhouse: Exit Hangar → Corridor A → Main Hub → Corridor C → Greenhouse entrance

DOOR CODES (John remembers):
- Greenhouse main entrance: unlocked
- Greenhouse side entrance (Corridor E): 4782
- Main Hub to Corridor C: unlocked

ENVIRONMENTAL DETAILS:
- Greenhouse is warm and humid
- Sounds: ventilation system humming, water dripping
- Smells: fertilizer, peppers, damp soil
```

**Why One Big String?**
- Simple to author and edit
- All goes to LLM as context anyway
- No complex data structures needed
- Easy to add/remove information
- LLM naturally extracts relevant info when asked

---

## Implementation Components

### 1. SurvivorProfile.cs ✅ COMPLETE
**Location:** `Assets/EpsilonIV/Scripts/SurvivorProfile.cs`

**Purpose:** ScriptableObject template for creating survivor data

**Status:** Implemented

**Usage:**
```
Unity: Right-click → Create → Survivors/Survivor Profile
Name: "John_GreenhouseManager"
Fill in all fields in Inspector
```

### 2. SurvivorController.cs ⏳ TO DO
**Location:** `Assets/EpsilonIV/Scripts/SurvivorController.cs`

**Purpose:** Component that manages one survivor NPC

**Responsibilities:**
- Reference to SurvivorProfile data
- Spawn NPC via Player2 API
- Build game_state_info context for each message
- Track rescue state
- Handle trigger detection

**Key Methods:**
```csharp
void SpawnSurvivor()
    - Calls Player2Npc.SpawnNpcAsync()
    - Sends characterDescription, systemPrompt, voiceId

string BuildGameStateContext(string playerMessage)
    - Gets player's current location
    - Appends survivor's entire knowledgeBase
    - Returns formatted context string

void OnPlayerMessage(string message)
    - Builds context
    - Sends to Player2Npc

void OnRescueTriggerEntered()
    - Marks survivor as Located
    - Triggers rescue sequence
```

### 3. RadioManager.cs ⏳ TO DO
**Location:** `Assets/EpsilonIV/Scripts/RadioManager.cs`

**Purpose:** Manages which survivor(s) the player can currently talk to

**Responsibilities:**
- Track active radio channel(s)
- Handle channel switching
- Update UI to show active survivor
- Route player input to correct survivor

**Channel Modes (flexible for future):**
- **Sequential**: One survivor at a time, next opens when rescued
- **Manual**: Player chooses from list
- **Multi-channel**: Multiple open simultaneously
- **Dynamic**: Story-triggered

**Initial Implementation:** Sequential mode only

### 4. RadioUI.cs ⏳ TO DO
**Location:** `Assets/EpsilonIV/Scripts/UI/RadioUI.cs`

**Purpose:** Display radio interface

**UI Elements:**
- Survivor name display
- Message history (conversation log)
- Input field (text fallback)
- Push-to-talk indicator (when using STT)
- Status indicators (connecting, listening, etc.)

### 5. RescueTrigger.cs ⏳ TO DO
**Location:** `Assets/EpsilonIV/Scripts/RescueTrigger.cs`

**Purpose:** Detect when player physically finds survivor

**Implementation:**
```csharp
public class RescueTrigger : MonoBehaviour
{
    public string triggerID;
    public SurvivorController survivor;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            survivor.OnPlayerFound();
        }
    }
}
```

### 6. PlayerLocationTracker.cs ⏳ TO DO
**Location:** `Assets/EpsilonIV/Scripts/PlayerLocationTracker.cs`

**Purpose:** Track player's current zone for context

**Tracks:**
- Current zone/room name
- Nearby landmarks
- Recently unlocked doors

**Usage:** SurvivorController reads this to build context

---

## Player2 API Integration

### SDK Components Used

**NpcManager** (Singleton)
- One per scene
- Stores Client ID and API key
- Handles authentication
- Routes NPC responses

**Player2Npc** (Per Survivor)
- Attached to each survivor GameObject
- Handles spawn and chat API calls
- Receives responses via WebSocket

**Player2STT** (Global or Per Scene)
- Captures microphone input
- Streams to STT service
- Fires OnSTTReceived event with transcript

**Player2NpcResponseListener** (SDK Internal)
- Listens to WebSocket
- Parses incoming responses
- Routes to correct NPC

### API Endpoints Used

**POST /npcs/spawn**
```json
Request:
{
  "name": "John - Greenhouse Manager",
  "short_name": "John",
  "character_description": "I am John, the greenhouse manager...",
  "system_prompt": "You are John, a know-it-all...",
  "tts": {
    "voice_ids": ["voice-id"],
    "speed": 1.0,
    "audio_format": "mp3"
  },
  "keep_game_state": true
}

Response: "npc-uuid-here"
```

**POST /npcs/{npc_id}/chat**
```json
Request:
{
  "sender_name": "Player",
  "sender_message": "Where are you?",
  "game_state_info": "Player Location: Main Hub\n\n--- JOHN'S KNOWLEDGE ---\n[entire knowledgeBase]",
  "tts": "server"
}

Response (via WebSocket):
{
  "npc_id": "npc-uuid",
  "message": "I'm in the greenhouse. Ran here when the aliens showed up.",
  "audio": { base64-encoded-mp3 }
}
```

### Authentication Flow

1. **NpcManager.Start()** checks for API key
2. **AuthenticationUI.Setup(npcManager)** handles auth
3. **Tries Player2 App** (localhost:4315) first
4. **Falls back to browser** OAuth flow if needed
5. **Stores API key** in NpcManager
6. **All NPCs use this key** for subsequent requests

---

## Implementation Roadmap

### Phase 1: Core Data & Setup ✅
- [x] Create SurvivorProfile.cs ScriptableObject
- [x] Set up NpcManager in test scene
- [x] Configure authentication
- [x] Create test SurvivorProfile for John

### Phase 2: Basic Communication ⏳
- [ ] Create SurvivorController.cs
- [ ] Implement SpawnSurvivor()
- [ ] Implement BuildGameStateContext()
- [ ] Wire up Player2Npc component
- [ ] Test text-based conversation with John

### Phase 3: Player Location Tracking
- [ ] Create PlayerLocationTracker.cs
- [ ] Define zone triggers in scene
- [ ] Track player's current location
- [ ] Include location in context

### Phase 4: Physical Discovery
- [ ] Create RescueTrigger.cs
- [ ] Place trigger at John's hiding spot
- [ ] Test rescue detection
- [ ] Handle state transitions

### Phase 5: Radio Management
- [ ] Create RadioManager.cs
- [ ] Implement sequential channel mode
- [ ] Test channel switching on rescue

### Phase 6: UI Development
- [ ] Create RadioUI.cs
- [ ] Design radio interface
- [ ] Show conversation history
- [ ] Display active survivor

### Phase 7: Voice Integration
- [ ] Add Player2STT component
- [ ] Wire up push-to-talk
- [ ] Test voice input → transcript → response

### Phase 8: Multiple Survivors
- [ ] Create 2-3 more SurvivorProfiles
- [ ] Set up their hiding locations
- [ ] Test full rescue loop
- [ ] Balance difficulty

### Phase 9: Polish
- [ ] Add radio static/effects
- [ ] Improve UI visuals
- [ ] Add sound effects
- [ ] Test edge cases

### Phase 10: Integration
- [ ] Integrate with game state manager
- [ ] Connect to mission objectives
- [ ] Add to main game flow

---

## Testing Plan

### Unit Tests

**SurvivorProfile:**
- [ ] Create asset via Unity menu
- [ ] Fill all fields
- [ ] Verify data persists

**SurvivorController:**
- [ ] Spawns NPC successfully
- [ ] Builds context correctly
- [ ] Sends messages to API
- [ ] Receives responses

**RescueTrigger:**
- [ ] Detects player entry
- [ ] Calls correct survivor
- [ ] Updates rescue state

### Integration Tests

**Conversation Flow:**
1. Player asks "Where are you?"
2. LLM responds using knowledgeBase
3. Player asks follow-up questions
4. Responses stay consistent

**Context Building:**
1. Player in different locations
2. Context includes correct player position
3. KnowledgeBase always included
4. Format is correct for API

**Rescue Sequence:**
1. Player talks to John
2. Learns location clues
3. Navigates to greenhouse
4. Enters correct garden
5. Trigger fires
6. John marked as rescued
7. Next channel opens

### Edge Cases

- [ ] What if player asks John something he doesn't know?
- [ ] What if player is rude/hostile?
- [ ] What if LLM hallucinates information not in knowledgeBase?
- [ ] What if player finds hiding spot without talking first?
- [ ] What if player gets lost and needs to ask again?
- [ ] What if network connection drops mid-conversation?

### Performance Tests

- [ ] Multiple survivors spawned (memory usage)
- [ ] Long conversation history (token limits)
- [ ] Rapid message sending (rate limits)
- [ ] WebSocket reconnection handling

---

## Current Implementation Status

### Completed ✅
- Player2 SDK installed and configured
- NpcManager set up in scene
- Authentication working (tested)
- SurvivorProfile ScriptableObject created
- Text-based NPC conversation tested
- Cursor toggle for UI interaction added

### In Progress ⏳
- Creating SurvivorController component
- Designing context injection system
- Planning trigger system

### Not Started ❌
- RadioManager
- RadioUI
- PlayerLocationTracker
- RescueTrigger
- Voice integration
- Multiple survivors

---

## Technical Considerations

### Token Usage
**Each message to API includes:**
- System prompt (~100 tokens)
- Character description (~50 tokens)
- Conversation history (grows over time)
- Game state info (~500-1000 tokens for knowledgeBase)
- Player message (~10-50 tokens)

**Optimization:**
- Start with full knowledgeBase each time (simple)
- Monitor token usage
- If needed, implement smart context pruning later

### Conversation History
- Player2 API maintains history server-side
- Each NPC has separate conversation thread
- History persists across messages automatically
- Consider: Should history persist across game sessions?

### Error Handling
- Network failures during conversation
- API rate limits exceeded
- Invalid/incomplete responses
- TTS generation failures
- Microphone permission denied

### Scalability
- System supports any number of survivors
- Each survivor is independent
- Knowledge bases can be any size
- LLM naturally handles variations in content

---

## Future Enhancements

### Advanced Features (Post-MVP)
- **Multiple active channels** - Talk to 2+ survivors at once
- **Survivor-to-survivor communication** - NPCs talk to each other
- **Dynamic knowledge** - Survivors learn new info during game
- **Injury/condition mechanics** - Survivors get worse over time
- **False information** - Panicked survivors give wrong directions
- **Language barriers** - Non-English speaking survivors
- **Radio degradation** - Signal gets worse in certain areas

### Quality of Life
- **Conversation bookmarks** - Save important clues
- **Map markers** - Auto-mark locations mentioned by survivors
- **Hint system** - Suggest questions if player is stuck
- **Conversation replay** - Review past exchanges
- **Transcript export** - Save conversations for later

---

## Notes & Decisions

### Design Decisions Made
1. **One big knowledgeBase string** - Simpler than complex data structures
2. **Full context each time** - Easier than dynamic context injection
3. **Sequential channel mode first** - Simpler than multi-channel
4. **Text + Voice support** - Text for testing, voice for immersion
5. **Trigger-based rescue** - Simple, reliable detection

### Questions Remaining
1. Should rescued survivors be re-contactable?
2. How to handle if player finds survivor without talking first?
3. Should there be a "wrong location" penalty?
4. How many survivors total?
5. Should final survivor be different/special?

### References
- Player2 API Docs: `./Docs/p2-api.yaml`
- Player2 SDK README: `./Assets/unity-player2-sdk-main/README.md`
- Game State Manager: `./Assets/EpsilonIV/Scripts/GameStateManager.cs`

---

**Document Version:** 1.0
**Last Updated:** 2025-01-30
**Status:** Design Complete, Implementation In Progress
