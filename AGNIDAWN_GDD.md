# 🔥 AGNIDAWN — Game Design Document
### *Indian Mythology Survival Roguelite*
**Version:** 0.2 | **Date:** 2026-05-02 | **Engine:** Unity 6 (6000.4.2f1) + URP 17.4.0

---

## 1. CONCEPT OVERVIEW

**Tagline:** *"Survive until Agni rises. 20 minutes against the darkness of ancient India."*

**Genre:** Top-down roguelite bullet-heaven survival  
**Platform:** PC (primary), Android/iOS (secondary)  
**Target Audience:** Fans of 20 Min Until Dawn, Vampire Survivors, Hades — ages 16–35  
**Art Style:** Stylized 2D with toon shading, vibrant Indian colour palette, dynamic URP lighting  

### The Core Premise
A sacred temple is besieged by Asuras, Rakshasas, and demons of the night. At the centre of the courtyard burns the **Agni Kund** — a divine fire altar. If it goes out, darkness wins. You must survive until dawn, when Agni (the fire god) rises and purifies everything in his path.

---

## 2. GAPS FOUND IN 20 MINUTES UNTIL DAWN

| Problem in Original | Our Solution |
|---------------------|-------------|
| Shallow narrative — no story | Rich Indian mythology with lore, shloka fragments, Purana Book |
| Generic weapons — guns feel samey | 10 Divine Astras with completely unique mechanics |
| Forgettable bosses — 1-phase, low impact | 4 multi-phase mythological bosses with arena modifications |
| Single biome — always night sky | 5 distinct Indian environments |
| Thin meta-progression | Full Shrine System with 5 deity shrines |
| No character identity | 5 characters with distinct playstyles rooted in mythology |
| Generic audio | FMOD adaptive Indian classical/folk fusion |
| No environmental storytelling | Each biome has visual narrative of the battle |
| Upgrade tree repetitive after 3 runs | Boon system with 40+ upgrades across 3 tiers |
| Lantern mechanic under-explored | Agni Kund with 5 intensity tiers and Mantra channel mechanic |

---

## 3. CORE GAMEPLAY LOOP

```
START RUN
  → Select Character + Starting Weapon
  → 20-minute timer begins
  → Guard Agni Kund while killing enemies
  → Killing enemies drops XP orbs → Level up → Choose 1 of 3 Divine Boons
  → Every 5 minutes: BOSS appears
  → Agni Dial rises with kill streaks → unlocks passive powers
  → Survive all 20 minutes → VICTORY (Agni rises, cinematic sunrise)
  → Collect Divine Shards → Meta-progression in Shrine menu
REPEAT
```

### Agni Dial (Core Mechanic Evolution)
```
Tier 1: SPARK    — dim glow, no bonus
Tier 2: EMBER    — +10% damage
Tier 3: FLAME    — +25% damage, enemies slow near Kund  
Tier 4: BLAZE    — +40% damage, fire ring around Kund damages enemies
Tier 5: INFERNO  — +60% damage, divine aura, weapons gain element
```
**Mantra Channel:** Hold [E] for 2 seconds while near Kund → boosts Dial by 1 tier, but player is rooted and vulnerable.

---

## 4. CHARACTERS (Selectable)

### 4.1 Arjuna — The Eternal Archer
- **Starting Weapon:** Gandiv (bow)
- **Passive:** Arrows pierce through enemies. Every 10th arrow is a Divya Astra (divine).
- **Special:** Indra's Eye — time slows, next 3 shots do 5x damage
- **Playstyle:** Ranged, precision, high single-target DPS

### 4.2 Bhima — The Titan of Kurukshetra
- **Starting Weapon:** Kaumodaki Gada (mace)
- **Passive:** Kills within 2m radius generate shockwave
- **Special:** Rage of Vayu — berserker mode, 3x speed, 2x damage, 10 seconds
- **Playstyle:** Melee, AoE, tank

### 4.3 Adi Shankaracharya — The Wandering Sage
- **Starting Weapon:** Trishul (thrown)
- **Passive:** Enemies killed by spells drop mana crystals that refill ultimate
- **Special:** Brahman Astra — summons geometric brahmic energy pattern, clears screen
- **Playstyle:** Spell-caster, slow movement, massive AoE

### 4.4 Mohini — The Divine Illusionist
- **Starting Weapon:** Vel (Murugan's spear)
- **Passive:** Dodge roll leaves a phantom decoy that attacks enemies for 3 seconds
- **Special:** Maya Veil — become invisible for 5 seconds, all attacks are crits from stealth
- **Playstyle:** High mobility, evasion, burst crit damage

### 4.5 Hanuman Bhakt — The Devotee
- **Starting Weapon:** Sudarshana Chakra (orbiting disc)
- **Passive:** Agni Kund gains +20% resistance. Healing items give 50% more.
- **Special:** Ram Raksha — protective aura, all nearby allies (+ Kund) take 50% less damage
- **Playstyle:** Support/tank hybrid, Kund protection specialist

---

## 5. DIVINE WEAPONS (Astras)

| Weapon | Source | Mechanic | Upgrade Path |
|--------|--------|----------|-------------|
| **Gandiv** | Arjuna's bow | Rapid-fire piercing arrows | T1: Speed → T2: Split-shot → T3: Indra's Storm (fan of 7) |
| **Trishul** | Shiva's trident | Thrown, returns boomerang-style | T1: Range → T2: Chain (hits 3 enemies) → T3: Shiva's Fury (3 simultaneous) |
| **Sudarshana Chakra** | Vishnu's disc | Orbits player, also thrown on click | T1: +Orbits → T2: Homing → T3: Eternal Spin (permanent orbit storm) |
| **Brahmastra** | Ultimate missile | High-damage AoE, 3 charges, slow reload | T1: Blast radius → T2: Shockwave → T3: Brahma's Wrath (screen-wide) |
| **Agneyastra** | Fire astra | Fireball leaves burning trail, DoT | T1: Trail duration → T2: Explosion on death → T3: Napalm (enemies spread fire) |
| **Varunastra** | Water astra | Water wall that slows + knockbacks | T1: Wall width → T2: Freeze → T3: Flood (whole arena slow zone) |
| **Pashupatastra** | Shiva's ultimate | Screen-clear with cooldown | T1: Recharge speed → T2: Partial charge use → T3: Always available |
| **Kaumodaki Gada** | Vishnu's mace | Melee slam with shockwave | T1: Shockwave radius → T2: Stun → T3: Earthquake (persistent tremor field) |
| **Vel** | Murugan's spear | Thrown, chains lightning between enemies | T1: Chain count → T2: Paralysis → T3: Tempest (tornado on hit) |
| **Pinaka** | Shiva's bow | Slow charge, splits earth on release | T1: Fissure length → T2: Side fissures → T3: Tectonic (full arena cracks) |

---

## 6. ENEMY ROSTER

### Standard Enemies
| Enemy | Type | Behaviour | Weakness |
|-------|------|-----------|----------|
| Asura Grunt | Rusher | Direct charge, medium speed | Fire (Agneyastra) |
| Rakshasa Berserker | Tank-rusher | Charges, ignores knockback | Lightning (Vel) |
| Naga Spitter | Ranged | Kites, shoots poison blobs | Water (Varunastra) |
| Pishach | Stealth | Invisible until 3m range, teleports | Light (Brahmastra) |
| Vetala | Necromancer | Resurrects fallen enemies nearby | Trishul (divine metal) |
| Yaksha Brute | Heavy tank | Slow, armoured, stomps | Gada (physical) |
| Danava Swarm | Swarm | 10+ tiny fast units cluster | AoE (Agneyastra, Brahmastra) |
| Gandharva Archer | Ranged sniper | Maintains distance, headshots | Chakra (homing) |

### Elite Variants (appear after minute 10)
- **Maha-Rakshasa** — Rakshasa Berserker x3 size, splits into 3 on death  
- **Naga Raja** — Naga Spitter with 3-shot burst and poison pool  
- **Preta** — Pishach that permanently cloaks and only shows hitbox on hit  

---

## 7. BOSSES

### 7.1 Ravana — The Demon King (Minute 5)
**Phases:**
1. **10 Heads Phase** — Each head has a different attack pattern (fire breath, ice shards, poison mist, lightning bolt, rock throw, wind gust, dark beam, mind maze, thorn spray, divine nullify). Must kill all 10 heads.
2. **Headless Rage** — Ravana's body charges blindly at double speed, massive AOE slams.
3. **Final Form** — One head regrows (golden), now fires all 10 attacks at once in sequence.

**Arena Modification:** Pillars appear mid-fight that provide cover but can be destroyed.  
**Lore Drop:** "Ravana Stotram" fragment — reveals his tragic origin as a great devotee of Shiva.

### 7.2 Mahishasura — The Buffalo Demon (Minute 10)
**Phases:**
1. **Warrior Form** — Sword and shield combat, parryable attacks.
2. **Half-Transform** — Upper body buffalo, charges with horns, earth-shaking stomps.
3. **Full Buffalo** — Massive, pure charge attacks, creates dust storm that obscures vision.

**Arena Modification:** Arena floor cracks with stomps. Last phase covers half the arena in dust.  
**Lore Drop:** "Devi Mahatmyam" verse — how Durga was manifested to defeat him.

### 7.3 Kali — The Goddess Unchained (Minute 15)
**Phases:**
1. **Controlled Fury** — Chain-sickle attacks, summons Asura grunts.
2. **Blood Frenzy** — Every enemy that dies in her presence (including enemies) makes her stronger. Arena slowly fills with red energy.
3. **Mahakali** — Massive form, extends arms across arena, screen-filling dark AoE that only the Agni Kund's light can stop.

**Arena Modification:** Darkness creeps in from edges; only area near Kund is safe.  
**Special:** Killing enemies near Kali HURTS — strategy shifts to kiting her away from the Kund.  
**Lore Drop:** "Kali Stotram" fragment — explains her role as destroyer who also protects.

### 7.4 Vritra — The Storm Dragon (Minute 20 — FINAL)
**Phases:**
1. **Serpentine Form** — Huge snake-dragon, sweeping tail, lightning breath.
2. **Storm Phase** — Calls rain storm that dims Agni Kund. Thunder strikes. Must defeat while maintaining Kund.
3. **Dawn Blocker** — Tries to physically block the sunrise on the horizon. Player must hit its heart 7 times.

**Arena Modification:** Entire arena becomes a storm. Lightning strikes random positions. Rain falls on Kund.  
**Victory:** Vritra is defeated, rain stops, sun breaks over the horizon. Dawn sequence.  
**Lore Drop:** Full myth of Indra vs Vritra from the Rigveda.

---

## 8. BIOMES

### 8.1 Temple Courtyard (Default / Tutorial)
- Stone Dravidian temple architecture
- Torch-lined paths, lotus pond reflection
- Agni Kund is central altar
- Time of night: deep midnight
- Enemy density: Medium
- Unique hazard: Collapsed pillars block paths (rotate every 3 minutes)

### 8.2 Jungle Ruins — Vana Parva
- Dense foliage, ancient forgotten temple ruins
- Fireflies as ambient particles
- Fog of war at edges
- Time of night: Pre-dawn mist
- Enemy density: High (Nagas dominant)
- Unique hazard: Quicksand patches slow movement

### 8.3 Riverbank Eclipse — Ganga Teer
- Sacred Ganga river, full lunar eclipse overhead
- Glowing water (bioluminescent), mist rising
- Eclipse makes Agni Kund dim faster
- Enemy density: Medium (Pishach dominant)
- Unique hazard: Flood waves every 90 seconds push enemies and player

### 8.4 Burning Battlefield — Kurukshetra
- Scorched earth, fires in background, war banners
- Kurukshetra visual references — chariots wreckage, abandoned weapons
- Dust storms reduce visibility periodically
- Enemy density: Very High (all types, escalated)
- Unique hazard: Falling fire arrows from sky (dodge or block with Chakra)

### 8.5 Pataal — The Underworld
- Underground, glowing blue-purple crystals
- Floating platforms over an abyss
- No conventional "dawn" — Agni Kund must reach Inferno tier to end night
- Enemy density: Elite-heavy
- Unique hazard: Platforms collapse after 20 seconds; must keep moving

---

## 9. DIVINE BOON SYSTEM (Level-Up Upgrades)

On level-up, player chooses 1 of 3 randomly selected boons. Categorised by deity:

**Brahma Boons (Creation)** — Weapon enhancement, projectile count, damage
- Arrow Veda: +2 projectiles to bow weapons
- Srishti Pulse: Weapons gain 15% explosion radius
- Brahma's Breath: Crits leave a fire patch

**Vishnu Boons (Preservation)** — Survivability, healing, shields
- Sankalpa Shield: Start each wave with a 1-hit divine shield
- Amrit Drop: Enemies have 5% chance to drop healing lotus
- Chakra Guard: Sudarshana Chakra blocks one projectile per rotation

**Shiva Boons (Destruction)** — Area effects, chaos, power
- Tandav Step: Movement leaves fire trail for 2 seconds
- Nataraja Spin: Dodge roll creates explosion
- Neelkanth Venom: Weapons gain 20% poison chance

**Saraswati Boons (Wisdom)** — XP, meta-synergies, utility
- Gyana Flow: +25% XP gain
- Viveka: See enemy health bars and weaknesses
- Smriti: Gain 1 extra boon choice at level-up

**Agni Boons (Fire/Dawn)** — Agni Kund buffs, fire damage, synergies
- Agni Raksha: Kund takes 20% less damage
- Jyoti Wave: Kund emits fire pulse every 10 seconds
- Usha's Promise: At minute 19, all stats +50% for final minute

---

## 10. META-PROGRESSION — SHRINE SYSTEM

Between runs, spend **Divine Shards** at the 5 shrines:

| Shrine | Cost | What it Unlocks |
|--------|------|-----------------|
| **Brahma** (Creation) | 50–500 shards | New starting weapons available in runs |
| **Vishnu** (Preservation) | 100–800 shards | New passive boons added to the boon pool |
| **Shiva** (Destruction) | 200–1000 shards | New playable characters |
| **Saraswati** (Wisdom) | 75–400 shards | Lore pages, art gallery, character stories |
| **Lakshmi** (Abundance) | 150–600 shards | Shard multiplier, bonus starting conditions |

**Shard Sources:**
- Surviving (base: 50 shards)
- Each boss killed (+25 shards)
- Run duration reached (every 5 minutes: +10 shards)
- Difficulty multiplier (Tandav: 1.5x, Pralaya: 2x)

---

## 11. UI/UX DESIGN LANGUAGE

### Colour Palette
- **Primary:** Deep Midnight Blue (#0D1B2A)
- **Secondary:** Saffron Gold (#FF9933)
- **Accent:** Sacred Red (#C62828)
- **Divine:** Luminous White-Gold (#FFF176)
- **Fire:** Flame Orange (#FF6B35)

### Typography
- Headers: Custom Sanskrit-inspired serif (Tiro Devanagari)
- Body: Clean sans-serif (Noto Sans Devanagari Latin)
- Flavour text: Italic with shloka font

### Design Elements
- Mandala borders on all UI panels
- Lotus petal health bar (petals fall off as health decreases)
- Agni Dial as a brass oil lamp with growing flame
- Deity card design for boon selection (illuminated manuscript style)
- Intricate kolam/rangoli patterns as loading screens

---

## 12. AUDIO DESIGN

### Adaptive Music (FMOD)
```
State: Menu          → Raga Bhairavi (dawn raga, meditative)
State: Gameplay-Low  → Tabla base layer + ambient temple
State: Gameplay-Mid  → Add shehnai + mridangam
State: Gameplay-High → Full ensemble, urgent tempo
State: Boss         → Unique boss theme (see below)
State: Victory      → Full orchestral sunrise reveal
State: Death        → Silence, then single mournful bansuri
```

### Boss Music Themes
- **Ravana:** "Lanka Dahan" — aggressive dhol drums, dark veena
- **Mahishasura:** "Mahishasura Mardini" inspired — powerful, rhythmic
- **Kali:** "Kali Tandav" — chaotic polyrhythm, discordant strings
- **Vritra:** "Storm of Vritra" — orchestral, lightning-fast tempo

---

## 13. TECHNICAL ARCHITECTURE

### Unity Systems
```
Core/
  GameManager.cs        — singleton, game state machine
  EventBus.cs           — decoupled event system
  ObjectPool.cs         — pooling for bullets, enemies, VFX
  SaveSystem.cs         — JSON save to persistent data path
  TimeManager.cs        — game time, pause, slow-motion

Player/
  PlayerController.cs   — input, movement, dodge
  PlayerStats.cs        — health, speed, damage stats
  UpgradeManager.cs     — boon application
  CharacterData.cs      — ScriptableObject per character

Weapons/
  WeaponBase.cs         — abstract base
  ProjectileBase.cs     — pooled projectile
  AstraData.cs          — ScriptableObject per weapon

Enemies/
  EnemyBase.cs          — abstract base with state machine
  WaveManager.cs        — spawn curves, difficulty scaling
  EnemySpawner.cs       — weighted random spawning

Bosses/
  BossBase.cs           — phase system, arena modification
  BossPhase.cs          — individual phase definition

AgniKund/
  AgniKund.cs           — health, tier system, visual sync
  AgniDial.cs           — tier tracking, blessings application

MetaProgression/
  ShrineManager.cs      — persistent unlock data
  ShardManager.cs       — currency tracking
  LoreManager.cs        — shloka collection
```

### ScriptableObject Architecture
Everything data-driven via ScriptableObjects:
- `CharacterData` — stats, abilities, portrait, backstory
- `AstraData` — base stats, upgrade tiers, VFX references
- `EnemyData` — health, speed, drop table, weakness
- `BoonData` — effect, deity affiliation, tier
- `BiomeData` — music reference, hazards, enemy weights
- `BossData` — phases, arena modifications, lore drop

---

## 14. DEVELOPMENT PHASES & MILESTONES

| Phase | Content | Target |
|-------|---------|--------|
| Phase 0 | Environment setup, repo, project scaffold | Week 1 |
| Phase 1 | Core architecture (GameManager, EventBus, ObjectPool) | Week 2 |
| Phase 2 | Player (1 character), basic movement, health | Week 3 |
| Phase 3 | 2 weapons (Gandiv + Trishul), 2 enemies | Week 4 |
| Phase 4 | Agni Kund mechanic, wave spawner, timer | Week 5 |
| Phase 5 | Level-up / Boon system (10 boons) | Week 6 |
| Phase 6 | First boss (Ravana), basic VFX | Week 7–8 |
| Phase 7 | Temple Courtyard biome, full art pass | Week 9–10 |
| Phase 8 | All 5 characters, all 10 weapons | Week 11–12 |
| Phase 9 | All 4 bosses | Week 13–15 |
| Phase 10 | Meta-progression / Shrine system | Week 16–17 |
| Phase 11 | All 5 biomes | Week 18–20 |
| Phase 12 | Audio integration (FMOD) | Week 21–22 |
| Phase 13 | VFX polish, shaders | Week 23–24 |
| Phase 14 | QA, balance, performance | Week 25–26 |
| Phase 15 | Android port, testing | Week 27–28 |
| Release | Steam Early Access + Google Play | Week 30 |

---

## 15. TOOLS & PIPELINE

| Tool | Purpose |
|------|---------|
| Unity 6 (6000.4.2f1) + URP 17.4.0 | Game engine |
| Blender 5.1 | 3D character/enemy modelling, sprite renders |
| FMOD Studio | Adaptive audio |
| DaVinci Resolve | Trailer, cutscenes |
| Figma | UI wireframes and mockups |
| Canva | Marketing materials |
| Linear | Ticket tracking |
| GitHub | Version control (branch: main/develop/feature/FAI-*) |
| Slack | Team communication (#cowork-updates, #cowork-commands, #cowork-approvals) |

---

*"Agni Prajvalito Deva" — May the divine fire be lit.*
