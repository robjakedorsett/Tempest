# Tempest (Codename)

## Project Overview

Tempest is a co-op roguelite FPS set in Southeast Asian mythology. 2-4 players drop into procedurally generated expeditions (sunken temples, jungle ruins, volcanic caverns), fight through swarms of mythological creatures with random loadouts, stack in-run blessings into powerful builds, and extract. Between runs, players upgrade a shared hub ("The Sanctuary") and progress individual light classes.

**Full game design:** [docs/GDD.md](docs/GDD.md) — read this first for the complete vision.

## Status

Pre-production. No code yet. The GDD is the first deliverable.

## Quick Reference

- **Engine:** Unity 6 (6000.x) with URP
- **Networking:** Unity Netcode for GameObjects (host-based, 2-4 players)
- **Platform:** PC (Steam)
- **Tone:** Over-the-top action. "Doom meets Deep Rock Galactic in a Cambodian temple."

## Key Design Decisions

- **Southeast Asian mythology** — chosen for zero competition in the co-op FPS space, visual distinctiveness, and deep enemy bestiary
- **Player-chosen loadouts** — players pick their primary, secondary, and consumable before each run. No swapping mid-run. Weapon pool expands via meta progression.
- **In-run blessings** — XP from kills triggers level-ups with a choice of 3 random blessings that stack and synergize (Mega Bonk style)
- **Light classes** — 4 classes (Warden, Seeker, Invoker, Mender) each with a passive and one active ability. Soft roles, no hard dependencies.
- **Meta progression** — hub building upgrades (shared) + character skill trees (individual). Spirit shards from level-ups, relics from runs, mythic fragments from bosses.
- **Host-based networking** — no dedicated servers. Solo is just hosting alone.

## Prototype Goal

Prove the core fun in ~2 weeks: two players connected, one room, enemies, one weapon that feels good, blessing stacking. If it's fun at this stage, build out. If not, the core needs rethinking. See GDD Section 11.

## Reference Games

- Deep Rock Galactic (expedition structure, extraction, co-op feel)
- Risk of Rain 2 (in-run power scaling)
- Gunfire Reborn (FPS roguelite with mythology)
- Helldivers 2 (over-the-top co-op action)
- Mega Bonk (blessing/upgrade stacking system)

## Conventions

To be established during prototype development. Follow Unity 6 best practices and patterns from prior projects (hierarchical state machines, event bus, singleton managers) where appropriate — but don't over-architect before the game's shape is clear.
