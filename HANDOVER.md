# AGNIDAWN — Shared Handover Document
> Both the **live Cowork session** and the **cowork-slack-command-monitor scheduled task** read and update this file.
> Before starting any coding work, READ this file. After finishing, UPDATE the relevant sections.

---

## WHO BUILT WHAT

| Phase | What | Built By | Commit | Status |
|-------|------|----------|--------|--------|
| Phase 1 | Core Architecture (GameManager, EventBus, ObjectPool, TimeManager, SaveSystem, AstraData, BoonData, EnemyData) | Live Session | `46f7cea` | ✅ Done |
| Phase 2 | Player Systems (PlayerController, HealthSystem, AstraController, LevelUpManager, BoonApplier) | Live Session | `46f7cea` | ✅ Done |
| Phase 2 | Unit Tests (EventBusTests, SaveSystemTests) | Live Session | `46f7cea` | ✅ Done |
| Fix | asmdef files + BoonData circular dependency fix | Live Session | `4e36283` | ✅ Done |
| Phase 3 | Enemy Systems (BaseEnemy, EnemyBehaviours x6, SpawnManager) | Live Session | `bc66d60` | ✅ Done |
| Phase 4 | Agni Kund 5-tier system (AgniKund.cs, AgniKundTests, EnemyDataTests) | Live Session | `bc66d60` | ✅ Done |
| Phase 5 | Boss System (BaseBoss, BossData, BossManager, Ravana, Mahishasura, Kali, Vritra, BossSystemTests) | Scheduled Task | `a3effd9` | ✅ Done |
| Fix | AGNIDAWN.Bosses.asmdef + BossSystemTests namespace fix | Live Session | `1307310` | ✅ Done |
| Phase 6 | Astra Weapons — BaseAstraProjectile + 10 divine weapon scripts + AstraSystemTests | Scheduled Task | `b86cfa5` | ✅ Done |
| Phase 7 | Biomes — BiomeData SO, BiomeManager (timed rotation), SpawnManager registry+biome hooks, BaseEnemy slow/knockback consumers, SudarshanaChakra fix, BiomeSystemTests | Scheduled Task | `b9c37c5` | ✅ Done |
| Phase 8 | Meta-Progression & Shrine System — DivineShardManager, ShrineData/Manager (5 shrines), LoreFragment/Manager, DifficultyData/Manager, SaveSystem extended, MetaProgressionTests (20 tests) | Scheduled Task | `c1ddfcf` | ✅ Done |
| Phase 9 | FMOD Audio Integration — AudioEventData SO, AudioManager (FMOD+Unity fallback), MusicManager (biome/boss adaptive), SFXController (EventBus→audio), AudioBusController, AudioSystemTests (25 tests) | Scheduled Task | `2102384` | ✅ Done |
| Phase 10 | URP Post-Processing — AgniTierVisualData SO (5-tier bloom/vignette/CA), AgniVisualStateManager (lerp coroutines), CameraShakeController, ScreenFlashController, VisualSystemTests (18 tests) | Scheduled Task | `43d4100` | ✅ Done |
| Phase 11 | VFX & Shader System — AGNIDAWN.VFX assembly, VFXEventData SO, VFXManager (EventBus→pool), AgniFlameController (5-tier particle/light), PlayerDivineAura (tier 4/5 corona), BossShockwaveController, MandalaFXController (Brahmastra/Pashupatastra), VFXSystemTests (22 tests) | Scheduled Task | `764d117` | ✅ Done |
| Phase 12 | UI/UX System — AGNIDAWN.UI assembly, BaseUIPanel (CanvasGroup fade), UIManager (screen stack), HUDController (health/Agni Dial/timer/XP/kills/shards), MainMenuUI, LevelUpUI (rarity boon cards), BossIntroOverlayUI (cinematic), BossHealthBarUI (lag-bar+pips), PauseMenuUI, DeathScreenUI (4 Sanskrit poems), VictoryScreenUI (sunrise anim), UISystemTests (14 tests). Fixed: GameManager TotalKills/CurrentLevel/RestartRun/ReturnToMainMenu | Live Session | `1307310` | ✅ Done |
| Phase 13 | QA & Build Pipeline — CI/CD rewrite (editmode+playmode+Android APK+auto-tag), PerformanceTests, BuildValidationTests, QASystemTests (20 tests), run_tests.ps1, build_android.ps1, AndroidBuilder.cs, PR template | Live Session | `45505e0`+`3f6af32` | ✅ Done |
| Phase 14 | Mobile Testing — IL2CPP/ARM64 APK (84.7 MB) built with Unity 6.4 batch-mode, installed on Samsung Z Fold7 (RZGYA0KX12E) via ADB, launched UnityPlayerGameActivity, logcat confirmed: Vulkan/Adreno init, AAudio stream, 1080×2520 SurfaceView, SetGameState mode:CONTENT, zero Unity errors | Live Session | — | ✅ Done |
| Phase 18 | Agni Rigged Character — AgniRiggedCharacter.cs: 20-part pivot-bone hierarchy (hip→spine→neck→hair, shoulders→upper_arm→elbow→wrist, hip→knee→ankle), sin-wave procedural idle (breathing, hair flicker, arm sway) + run (leg/arm swing, knee bend, body bob, hair streams back). 20 PNG parts added to Resources/Characters/AgniParts/ (PPU=100). GameBootstrap spawns AgniVisual child with rigged component instead of flat sprite. Verified on Z Fold7: "[AgniRiggedCharacter] Rig built — 20 parts assembled." zero missing-sprite warnings, zero Unity errors. | Live Session | `12f7f7a` | ✅ Done |

---

## ARCHITECTURE — MUST READ BEFORE CODING

### Assembly Dependency Chain (NEVER break this order)
```
AGNIDAWN.Core
    ↓
AGNIDAWN.Player  (refs Core)
    ↓
AGNIDAWN.Enemies  (refs Core + Player)
    ↓
AGNIDAWN.Bosses   (refs Core + Player + Enemies)
    ↓
AGNIDAWN.Gameplay (refs Core + Player + Enemies)
    ↓
AGNIDAWN.Audio    (refs Core + Player + Enemies + Bosses + Gameplay)
    ↓
AGNIDAWN.Visuals  (refs Core only — post-processing, camera shake, screen flash)
    ↓
AGNIDAWN.VFX      (refs Core only — particles, flames, aura, shockwave, mandala)
    ↓
AGNIDAWN.UI       (refs Core + Bosses — all UI panels, UIManager, HUD)
    ↓
AGNIDAWN.Tests.EditMode (refs all above, Editor-only)
AGNIDAWN.Editor   (Editor-only, no game refs — build pipeline scripts)
```
**Rule:** Core must NEVER reference Player/Enemy/Boss types directly. Use EventBus for upward communication.

### Key EventBus Events (string constants in GameManager.cs)
| Event | Type | Emitter | Purpose |
|-------|------|---------|---------|
| `EVT_GAME_START` | void | GameManager | Run begins |
| `EVT_GAME_OVER` | void | GameManager | Agni Kund dies |
| `EVT_VICTORY` | void | GameManager | Vritra defeated |
| `EVT_LEVEL_UP` | void | LevelUpManager | Player levelled up |
| `OnAgniKundDamaged` | float | AgniKund | Damage taken |
| `OnAgniTierChanged` | int, int | AgniKund | new tier, prev tier |
| `OnAgniAuraChanged` | float, float | AgniKund | speedBuff, damageBuff |
| `OnBoonApplyRequest` | BoonData, GameObject | BoonData | BoonApplier listens |
| `OnXPDropped` | float, Vector2 | BaseEnemy/BaseBoss | LevelUpManager listens |
| `OnEnemyDied` | string | BaseEnemy | SpawnManager listens |
| `OnBossSpawned` | string | BaseBoss | BossManager emits |
| `OnBossPhaseChanged` | string, int | BaseBoss | UI listens |
| `OnBossDied` | string | BaseBoss | BossManager listens |
| `OnMinutePassed` | int | GameManager | BossManager listens |
| `OnBossSpawnBegin` | void | BossManager | SpawnManager pauses |
| `OnBossSpawnEnd` | void | BossManager | SpawnManager resumes |
| `OnAstraFired` | AstraData | AstraController | UI / audio listens |
| `OnAstraEquipped` | AstraData | AstraController | UI listens |
| `OnAstraUpgraded` | AstraData, int | AstraController | UI listens |
| `OnAstraRemoved` | AstraData | AstraController | UI listens |
| `OnAstraSpecial` | string | Astra projectiles | VFX / audio listeners (Phase 6) |
| `OnAoEDetonation` | float, Vector2 | Brahmastra/Pashupatastra | VFX radius + shake |
| `OnSlowApplied` | GameObject, float, float | Varunastra | BaseEnemy slow (speed mult, duration) |
| `OnKnockbackApplied` | GameObject, Vector2, float | Vayuastra | BaseEnemy Rigidbody2D impulse |
| `OnLightningChain` | Vector2, Vector2 | Vajra | VFX line renderer |
| `OnBiomeEntered` | BiomeData | BiomeManager | SpawnManager + BaseEnemy cache modifiers |
| `OnBiomeExited` | BiomeData | BiomeManager | UI / audio transitions (Phase 13/14) |
| `OnShardCollected` | int | DivineShardManager | UI shard counter update |
| `OnShardSpent` | int | ShrineManager | UI shard counter update |
| `OnShrineUnlocked` | string shrineId | ShrineManager | UI shrine node activation |
| `OnLoreCollected` | string fragmentId | LoreManager | Purana book notification |
| `OnDifficultyChanged` | string difficultyId | DifficultyManager | UI difficulty selection |

### ObjectPool Key Convention
Format: `Prefix_Name` — e.g. `Bullet_Trishul`, `Enemy_Rakshasa`, `VFX_AgniHit`, `Boss_Ravana`

### GameManager State Machine
`MainMenu → Loading → Playing → Paused → LevelUp → GameOver / Victory`

---

## BOSS SCHEDULE (from BossData.spawnAtMinute)
| Minute | Boss | HP | Phases | Special |
|--------|------|----|--------|---------|
| 5 | Ravana | 2000 | 3 (thresholds: 0.66, 0.33) | 10 heads, rage per dead head |
| 10 | Mahishasura | 2800 | 3 (thresholds: 0.66, 0.33) | 3 form transformations |
| 15 | Kali | 3200 | 2 (threshold: 0.50) | Bloodlust stacks (cap 50), Mahakali form |
| 20 | Vritra | 4500 | 4 (thresholds: 0.75, 0.50, 0.25) | Weather control FSM, final boss |

---

## KNOWN ISSUES / TECH DEBT
- [ ] `BossSystemTests.cs` — namespace was `AGNIDAWN.Tests`, should be `AGNIDAWN.Tests.EditMode` (being fixed)
- [ ] `AGNIDAWN.Bosses.asmdef` was missing (being fixed)
- [x] `BaseBoss.OnEnable()` — wrong EventBus subscription removed; death detection via Update() polling — fixed Phase 16 ✅
- [x] `GameManager.cs` emits `OnMinutePassed` — wired in Phase 16 ✅
- [x] `QualityManager.cs` added — GPU tier auto-detect (Low/Medium/High) in Phase 16 ✅
- [ ] Phase 6 astra prefabs not created yet — each Astra script requires a Unity prefab with its component assigned in AstraData.projectilePrefab
- [x] **APK batch-mode UPM crash** — Root cause: `PROGRAMDATA` env var absent in non-interactive MCP sessions; Unity passes it to `UnityPackageManager.exe` (Node.js) which calls `path.join(process.env.PROGRAMDATA,"Unity","config")` on startup. Missing var → Node.js `TypeError` → UPM exit code 101 before IPC pipe created → Unity "Could not connect to IPC stream Upm-{PID} after 30s". Fix: `if (-not $env:PROGRAMDATA) { $env:PROGRAMDATA = "C:\ProgramData" }` added to `Tools/build_android.ps1` before Unity launch. ✅
- [x] `OnSlowApplied` / `OnKnockbackApplied` — consumers added to BaseEnemy (Phase 7) ✅
- [x] SudarshanaChakra `FindNearestEnemy()` — replaced with SpawnManager.ActiveEnemies registry (Phase 7) ✅

---

## PHASE 17 — ART DIRECTION (CONFIRMED — DO NOT DEVIATE)

**Style:** Painted/rendered sprite style — NOT pixel art, NOT procedural shapes.
**Reference:** Asura demon sprite sheet provided by Faizan (Midjourney-generated).
**Spec per character:**
- High-detail painted look with lava/fire cracks and glowing emissive effects
- Semi-isometric perspective (slight top-down angle)
- Transparent background per frame (PNG with alpha)
- Each frame ~100×120px
- Minimum animation states: **Idle** + **Walk** (more per character if relevant)

**Characters needing sprites:** Agni ✅ Asura ✅ Rakshasa ✅ Naga ✅ Pisacha ✅ Vetala ✅ — all 6 confirmed. Still needed: Agni Kund (5 tiers), Arena background tile, 10 Astra projectiles.

**Workflow:**
1. Faizan generates each sprite sheet via Midjourney and shares the image here or in #cowork-commands
2. Live session slices frames, imports as Texture2D, wires into CharacterSpriteFactory + GameBootstrap
3. DO NOT implement placeholder pixel art — wait for Midjourney assets

**⚠️ NO SESSION should begin Phase 17 sprite implementation until Faizan provides the source images.**

---

### ASURA — Enemy Type 1 (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Asura_reference.png`

**Visual design:**
- Bulky red/orange lava-cracked body with glowing fire veins and embers
- Large curved bull horns on head
- Carries a war axe
- **Palette:** deep red, orange-red lava cracks, bright orange glow, dark brown shadow
- Painted 2D, semi-isometric, transparent background per frame

**Sheet layout — 4 rows:**

| Row | Contents | Frames |
|-----|----------|--------|
| Row 1 | Idle + Walk | 4 idle + 6 walk |
| Row 2 | Idle variant + Walk variant | 4 + 4 |
| Row 3 | Idle + Walk | 4 + 4 |
| Row 4 | Idle + Boss variant walk | 4 + 4 |

**Animation states → game mapping:**

| Sheet Frames | Game Anim | Trigger |
|-------------|-----------|---------|
| Row 1 Idle (4fr) | `idle` | Default / in range, not moving |
| Row 1 Walk (6fr) | `chase` | Moving toward Agni Kund or player |
| Row 4 Boss frames | `boss_variant` | Used for Asura boss (Ravana's minions / Phase 3 elite variant) |

**Implementation notes:**
- Row 1 is the primary animation set — use for standard Asura enemy
- Row 4 boss variant reserved for elite/boss-tier Asura spawns (higher HP, larger scale ~1.5×)
- `BaseEnemy.cs` drives `idle` ↔ `chase` via `Animator.SetBool("isMoving", ...)`
- Boss variant triggered by a `isElite` flag on the enemy data

---

### RAKSHASA — Enemy Type 2 / Warrior (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Rakshasa_reference.png`

**Visual design:**
- Powerfully built dark blue-grey demonic warrior
- Wild mane/fur around head and shoulders, beast-like face with fangs
- Gold ornate armor on arms and chest, gold medallion center piece
- Carries a large curved golden war blade/scimitar
- **Palette:** dark blue-grey body, gold armor, orange fire accents, bright gold weapon
- Painted 2D, semi-isometric, transparent background per frame

**Sheet layout:**

| Position | Contents | Frames |
|----------|----------|--------|
| Top left | Standing (idle) | 4 |
| Top right | Guarded Creed (move) | 6 |
| Top right (inner) | Aggr Rush | 4 |
| Bottom left | Aggr Rush variant | 4 |
| Bottom right | Rakshasa Magic Effects (particles) | multiple sets |

**Rakshasa Magic Effects breakdown:**
- Blue orbs → projectile VFX (`VFX_RakshasaOrb`)
- Flame slashes → melee hit VFX (`VFX_RakshasaSlash`)
- Claw effects → heavy attack impact (`VFX_RakshasaClaw`)

**Animation states → game mapping:**

| Sheet Frames | Game Anim | Trigger |
|-------------|-----------|---------|
| Standing (4fr) | `idle` | Default / in range |
| Guarded Creed (6fr) | `move` | Pathing toward Agni Kund |
| Aggr Rush (4fr) | `attack` | Melee swing |
| Aggr Rush variant (4fr) | `attack_alt` | Alternate attack (blend randomly) |
| Magic Effects | `vfx` | Spawned at hit/death contact points |

**Implementation notes:**
- Rakshasa is the fast flanker archetype — high move speed, medium HP, hits hard
- Two attack variants (`attack` + `attack_alt`) blended randomly per swing to avoid repetition
- Blue orb particles → Rakshasa has a ranged throw at distance > 4f; uses `VFX_RakshasaOrb` from ObjectPool
- Gold scimitar weapon sprite extends beyond body bounds — set attack hitbox as a separate child `BoxCollider2D` offset forward
- Can be slowed (`OnSlowApplied` applies) and knocked back (`OnKnockbackApplied` applies) — no immunities

---

### VETALA — Enemy Type 5 / Undead Berserker (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Vetala_reference.png`

**Visual design:**
- Grey decaying undead corpse, hunched aggressive posture
- Exposed skull face, sunken eyes, hollow cheeks, bare decaying torso
- Long dragging arms with clawed hands — speeds up when damaged (already in BaseEnemy code)
- Tattered brown cloth around waist
- **Palette:** grey-green decay, pale bone, dark shadow, tattered brown cloth
- Painted 2D, semi-isometric, transparent background per frame

**Sheet layout:**

| Position | Contents | Frames |
|----------|----------|--------|
| Top left | Hunched Stance (idle) | 4 |
| Top right | Decaying Posture (move) | 4 |
| Left middle | Aggressive Rush (enraged) | 4 |
| Bottom left | Aggressive Rush variant | 4 |
| Bottom right | Vetala Magick Effect (particles) | — |

**Animation states → game mapping:**

| Sheet Frames | Game Anim | Trigger |
|-------------|-----------|---------|
| Hunched Stance (4fr) | `idle` | Default / HP > 50% and stationary |
| Decaying Posture (4fr) | `move` | Moving toward target, HP > 50% |
| Aggressive Rush (4fr) | `enraged` | HP drops below 50% — stays until death |
| Aggressive Rush variant (4fr) | `enraged_alt` | Alternate enraged cycle (blend randomly) |
| Vetala Magick Effect | `death` | OnDeath dissolve |

**Implementation notes:**
- `enraged` transition is permanent — once triggered at ≤50% HP it never reverts
- On `enraged` trigger: `moveSpeed *= 1.6f` + `SpriteRenderer` tint shifts to darker grey-green (Color mul ~0.8 green channel)
- Blend `enraged` and `enraged_alt` randomly (pick one per spawn) to avoid visual repetition in large hordes
- Vetala Magick Effect frames → `VFX_VetalaDeath` pool entry, dark swirling dissolve
- Long dragging arms mean attack hitbox is wider than the sprite bounds — set `CircleCollider2D` radius ~10% wider than visual
- Immune to slow effects (`OnSlowApplied` — skip Vetala; undead don't feel pain)

---

### PISACHA — Enemy Type 4 / Ghost (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Pisacha_reference.png`

**Visual design:**
- Wispy ethereal ghost — pale white-green glowing body, no solid lower body, trails off into tendrils
- Haunting face with hollow scream expression
- Floats and drifts — no walk cycle, flowing glide motion
- **Palette:** pale white, mint green, bright green glow, dark void hollow eyes
- Painted 2D, semi-isometric, transparent background per frame

**Sheet layout:**

| Position | Contents | Frames |
|----------|----------|--------|
| Top left | Standing | 4 |
| Top right | Guarded Creed (drift move) | 6 |
| Bottom left | Agha Rush (attack) | 4 |
| Bottom right | Pisacha Manifestation Effect (particles) | — |

**Animation states → game mapping:**

| Sheet Frames | Game Anim | Trigger |
|-------------|-----------|---------|
| Standing (4fr) | `idle` | Default / hovering in place |
| Guarded Creed (6fr) | `move` | Drifting toward Agni Kund |
| Agha Rush (4fr) | `attack` | Lunge/rush at target |
| Manifestation Effect | `spawn` + `death` | OnEnable (spawn) + OnDeath (dissolve) |

**Implementation notes:**
- Pisacha has no `Rigidbody2D` gravity — `gravityScale = 0`, moves via `transform.position` lerp for smooth float
- Apply a subtle sine-wave vertical offset in `Update()` (amplitude ~0.05f, speed ~2f) to sell the hover
- Manifestation Effect frames used for BOTH spawn (fade-in) and death (dissolve) — play forward on spawn, reverse on death
- `SpriteRenderer.color.a` lerp to 0.7f during `move` state — ghosts are semi-transparent when drifting
- Does NOT trigger `OnKnockbackApplied` — Pisacha ignores knockback (ethereal, passes through wind)

---

### NAGA — Enemy Type 3 / Tank (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Naga_reference.png`

**Visual design:**
- Humanoid upper body with green armored scales, cobra-like head with flared hood
- Large coiled green serpent lower body — no legs, glides across ground
- Carries shield + spear/sword — visually confirmed tank role
- Small serpent companion particles (bottom center of sheet) — use as death/hit VFX
- **Palette:** deep green, gold armor trim, lighter green scales, glowing eyes
- Painted 2D, semi-isometric, transparent background per frame

**Sheet layout:**

| Position | Contents | Frames |
|----------|----------|--------|
| Top left | Standing | 4 |
| Top right | Guarded Creed (move) | 6 |
| Right side | Serpent Guard Atk | 4 |
| Bottom left | Standing variant | 4 |
| Bottom right | Guarded Creed variant | 4 |
| Bottom center | Serpent companion particles | — |

**Animation states → game mapping:**

| Sheet Frames | Game Anim | Trigger |
|-------------|-----------|---------|
| Standing (top left, 4fr) | `idle` | Default / stationary |
| Guarded Creed (top right, 6fr) | `move` | Moving toward Agni Kund |
| Serpent Guard Atk (4fr) | `attack` | Attack swing |

**Implementation notes:**
- Naga is the tank archetype — higher HP, slower move speed than Asura
- Shield visual confirms a `damageReduction` stat in `EnemyData` (suggest 0.3f — 30% less damage from front)
- Serpent companion sprites → use as `VFX_NagaDeath` pool entry on `BaseEnemy` death
- Scale: ~1.3× standard enemy size to reinforce bulk
- Bottom variant rows available for an elite/armored Naga tier if needed

---

### AGNI — Player Character (✅ Reference confirmed)

**File:** `Assets/Sprites/Characters/Agni_reference.png`

**Visual design:**
- Fire/lava-cracked orange-red body with glowing flame accents
- Semi-serpentine lower body (dragon/naga hybrid elemental form)
- Fire mage aesthetic — elemental, not a traditional warrior
- Small fire particle effects around the body
- **Palette:** deep orange, red-brown, gold, bright orange flame highlights
- Painted 2D, semi-isometric, transparent background per frame

**Animation states in sheet → game mapping:**

| Sheet State | Game Anim | Trigger |
|-------------|-----------|---------|
| Standing | `idle` | Default / no input |
| Stealth Creep | `move` | Player moving |
| Fire Dagger Atk | `attack` | Astra fired |
| Agniform Mage | `special` | Ultimate / high Agni tier (tier 4–5) |

**Implementation notes:**
- Slice sheet into individual animation states using Unity Sprite Editor
- Create `AnimatorController` with the 4 states above
- `PlayerController.cs` drives transitions via `Animator.SetTrigger` / `SetBool`
- `special` state plays automatically when `AgniKund` tier ≥ 4 (listen to `OnAgniTierChanged` event)

---

## NEXT PHASES (from Linear)
| Ticket | Phase | What | Status |
|--------|-------|------|--------|
| FAI-11 | Phase 6  | Divine Weapon Astras — 10 weapons with unique projectile scripts (Trishul, Gandiv, Sudarshana Chakra, Brahmastra, Pashupatastra, Nagastra, Varunastra, Vayuastra, Agneyastra, Vajra) | ✅ Done |
| FAI-12 | Phase 7  | Biomes (Forest, Desert, Mountain, Ocean, Underworld) | ✅ Done |
| FAI-13 | Phase 8  | Meta-Progression & Shrine System (Divine Shards, 5 Shrines, Lore, Difficulty) | ✅ Done |
| FAI-14 | Phase 9  | FMOD Audio Integration | ✅ Done |
| FAI-15 | Phase 10 | URP Post-Processing (per-tier Agni visual states) | ✅ Done |
| FAI-16 | Phase 11 | VFX & Shader System | ✅ Done |
| FAI-14 | Phase 12 | UI/UX System — BaseUIPanel, UIManager, HUD, MainMenu, LevelUp, BossIntro, BossHP, Pause, Death, Victory + 14 tests | ✅ Done |
| FAI-17 | Phase 13 | QA, Testing & Build Pipeline — CI/CD rewrite (editmode+playmode+Android+auto-tag), PerformanceTests, BuildValidationTests, QASystemTests (20 tests), run_tests.ps1, build_android.ps1, AndroidBuilder.cs | ✅ Done |
| FAI-19 | Phase 14 | Mobile Testing — APK built (84.7 MB IL2CPP/ARM64 debug), installed on Samsung Z Fold7 (RZGYA0KX12E) via Unity ADB, launched, logcat verified: Vulkan+Adreno init, AAudio active, 1080×2520 SurfaceView, SetGameState mode:CONTENT — zero Unity errors | ✅ Done |
| —      | Phase 15 | Bootstrap & Playable Prototype — AGNIDAWN.Bootstrap assembly, GameBootstrap (no-prefab scene wiring: CameraFollow, AgniKundMini, VirtualJoystick, SimpleEnemySpawner, HUDUpdater), MainMenuBootstrap, SpriteFactory primitives. Playable on device with zero ScriptableObjects. | ✅ Done |
| FAI-20 | Phase 16 | Game Integration & Critical Fixes — GameManager emits OnMinutePassed (bosses now spawn), BaseBoss health listener fixed (polling vs wrong EventBus event), QualityManager (GPU tier auto-detect, 3 tiers, Z Fold7→High), GameBootstrap wires QualityManager, IntegrationTests (10 tests) | ✅ Done |
| FAI-17 | Phase 17 | Character Sprite System — CharacterAnimator.cs (code-driven frame animator, auto idle/walk via linearVelocity), CharacterSpriteFactory.cs (zero-prefab sprite loader from Resources/Characters/). All 6 characters extracted from Midjourney sheets: Agni (2+2fr), Asura (8+8fr), Naga (3+3fr), Pisacha (2+2fr), Vetala (3+3fr), Rakshasa (3+3fr). GameBootstrap updated: player uses Agni sprite, 5 enemy types cycle Asura/Rakshasa/Naga/Pisacha/Vetala. APK (145.7 MB) built + installed on Z Fold7. Root cause of batch-mode UPM crash diagnosed + fixed (PROGRAMDATA env var). | ✅ Done |

---

## CONVENTIONS ALL SESSIONS MUST FOLLOW
1. **Never commit to `main`** — always develop branch or feature branches
2. **Always run `Tools/check_errors.ps1`** after writing Unity scripts — check Editor.log before git push
3. **Every new namespace needs an `.asmdef` file** — follow the chain above
4. **Test files go in `Assets/Scripts/Tests/EditMode/`** with namespace `AGNIDAWN.Tests.EditMode`
5. **ScriptableObjects** use `[CreateAssetMenu(menuName = "AGNIDAWN/...")]`
6. **All inter-system communication** goes through EventBus — no direct cross-assembly references upward
7. **BossData.spawnAtMinute** must be 5, 10, 15, or 20 — GameManager emits `OnMinutePassed` on exact minute marks

### SESSION LOCK — must be followed by EVERY session (live and scheduled)

**Purpose:** Prevents two sessions from writing files at the same time.

**Lock file:** `C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game\.cowork-session.lock`

**Live session — ACQUIRE at start of any file/git work:**
```powershell
$ts = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
$lock = "{`"owner`":`"live-session`",`"since`":`"$ts`",`"doing`":`"DESCRIPTION`"}"
Set-Content "C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game\.cowork-session.lock" -Value $lock
```

**Live session — RELEASE when done or before ending the session:**
```powershell
Remove-Item "C:\Users\Micro\30 Minute\30 Minutes\agnidawn-game\.cowork-session.lock" -Force -ErrorAction SilentlyContinue
```

**Scheduled task behaviour when lock exists:**
- Lock owned by `live-session` AND since < 30 min → back off, retry next run
- Lock owned by `live-session` AND since > 30 min → stale, delete and proceed
- Lock owned by another scheduled task AND since < 10 min → back off
- Lock owned by another scheduled task AND since > 10 min → stale, delete and proceed

**Scheduled task — check BOTH guards before any file/git write:**
1. `mcp__session_info__list_sessions` — if a non-monitor Cowork session is running → back off
2. Lock file check above → if live-session lock is fresh → back off

---

## LAST UPDATED
- `2026-05-02` — Live Session: Phase 1-4, asmdef fixes, lock system, this handover doc
- `2026-05-02` — Scheduled Task: Phase 5 Boss System (commit `a3effd9`)
- `2026-05-02` — Live Session: Fixed Bosses asmdef + BossSystemTests namespace
- `2026-05-02` — Scheduled Task: Phase 6 — 10 divine weapon Astra projectile scripts + AstraSystemTests (11 files, AGNIDAWN.Gameplay.Astras namespace)
- `2026-05-02` — Scheduled Task: Phase 7 — Biomes (BiomeData SO, BiomeManager, SpawnManager registry, BaseEnemy slow/knockback, SudarshanaChakra fix, 14 tests) commit `b9c37c5`
- `2026-05-02` — Scheduled Task: Phase 8 — Meta-Progression & Shrine System (SaveSystem extended, DivineShardManager, ShrineData/Manager, LoreFragment/Manager, DifficultyData/Manager, 20 EditMode tests) commit `c1ddfcf`
- `2026-05-02` — Scheduled Task: Phase 9 — FMOD Audio Integration (AGNIDAWN.Audio assembly, AudioEventData SO, AudioManager FMOD+Unity fallback, MusicManager biome/boss adaptive music, SFXController EventBus→SFX, AudioBusController, 25 EditMode tests)
- `2026-05-03` — Scheduled Task: Phase 10 — URP Post-Processing (AgniTierVisualData SO, AgniVisualStateManager, CameraShakeController, ScreenFlashController, 18 EditMode tests) — pushed commit `43d4100`
- `2026-05-03` — Scheduled Task: Phase 11 — VFX & Shader System (AGNIDAWN.VFX assembly, VFXEventData SO, VFXManager, AgniFlameController, PlayerDivineAura, BossShockwaveController, AutoReturnToPool, MandalaFXController, 22 EditMode tests) — commit `764d117`
- `2026-05-03` — Live Session: Phase 13 — QA & Build Pipeline (CI rewrite, PerformanceTests, BuildValidationTests, QASystemTests 20 tests, run_tests.ps1, build_android.ps1, AndroidBuilder.cs). FAI-17 → Done — commits `45505e0` + `3f6af32`
- `2026-05-03` — Live Session: Phase 12 — UI/UX System (AGNIDAWN.UI assembly, BaseUIPanel, UIManager, HUDController, MainMenuUI, LevelUpUI, BossIntroOverlayUI, BossHealthBarUI, PauseMenuUI, DeathScreenUI, VictoryScreenUI, UISystemTests 14 tests). Fixed: UI asmdef Bosses ref, GameManager TotalKills/CurrentLevel/RestartRun/ReturnToMainMenu, bestRunTimeSeconds field name. FAI-14 → Done — commit `1307310`
- `2026-05-03` — Live Session: Phase 15 — Bootstrap & Playable Prototype (AGNIDAWN.Bootstrap assembly, GameBootstrap, MainMenuBootstrap, SpriteFactory, AgniKundMini, VirtualJoystick, SimpleEnemySpawner, CameraFollow, HUDUpdater) — commits `cc118fa` + `dd2b755`
- `2026-05-03` — Scheduled Task: Phase 16 — Game Integration & Critical Fixes (GameManager OnMinutePassed wired, BaseBoss death detection fixed, QualityManager new singleton, IntegrationTests 10 tests) — commits `555afe7` + `4cb4747`
- `2026-05-03` — Live Session: Phase 17 — Character Sprite System (CharacterAnimator.cs, CharacterSpriteFactory.cs, GameBootstrap updated for all 6 characters, 42 sprite frames extracted from Midjourney sheets into Resources/Characters/). Compile: 0 CS errors.
- `2026-05-04` — Live Session: Phase 17 APK — Diagnosed UPM IPC crash (PROGRAMDATA missing in batch-mode env block). Fixed Tools/build_android.ps1 with `if (-not $env:PROGRAMDATA)` guard. APK built (145.7 MB IL2CPP/ARM64) and installed on Samsung Z Fold7 (RZGYA0KX12E) via ADB. FAI-17 → ✅ Done.

---
*When you finish a coding session, add a row to "Last Updated" and update the "WHO BUILT WHAT" table.*
