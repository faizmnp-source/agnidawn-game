## Summary
<!-- One-line description of what this PR does -->

## Linear Ticket
<!-- e.g. Closes FAI-XX -->

---

## QA Checklist

### Code Quality
- [ ] No compile errors (`Tools/check_errors.ps1` returns 0 errors)
- [ ] No new CS warnings introduced
- [ ] Every new namespace has a matching `.asmdef` file
- [ ] No direct cross-assembly references that break the dependency chain (Core → Player → Enemies → Bosses → Gameplay → Audio → Visuals → VFX → UI)
- [ ] All new EventBus events documented in HANDOVER.md event table

### Testing
- [ ] EditMode tests written for new systems (≥1 test per public method with logic)
- [ ] All existing tests still pass locally
- [ ] No tests skipped or marked `[Ignore]` without a comment explaining why

### Gameplay Feel
- [ ] Tested in Unity Editor Play Mode (not just EditMode)
- [ ] No null reference exceptions in Console during a full run
- [ ] Frame rate stays above 60fps in Editor Play Mode (check Profiler)

### Memory & Performance
- [ ] No obvious memory leaks (e.g. event listeners unsubscribed in OnHide/OnDestroy)
- [ ] Object pooling used for any frequently-spawned GameObject (projectiles, VFX, enemies)
- [ ] No `FindObjectOfType` / `GameObject.Find` calls in Update loops

### Save / Load
- [ ] If new data is persisted: `SaveData` struct updated and `SaveSystem` migration tested
- [ ] Save file loads correctly after the change (test with an existing save JSON)

### Audio
- [ ] New gameplay actions have corresponding `AudioEventData` entries or EventBus hooks
- [ ] No audio clips left as direct `AudioSource.PlayClipAtPoint` calls (use AudioManager)

### Mobile / Android (if applicable)
- [ ] Tested on Z Fold7 emulator or device via ADB
- [ ] No UI elements clipped on foldable screen (inner display 2176×1812)
- [ ] APK size delta noted in PR description

### HANDOVER.md
- [ ] "WHO BUILT WHAT" table updated
- [ ] "LAST UPDATED" entry added
- [ ] Any new EventBus events added to the event table

---

## Screenshots / Video
<!-- Attach a GIF or screenshot if there are visual changes -->

## Notes for Reviewer
<!-- Anything tricky, known issues, or follow-up tickets -->
