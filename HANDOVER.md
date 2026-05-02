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
| Fix | AGNIDAWN.Bosses.asmdef + BossSystemTests namespace fix | Live Session | pending | 🔄 In Progress |

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

---

## NEXT PHASES (from Linear)
| Ticket | Phase | What | Status |
|--------|-------|------|--------|
| FAI-11 | Phase 6 | Divine Weapon Astras — 10 weapons with unique projectile scripts (Trishul, Gandiv, Sudarshana Chakra, Brahmastra, Pashupatastra, Nagastra, Varunastra, Vayuastra, Agneyastra, Vajra) | 🔜 Next |
| FAI-12 | Phase 7 | Biomes (Forest, Desert, Mountain, Ocean, Underworld) | Pending |
| FAI-13 | Phase 8 | UI System (HUD, Level-Up Screen, Boss Health Bar, Pause Menu) | Pending |
| FAI-14 | Phase 9 | FMOD Audio Integration | Pending |
| FAI-15 | Phase 10 | URP Post-Processing (per-tier Agni visual states) | Pending |

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

---
*When you finish a coding session, add a row to "Last Updated" and update the "WHO BUILT WHAT" table.*
