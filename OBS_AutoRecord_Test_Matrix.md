# OBS Auto‑Record – Test Checklist (Printable)

> **Tip:** Temporarily set `PauseHoldTimeout` to **10–15s** for quick iteration, then restore.

## Setup
- [ ] Debug logging enabled (`isDebugMode = true`).
- [ ] Hold timeout set to 10–15s during testing.
- [ ] Adoption on resume in `onRecordResume()`:
  - [ ] `if (!ModInitiatedPause) ModInitiatedRecording = true;`
- [ ] `WhenInGym()` starts hold **only** when OBS is active, session is **mod‑owned**, and **not paused**.

---

## 1) Auto‑start → gym pause/hold → stop
- [ ] Enter **map** vs in‑list opponent → recording starts, owned by mod.
- [ ] Enter **gym** → `PauseRecord()` + hold queued.
- [ ] After timeout → `StopRecord()`; flags reset.

## 2) Same opponent during hold → resume
- [ ] In **gym** with hold, re‑enter **map** vs **same ID** before timeout.
- [ ] Expect `ResumeRecord()`; `QueuedForStopping=false`; no stop at old timeout.

## 3) Different opponent **NOT** in list during hold → stay paused
- [ ] In **gym** with hold, re‑enter **map** vs **different ID not in list**.
- [ ] Expect **no** OBS calls; remain paused; stop after hold timeout.

## 4) Different opponent **IN** list during hold → stop & start new
- [ ] In **gym** with hold, re‑enter **map** vs **different ID in list**.
- [ ] Expect `StopRecord()` → `StartRecord()` (new opponent); hold canceled.

## 5) External **resume** during hold → continue; **next gym** hold again
- [ ] In **gym** with hold, user **resumes** in OBS.
- [ ] Expect: hold canceled now; at **next gym**: `PauseRecord()` + new hold; stop at timeout.

## 6) **External start** → adopt after external **pause+resume**
- [ ] User **starts** in OBS manually; mod **does nothing**.
- [ ] User **pauses then resumes** in OBS → mod **adopts** (owned=true).
- [ ] At **next gym**: `PauseRecord()` + hold; stop at timeout.

## 7) Manual pause pre‑gym → **no hold** at that gym
- [ ] User **pauses** in OBS **before** gym.
- [ ] Enter **gym** while paused → **no hold**.
- [ ] If user **resumes before next gym**, then **that** gym should hold.

## 8) Duplicate gym events → single hold
- [ ] Cause `WhenInGym()` to run twice.
- [ ] Expect exactly **one** `PauseRecord()` and **one** active hold.

## 9) Short‑hold stress
- [ ] Set hold to **2–5s**.
- [ ] Exercise #2–#4 quickly; verify no stale holds after resume/stop.

## 10) External stop during hold
- [ ] In **gym** with hold, user **stops** in OBS.
- [ ] Expect flags reset; no double stop.

---

## Quick Smoke
- [ ] #1 Auto start → gym hold → stop
- [ ] #2 Same opponent → resume
- [ ] #3 Different not‑in‑list → stay paused → stop
- [ ] #4 Different in‑list → stop & start new
- [ ] #5 External resume → next gym hold
- [ ] #6 External start → adopt after pause+resume
- [ ] #7 Manual pause pre‑gym → no hold
