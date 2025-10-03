# ✅ OBS Auto-Record Mod Test Checklist

### **Setup**
- [ ] OBS connected and responding to API.
- [ ] AutoRecordList populated with at least one known PlayFab ID.
- [ ] Game mod loaded and state machine active.

---

## **1. Gym → Pause → Hold → Stop**
- [ ] Match with someone in auto-record list.
- [ ] Return to **gym**.
- [ ] Verify:
  - [ ] Recording pauses once (no duplicate pause calls).
  - [ ] `QueuedForStopping = true`.
- [ ] Wait **RecordingPauseHoldTimeout** (e.g., 3 min).
- [ ] Verify:
  - [ ] Recording stops automatically.
  - [ ] OBS state = stopped.
  - [ ] Log shows stop reason as mod-initiated.

---

## **2. Gym → Pause → Same Opponent Rematch**
- [ ] Match with someone in auto-record list
- [ ] Return to **gym** (pause + hold starts).
- [ ] Re-enter match with **same opponent** within hold time.
- [ ] Verify:
  - [ ] Recording resumes.
  - [ ] `QueuedForStopping` cleared.
  - [ ] No stop occurs after timeout.

---

## **3. Gym → Pause → Different Opponent (AutoRecord)**
- [ ] Match with someone in auto-record list
- [ ] Return to **gym** (pause + hold starts).
- [ ] Enter match with **different opponent** who is in AutoRecordList.
- [ ] Verify:
  - [ ] Previous recording stops.
  - [ ] New recording starts for new opponent.
  - [ ] Logs show correct opponent info.

---

## **4. Gym → Pause → External Resume**
- [ ] Start a mod-initiated recording.
- [ ] Return to **gym** (pause + hold starts).
- [ ] Manually resume recording in OBS (external).
- [ ] Verify:
  - [ ] Recording resumes.
  - [ ] ~~After timeout, recording **still stops** (per your policy).~~
  -   This is an error. After time out, recording shouldn't stop. But continue but another stop is queued after the next return to the gym
  - [ ] Logs indicate mod-initiated stop.

---

## **5. External Recording in Map**
- [ ] Start recording manually in OBS (not via mod).
- [ ] Enter a match with an AutoRecordList opponent.
- [ ] Verify:
  - [ ] Mod does **not** pause, stop, or restart recording.
  - [ ] Logs show no mod interference.

---

## **6. Paused Externally in Map**
- [ ] Start recording manually in OBS.
- [ ] Pause manually in OBS.
- [ ] Enter a match.
- [ ] Verify:
  - [ ] Mod does **not** resume or stop recording.
  - [ ] Logs confirm no mod action.

---

## **8. Logging & State Reset**
- [ ] After any stop (manual or mod):
  - [ ] `IsRecording = false`.
  - [ ] `IsPaused = false`.
  - [ ] `QueuedForStopping = false`.
  - [ ] `ModInitiatedRecording = false`.
  - [ ] `ModInitiatedPause = false`.
  - [ ] `StopRequestedByMod = false`.
