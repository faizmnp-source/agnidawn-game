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
- [ ] `BaseBoss.OnEnable()` subscribes to `OnPlayerDamagedWithSource` but should listen to its own HealthSystem's damage event — low priority
- [ ] `GameManager.cs` does not emit `OnMinutePassed` yet — BossManager needs this wired in GameManager.Update()
- [ ] No `QualityManager.cs` yet (FAI auto-detect GPU tier)
- [ ] Phase 6 astra prefabs not created yet — each Astra script requires a Unity prefab with its component assigned in AstraData.projectilePrefab
- [x] `OnSlowApplied` / `OnKnockbackApplied` — consumers added to BaseEnemy (Phase 7) ✅
- [x] SudarshanaChakra `FindNearestEnemy()` — replaced with SpawnManager.ActiveEnemies registry (Phase 7) ✅

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
| FAI-17 | Phase 13 | QA, Testing & Build Pipeline — Unity PlayMode tests, GitHub Actions, Appium for menu UI, ADB for Z Fold7 | ⏳ Next |
| FAI-19 | Phase 14 | Mobile Testing — Samsung Z Fold7 device testing, ADB APK push, Appium UI flows | ⏳ Upcoming |

---

## CONVENTIONS ALL SESSIONS MUST FOLLOW
1. **Never commit to `main`** — always develop branch or feature branches
2. **Always run `Tools/check_errors.ps1`** after writing Unity scripts — check Editor.log before git push
3. **Every new namespace needs an `.asmdef` file** — follow the chain above
4. **Test files go in `Assets/Scripts/Tests/EditMode/`** with namespace `AGNIDAWN.Tests.EditMode`
5. **ScriptableObjects** use `[CreateAssetMenu(menuName = "AGNIDAWN/...")]`
6. **All inter-system communication** goes through EventBus — no direct cross-assembly references upward
7. **BossData.spawnAtMinute** must be 5, 10, 15, or 20 — GameManager emits `OnMinutePassed` on exact minute marks

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
- `2026-05-03` — Live Session: Phase 12 — UI/UX System (AGNIDAWN.UI assembly, BaseUIPanel, UIManager, HUDController, MainMenuUI, LevelUpUI, BossIntroOverlayUI, BossHealthBarUI, PauseMenuUI, DeathScreenUI, VictoryScreenUI, UISystemTests 14 tests). Fixed: UI asmdef Bosses ref, GameManager TotalKills/CurrentLevel/RestartRun/ReturnToMainMenu, bestRunTimeSeconds field name. FAI-14 → Done — commit `1307310`

---
*When you finish a coding session, add a row to "Last Updated" and update the "WHO BUILT WHAT" table.*
