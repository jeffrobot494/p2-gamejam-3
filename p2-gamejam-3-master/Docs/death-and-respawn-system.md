# Death and Respawn System Design

## Overview
When the player dies, they respawn at the entrance of their current room with reduced time on the mission timer. The death sequence includes a dramatic camera fall and fade transitions.

## Core Mechanics
- **Death Penalty:** Time remaining reduced by X seconds
- **Health Reset:** Player respawns with full health
- **Inventory Preserved:** Player keeps all items
- **Room-based Respawn:** Player returns to entrance of current room
- **No Level Reset:** Environment state is preserved (doors open, items used, etc.)

## Components

### 1. GameTimer (New)
**Purpose:** Manages countdown timer for the mission

**Responsibilities:**
- Counts down from initial time
- Displays time remaining
- Reduces time on player death
- Broadcasts event when time runs out
- Can be paused/resumed

**Key Methods:**
```csharp
void ReduceTime(float seconds)
void PauseTimer()
void ResumeTimer()
bool IsTimeUp()
float GetTimeRemaining()
```

### 2. DeathManager (New)
**Purpose:** Orchestrates the entire death and respawn sequence

**Responsibilities:**
- Listens for `PlayerDeathEvent`
- Coordinates all death/respawn systems
- Manages timing of death sequence
- Handles fade transitions
- Communicates with other systems

**Key Methods:**
```csharp
void OnPlayerDeath()
IEnumerator DeathSequence()
void RespawnPlayer()
```

### 3. RoomCheckpoint (New)
**Purpose:** Defines respawn points for each room

**Responsibilities:**
- Stores respawn position and rotation
- Detects when player enters room (via trigger)
- Registers itself as current checkpoint
- Visual representation in editor (gizmo)

**Key Properties:**
```csharp
Transform RespawnPoint
bool IsCurrentCheckpoint
```

### 4. DeathCameraController (New)
**Purpose:** Handles camera animation during death

**Responsibilities:**
- Animates camera falling to ground
- Rotates camera to sideways view
- Resets camera to normal after respawn
- Smoothly transitions between states

**Key Methods:**
```csharp
IEnumerator FallOver()
void ResetCamera()
```

### 5. Integration with Existing Systems

**Health System:**
- Already triggers `PlayerDeathEvent`
- DeathManager listens for this event
- Reset to full health on respawn

**PlayerCharacterController:**
- Disabled during death sequence
- Re-enabled after respawn

**FadeCanvasGroup:**
- Used for fade to/from black transitions
- Can reuse existing fade system or create dedicated one

## Death Sequence Flow

```
1. Player Health reaches 0
   ↓
2. Health System broadcasts PlayerDeathEvent
   ↓
3. DeathManager receives event and starts DeathSequence()
   ↓
4. Disable PlayerController (player can't move)
   ↓
5. DeathCameraController.FallOver()
   - Camera tilts and drops to ground level
   - Rotates to sideways view
   ↓
6. Wait 1-2 seconds (let player see ground view)
   ↓
7. Fade to black (1 second)
   ↓
8. While screen is black:
   - Teleport player to last RoomCheckpoint position
   - Reset Health to full
   - GameTimer.ReduceTime(X seconds)
   - Reset camera rotation to normal
   ↓
9. Fade from black (1 second)
   ↓
10. Enable PlayerController (player can move)
```

## Implementation Stages

### Stage 1: Game Timer
- Create GameTimer component
- Implement countdown logic
- Create UI display
- Test timer reduction

### Stage 2: Basic Respawn
- Create RoomCheckpoint system
- Implement checkpoint detection
- Teleport player on death
- Reset health
- No animations yet - just functional

### Stage 3: Death Camera Animation
- Create DeathCameraController
- Implement fall/tilt animation
- Test camera transitions

### Stage 4: Death Manager Integration
- Create DeathManager
- Connect all systems
- Add fade transitions
- Implement full death sequence

### Stage 5: Polish
- Tune timing values
- Add sound effects
- Test edge cases
- Balance time penalty

## Configuration Values

**GameTimer:**
- Initial time: TBD (e.g., 300 seconds / 5 minutes)
- Death penalty: TBD (e.g., 30 seconds)

**Death Camera:**
- Fall duration: ~1 second
- Ground view duration: ~1.5 seconds
- Camera tilt angle: 90 degrees (lying on side)

**Fade Transitions:**
- Fade to black duration: 1 second
- Fade from black duration: 1 second

## Testing Checklist

- [ ] Timer counts down correctly
- [ ] Timer penalty applies on death
- [ ] Player respawns at correct checkpoint
- [ ] Health resets to full
- [ ] Inventory is preserved
- [ ] Environment state is preserved
- [ ] Camera animation plays smoothly
- [ ] Fade transitions work
- [ ] Player can't move during death sequence
- [ ] Multiple deaths in a row work correctly
- [ ] Death in different rooms uses correct checkpoints

## Future Enhancements (Optional)

- Audio cues for death/respawn
- Particle effects on respawn
- Screen effects (vignette, distortion) during death
- Variable time penalties based on how player died
- Checkpoint save system between sessions
- Death counter/statistics
