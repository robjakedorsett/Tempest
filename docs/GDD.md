# Tempest — Game Design Document

> **Codename:** Tempest (placeholder)
> **Genre:** Co-op Roguelite FPS
> **Players:** 2-4 (online co-op, solo viable)
> **Engine:** Unity 6 (URP)
> **Platform:** PC (Steam)
> **Status:** Pre-production / Concept

---

## 1. Concept

### Elevator Pitch

A 2-4 player co-op roguelite FPS set in the ruins and wilds of Southeast Asian mythology. Squads drop into procedurally generated expeditions — temple complexes, flooded cave networks, volcanic jungle — fighting through swarms of mythological creatures and elite beasts. Random loadouts force adaptation every run. Between runs, players upgrade a shared hub and progress individual characters.

### Tone

Over-the-top action. The world is dangerous and the mythology is treated with respect, but the gameplay is loud, fast, and chaotic. Think "Doom meets Deep Rock Galactic in a Cambodian temple."

### Reference Games

- **Deep Rock Galactic** — co-op expedition structure, extraction tension, squad banter
- **Risk of Rain 2** — in-run power scaling, build variety
- **Gunfire Reborn** — FPS roguelite with mythological theme
- **Helldivers 2** — over-the-top co-op action, light class roles
- **Mega Bonk** — in-run blessing/upgrade stacking and synergy system

### Pillars

1. **Satisfying gunfeel** — non-negotiable. Weapons must feel punchy and impactful.
2. **Your build, transformed** — pick a weapon you like, then watch blessings turn it into something you never expected.
3. **Co-op chaos creates stories** — the funny moments emerge naturally from the systems.
4. **Massive replayability** — procedural generation, stacking blessings, and build variety mean no two runs feel the same.

---

## 2. Setting — Southeast Asian Mythology

### Why Southeast Asian

- **Zero competition** in the co-op FPS space. Visually and thematically distinct from the crowded Norse/medieval/Egyptian landscape.
- **Visual distinctiveness** — naga temples, jungle ruins, volcanic caves. Screenshots stand out immediately on a Steam page.
- **Deep bestiary** — enormous roster of creatures with naturally distinct silhouettes and attack patterns, spanning swarm fodder to towering bosses.
- **Biome diversity** — jungle, flooded caves, volcanic depths, temple ruins all feel grounded and distinct.

### Cultural Approach

The mythology provides the lore depth for players who want to dig in, but moment-to-moment gameplay just needs clear enemy silhouettes and distinct attack patterns. A giant serpent warrior charging at you reads instantly regardless of whether you know it's a Naga. Treat the source material with respect — draw inspiration, don't parody.

---

## 3. Core Loop — The Expedition

### Mission Flow

1. **Drop** — squad lands at the edge of a procedurally generated level with their random loadouts.
2. **Push** — fight through connected zones toward the objective. Enemies escalate in density and type the deeper you go. Loot and resources scattered throughout.
3. **Objective** — reach and complete the primary goal (clear a nest, retrieve a relic, slay an elite beast, activate temple seals). Each biome has its own objective pool.
4. **Extraction** — once the objective is done, an extraction point opens. Getting there isn't free — the level gets more aggressive. Optional: push deeper for bonus loot at increased risk instead of extracting immediately.

### Run Length

15-25 minutes. Short enough to fit "one more run" into an evening.

### Failure

If the whole squad goes down, the run fails. Players keep some resources but lose bonus loot. No permadeath on characters — you just lose the run's rewards.

---

## 4. Biomes

Three biomes at launch, expandable later.

### Sunken Temple

Flooded stone corridors, trap rooms, vertical shafts. Tight sightlines, ambush-heavy. Linear-ish corridors with branching side rooms.

### Jungle Ruins

Overgrown crumbling structures in dense canopy. Open combat arenas connected by narrow jungle paths. Mix of indoor/outdoor. More exploration, flanking routes, verticality through canopy and ruins.

### Volcanic Caverns

Underground lava networks, unstable terrain, heat hazards. Sprawling cave networks with lava hazards and unstable bridges. Most open, most dangerous terrain. Deep Rock energy but tropical.

---

## 5. Combat

### Weapons

Players **choose their loadout** before each run: a **primary weapon**, a **secondary weapon**, and one **consumable item** (grenade/trap/totem). You pick weapons you enjoy and learn to master — but blessings transform how they behave mid-run, creating the build variety. New weapons are unlocked through meta progression (Armoury upgrades), expanding the pool over time.

No swapping weapons mid-run. You commit to your choices at the start. Temporary weapon pickups can still be found in the level as situational power-ups.

**Weapon pool examples (mythologically themed):**

| Slot | Examples |
|---|---|
| **Primary** | Spirit cannon, naga fang rifle, temple repeater, volcanic launcher, monsoon shotgun |
| **Secondary** | Bone pistol, jade dart gun, serpent revolver |
| **Consumable** | Fire totems, smoke bombs, spirit traps, swarm grenades |

Each weapon should feel distinct. Blessings are what make the same weapon play differently run-to-run — ricochet rounds on a shotgun plays completely differently to chain lightning on that same shotgun.

### Combat Feel

- Fast TTK on swarms — players should feel powerful mowing through fodder
- Longer TTK on elites — squad needs to focus fire
- Over-the-top effects on kills — satisfying feedback is non-negotiable
- Swarm pressure forces ammo management and positioning

### Enemy Roster

| Tier | Examples | Role |
|---|---|---|
| **Swarm** | Phi spirits, jungle imps, temple guardians (animated statues), insect hordes | Pressure the squad, force ammo management. Die fast but overwhelm in numbers. |
| **Special** | Leyak (flying heads that dive-bomb), Toyol (fast gremlins that steal resources), Jenglot (ambush predators that cling to walls) | Disruptors. Force players to react and reprioritize mid-fight. |
| **Elite** | Naga warriors (armored serpent soldiers), Rakshasa (shapeshifters that mimic squad callouts), Garuda knights (aerial heavy hitters) | Dangerous 1v1. Squad needs to focus fire. Each has distinct attack patterns to learn. |
| **Boss** | Naga King, Ravana (multi-armed demon lord), Bakunawa (moon-eating sea serpent) | End-of-expedition fights. Multi-phase, arena-based. The spectacle moment. |

---

## 6. In-Run Upgrades — Blessings

### Acquisition

- Killing enemies and completing zone objectives earns **spirit essence** (XP).
- On level-up, each player gets a **choice of 3 random blessings** — pick one.
- Blessings stack and synergize. Discovering broken combos with whatever the RNG gives you is core to the fun.

### Blessing Categories

| Category | Examples |
|---|---|
| **Weapon Mods** | Fire rate boost, ricochet rounds, explosive shots, chain lightning, lifesteal on hit |
| **Survivability** | Health regen, damage resistance, dodge roll, faster revival |
| **Squad Aura** | Buffs that affect nearby teammates — ammo regen, damage boost, enemy slow |
| **Mythological Blessings** | Bigger, rarer effects themed to SE Asian deities. Vishnu's arms (dual wield), Garuda's wings (double jump + hover), Naga's venom (all attacks poison) |

### Power Curve

By extraction, a well-stacked build should feel dramatically more powerful than when you dropped in. That power curve is what makes the "push deeper vs extract safely" decision interesting — your build might be cracked enough to risk it.

### Shrines

Occasionally find a shrine mid-run where you can spend resources to reroll a blessing or pick from a curated set. Small amount of agency over the RNG.

---

## 7. Light Classes

Each player picks a class before the run. Classes give a **passive trait** and **one active ability** on a cooldown. Loadouts are still random — your class flavors how you use whatever you get, not what you get.

**No class is required.** A squad of 4 Invokers should be viable (just chaotic). Classes add flavor and soft roles, not hard dependencies.

### Class Roster (4 at launch)

| Class | Passive | Active Ability | Playstyle |
|---|---|---|---|
| **Warden** | Reduced damage taken, draws enemy aggro more | **Stone Skin** — brief invulnerability, taunts nearby enemies | Frontliner. Gets in the thick of swarms so teammates can breathe. |
| **Seeker** | Faster movement, marks enemies hit for bonus squad damage | **Spirit Sight** — reveals all enemies and loot through walls for a short duration | Scout/support. Finds the path, sets up kills for others. |
| **Invoker** | Blessings are slightly more powerful | **Wrath** — next 3 shots deal massive bonus damage with AOE | Burst damage. Makes the most of whatever random weapon they get. |
| **Mender** | Slow passive heal to nearby downed teammates | **Restoration Totem** — places a healing zone for the squad | Sustain. Keeps the team alive during elite fights and extraction chaos. |

---

## 8. Meta Progression

### The Sanctuary (Hub)

A shared base camp that the squad returns to between runs. Starts as a basic jungle clearing, upgrades into a proper outpost over time. Social space where players hang out, manage loadouts, and prep for the next expedition.

### Hub Buildings

| Building | Function |
|---|---|
| **Armoury** | Expands the random weapon pool. More weapons unlocked = more loadout variety per run. |
| **Shrine of Blessings** | Unlocks new in-run blessings. Higher tier = rarer, more powerful blessings can appear. |
| **Workshop** | Craft consumables to bring into runs. Utility items only (grenades, shields, traps). |
| **War Table** | Unlocks new mission types and higher threat levels. Reveals bonus objectives for extra rewards. |
| **Monument** | Cosmetics. Skins, effects, emotes earned through milestones. No gameplay impact. |

### Character Progression

- Each class levels independently via XP from runs.
- Levels award **spirit shards** spent on **skill tree nodes** — small passive bonuses and ability variants.
- Example: Mender's Restoration Totem branches into "wider radius, weaker heal" or "small radius, strong heal + damage buff."
- Keeps individual investment without creating massive power gaps between new and veteran players.

### Resources

| Resource | Source | Use |
|---|---|---|
| **Relics** | Gathered during runs | Hub building upgrades |
| **Spirit Shards** | Earned on character level-up | Character skill tree nodes |
| **Mythic Fragments** | Rare boss drops | High-tier hub upgrades, cosmetics |

Three currencies max. More than that and it starts feeling like a spreadsheet.

---

## 9. Procedural Generation

### Two-Layer Approach

Generation is split into two layers with different strategies:

**Layer 1: Geometry (offline, tool-assisted, human-curated)**

Level layouts are assembled from room tiles by an editor tool, then baked into playable scenes. These are not generated at runtime — they're pre-validated maps with baked NavMesh, tested sightlines, and no stuck spots. The tool accelerates authoring but a human curates and playtests the output.

- Build a library of validated maps per biome (target: 15-30 per biome for variety)
- Game picks a map from the pool each run
- Room tiles are hand-crafted; the tool handles assembly and connection logic
- Output is a normal Unity scene — fully testable in editor

**Layer 2: Gameplay content (runtime-randomized)**

Each run randomizes what populates the baked map:

- Enemy spawn rift placement and composition
- Blessing shrine locations
- Loot distribution
- Objective type and location
- Environmental hazards (which rooms are flooded, which bridges are broken, where the lava flows)

These place dynamically on pre-validated anchor points in the map. This is where run-to-run replayability lives.

### Why Not Full Runtime Procgen

Full runtime geometry generation (ala Deep Rock Galactic) gives infinite layouts but costs months of edge-case debugging — NavMesh holes, unreachable areas, broken sightlines, spawns in walls. For a small team, baked maps with randomized content provides "enough" variety for hundreds of hours (Hades, Gunfire Reborn, and Dead Cells all ship this way) while staying testable and reliable.

### Per-Biome Generation

| Biome | Structure | Character |
|---|---|---|
| **Sunken Temple** | Linear corridors with branching side rooms | Tight, directed, ambush-heavy |
| **Jungle Ruins** | Open hub areas connected by paths | Explorative, vertical, flanking routes |
| **Volcanic Caverns** | Sprawling cave networks | Open, hazardous, most dangerous terrain |

### What Varies Per Run (Runtime Layer)

- Enemy spawn rift positions and wave composition
- Blessing shrine locations
- Loot distribution
- Objective type and location
- Environmental hazards

### What Stays Consistent

- Map geometry (baked, pre-validated)
- Biome art and theme per mission
- Difficulty scales with depth (further from drop = harder)
- Extraction always requires backtracking or a new path out — never trivial

### Map Builder Tool

An editor tool for assembling maps from room tiles:

- Procedurally assembles tile layouts based on biome rules
- Outputs a Unity scene with baked NavMesh
- Human reviews, adjusts, playtests before adding to the map pool
- Places anchor points for runtime content (rift origins, shrine slots, loot zones)
- Validates connectivity (all areas reachable), NavMesh coverage, and spawn distances

### Prototype Approach

For the prototype, hand-craft 1-2 rooms. Spawners, pickups, and blessings randomize at runtime within those rooms. The map builder tool comes later as a production accelerator — not needed to prove the fun.

---

## 10. Multiplayer & Technical

### Networking

- 2-4 player co-op, online. **Host-based** (one player hosts, others connect). No dedicated servers initially.
- **Unity Netcode for GameObjects** as the networking layer. Mirror is a fallback if Netcode proves painful.
- Solo play is hosting with no one joining. All systems work with 1-4 players.

### Lobby Flow

1. Host creates a run from the hub, selects mission
2. Friends join via invite code or Steam friends list
3. Everyone picks classes in the hub, then host launches the expedition

### Networked Systems

| System | Authority |
|---|---|
| Player movement, shooting | Client-predicted, server-authoritative |
| Enemy spawning and AI | Host-authoritative |
| Blessing choices | Per-player, synced for visibility |
| Loot drops and pickups | Host-authoritative |
| Procedural level | Host generates seed, clients build from same seed |

### Engine

Unity 6 with URP. Same toolchain as prior projects.

---

## 11. Prototype Scope

The prototype should prove the core fun before building systems. Target: **2 weeks of work.**

### Prototype must have:

- A single room with enemy spawns (swarm + one elite type)
- One weapon that feels good to shoot
- Two players connected via Netcode for GameObjects, synced
- In-run blessing choice on level-up (3 choices, pick one, they stack)
- Extraction trigger to end the run

### Prototype must NOT have:

- Hub/base building
- Character progression
- Procedural generation (use a hand-built test level)
- Multiple biomes
- Full weapon pool
- Polish, UI, menus

### Success criteria:

If two players can connect, shoot enemies, stack blessings, and have fun for 15 minutes in a single hand-crafted room — the core is validated. If it's not fun at this stage, no amount of systems will save it.
