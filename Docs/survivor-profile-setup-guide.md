# SurvivorProfile Setup Guide

**Date:** 2025-01-30
**Status:** Complete - Phase 1

---

## Overview

This guide explains how to integrate SurvivorProfile ScriptableObjects with your NPC GameObjects to give each NPC a unique personality, voice, and knowledge base.

---

## What Was Changed

### Player2Npc Component Update

The `Player2Npc` component now supports **optional** SurvivorProfile integration:

- **New Field:** `Survivor Profile Integration` section in the Inspector
- **Behavior:** If a SurvivorProfile is assigned, it overrides the default hardcoded NPC fields
- **Backward Compatible:** NPCs without a SurvivorProfile assigned will continue to use the default "Victor J. Johnson" configuration

---

## How to Assign SurvivorProfiles to NPCs

### Step 1: Locate Your NPC GameObjects

1. Open your scene in Unity
2. Find the NPCs in your scene hierarchy (they should be in `NPCManager.npc_array`)
3. Each NPC should have a `Player2Npc` component attached

### Step 2: Assign SurvivorProfile to Each NPC

1. Select an NPC GameObject in the hierarchy
2. In the Inspector, find the `Player2Npc` component
3. Look for the **"Survivor Profile Integration"** header
4. Click the circle icon next to the `Survivor Profile` field
5. Select one of the available SurvivorProfile assets:
   - `DrElenaVasquez` - Calm medical officer
   - `MarcusChen` - Nervous engineering technician
   - `CaptainReeves` - Authoritative station commander

### Step 3: Verify Configuration

Once assigned, the SurvivorProfile will provide:
- **Display Name:** What the NPC is called
- **Character Description:** First-person self-description for the AI
- **System Prompt:** Third-person personality and behavior guidelines
- **Voice ID:** Unique voice for text-to-speech
- **Knowledge Base:** What the NPC knows about the station (used in conversations later)

### Step 4: Test NPC Spawning

1. Enter Play mode
2. Watch the Console for spawn messages:
   - ✅ **Good:** `"Using SurvivorProfile 'DrElenaVasquez' for NPC spawn data"`
   - ⚠️ **Warning:** `"No SurvivorProfile assigned to NPC_GameObject. Using default hardcoded NPC configuration."`
3. If you see the warning, go back and assign a SurvivorProfile

---

## Available SurvivorProfiles

### Dr. Elena Vasquez
**Location:** `Assets/EpsilonIV/Data/Survivors/DrElenaVasquez.asset`

**Personality:** Calm, methodical medical professional
**Voice:** Female, reassuring tone
**Location:** Medical Bay Level 2, Quarantine Wing
**Knowledge:** Medical facilities, pharmaceutical storage, emergency medical procedures

---

### Marcus "Sparks" Chen
**Location:** `Assets/EpsilonIV/Data/Survivors/MarcusChen.asset`

**Personality:** Nervous, technical, talks fast when anxious
**Voice:** Male, younger, energetic
**Location:** Engineering Section C, Auxiliary Power Control Room
**Knowledge:** Power systems, engineering layouts, technical diagnostics

---

### Captain Sarah Reeves
**Location:** `Assets/EpsilonIV/Data/Survivors/CaptainReeves.asset`

**Personality:** Authoritative, protective, military discipline
**Voice:** Female, commanding tone
**Location:** Command Center Observation Deck
**Knowledge:** Station layout, tactical assessment, security systems, evacuation procedures

---

## Creating New SurvivorProfiles

### Method 1: Using Unity Editor (Recommended)

1. In the Project window, navigate to `Assets/EpsilonIV/Data/Survivors/`
2. Right-click in the folder
3. Select `Create > Survivors > Survivor Profile`
4. Name your new profile
5. Fill in all the fields in the Inspector:
   - `Survivor ID` - Unique identifier (e.g., "survivor_john")
   - `Display Name` - Name shown to player
   - `Character Description` - First-person self-description
   - `System Prompt` - Third-person personality guide
   - `Voice ID` - TTS voice identifier (use existing ones or fetch from Player2 App)
   - `Knowledge Base` - What this NPC knows about the station
   - `Hiding Spot Position` - World coordinates where they're located
   - `Trigger ID` - Identifier for the location trigger zone
   - `Rescue State` - Current mission status

### Method 2: Duplicate Existing Profile

1. Select an existing SurvivorProfile in the Project window
2. Press `Ctrl+D` (or `Cmd+D` on Mac) to duplicate
3. Rename the duplicate
4. Modify the fields to create a new character

---

## Important Notes

### Voice IDs
The voice IDs used in the test profiles are:
- Dr. Elena Vasquez: `21m00Tcm4TlvDq8ikWAM`
- Marcus Chen: `onwK4e9ZLuTAKqWW03F9`
- Captain Reeves: `EXAVITQu4vr4xnSDxMaL`

You can fetch additional voices using the Player2 SDK's voice fetching feature in the `Player2Npc` component Inspector.

### Knowledge Base Format
The `knowledgeBase` field will eventually be sent as `game_state_info` in chat messages to provide context. Structure it clearly with sections like:
- Survivor's location & memory
- Area layout
- Navigation information
- Environmental details

---

## Troubleshooting

**Problem:** NPC spawns with default "Victor J. Johnson" personality
**Solution:** Check that you assigned a SurvivorProfile in the Inspector and saved the scene

**Problem:** NPC spawns but has wrong personality
**Solution:** Verify you assigned the correct SurvivorProfile to the correct NPC GameObject

**Problem:** Can't find SurvivorProfile assets
**Solution:** Assets are located in `Assets/EpsilonIV/Data/Survivors/` - if missing, they need to be recreated

**Problem:** Voice doesn't match character
**Solution:** Check the `voiceId` field in the SurvivorProfile - you may need to update it to a different voice

---

## Next Steps

This completes **Phase 1: NPC & SurvivorProfile Integration**.

Future phases will:
- Integrate `knowledgeBase` into chat messages as game context
- Connect the radio UI to send messages to NPCs
- Display conversation history
- Add voice input (STT) support

---

**End of Setup Guide**
