# ObsAutoRecorder

Automatically controls OBS to record match sessions based on your preferences. Also renames clips and writes chapter markers coinciding with said clips to videos.

## Quick Start: 

### Install [Kalamart's OBS control API](https://thunderstore.io/c/rumble/p/Kalamart/OBS_Control_API/) and just go to your friends list and start picking people to auto record by clicking their name in fighter options
![bippit](https://i.imgur.com/fyFDi5Z.png)
#### If not go set it up even if you're not gonna use this mod it's honestly not that hard and it's very useful

## Features

- Auto-record matches based on a BP threshold or a custom list.
- Automatically rename recordings to include opponent names.
- Display OBS recording status on the health bar. (Can be hidden from rock cam and legacy cam)
- Replay buffer support with optional chapter markers.
- Keeps recording active for a short time after a match, allowing seamless continuation if you rematch them. (Recording Hold Timout in config)
- If the recording was started manually through OBS, the mod won't interfere and remains inactive.
- If you pause and then resume the recording within 0.5 seconds, the mod is reactivated and resumes control of the recording.
- When the mod is inactive due to an externally started recording, the OBS logo will blink (minimal icon appears red).

---

## How to Use


### Skippable Details (might answer some questions. idk. you can just dm me tbh)

1. **Selecting Opponents for Auto Recording**   
    - Go to your **Friends** or **Recently Met** list.
    - Click their name, 
    - Go to the **Selected Fighter** section and click their name there to toggle 
2. **Hold Recording Behavior**
    - After leaving a recorded match, the recording is in a held state indicated by the pause or record icon blinking
    - If you match with the same player again before it times out, the recording continues
    - Matching with a different opponent stops the current recording and starts a new recording for that opponent
3. **Manual Recording Behavior**:
    - If you start recording manually through OBS, the mod will remain inactive and not interfere. Indicated by the OBS icon blinking in the game.
    - To make recording automatic, just do a quick pause and resume on OBS
4. **Replay Buffer Usage**:
    - Enable replay buffer in OBS to use clip-saving features.
    - When a clip is saved, the replay buffer icon will blink for 7 seconds.
    - The clip is renamed with the same format as full recordings but dated to when the replay was taken with an additonal prefix as configured in the config file
5. **OBS Icon Indicators**:
    - When the mod is inactive due to external recording, the OBS logo will blink and the minimal icon will appear red.
    - When active, the recording status is displayed on the health bar.
6. **Managing AutoRecord List**:
    - You can manually edit `AutoRecordList.txt` in your **userdata** folder for people not in your friends list: `{playfabID} - {public name}`
7. **Config file in UserData/ObsAutoRecorder** 
    - Generated after first launching the game. 

---

## Configuration Options

### Auto Rename Settings
| Option | Default | Description |
|---|---|---|
| Enable Auto Rename | true | Automatically rename recorded files. |
| Auto Rename String | `{date}` `{time}` vs `{player}` | Format for renaming. Use `{player}`, `{date}`, `{map}`, and `{time}` as placeholders. The date and time is according to when you enter the match with the player. Supports folder creation e.g. `{player}/{date} {time}` |
| Clip Auto Rename String | R-`{date}` `{time}` vs `{player}` | Rename format for saved replay buffer files |
| Date Format | yyyy-MM-dd | Date format for renaming. [Format Reference](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings) |
| Time Format | HH-mm-ss | Time format for renaming. |
### Recording Settings
| Option | Default | Description |
|---|---|---|
| Recording Hold Timeout | 0 | Seconds to keep recording paused before auto-stop. |
| Pause After Match | false | Pause recording on returning to gym (Replay buffer doesn't work while recording is paused). |
| BP Threshold | -1 | Record players with BP greater than this value. -1 disables threshold. |
| Chapter Markers | true | Add chapter markers to video when clipping (Hybrid MP4 only). |
### Indicator Settings
| Option | Default | Description |
|---|---|---|
| Prefer Minimal Icon | false | Use minimal OBS icon for recording indicator. (a little broken but usable. No motivation to fix it)|
| Clip Icon Default Visibility | true | Show replay buffer icon always; otherwise, only shown blinking when a clip is saved. |
| Show Icons on Camera | true | Display icons on Rock Cam and Legacy Cam. |
| OBS Icon Position | 0 | Position of OBS Icon along healthbar. Left to right from 0 to 100 |
| Replay OBS Offset | 5 | Offset of Replay Buffer Icon from main OBS Icon | 
### ObsAutoRecorder
| Option | Default | Description |
|---|---|---|
| Debug Mode | false | Enable verbose logging for troubleshooting. (Also enables an in-game debug monitor)|

---

## Notes

- Fight sessions are determined by entry into a map, and then exits when you go back to your gym. 
- Uses Kalamart's OBS Control API to connect to OBS. Make sure it is [properly configured and connected](https://github.com/Kalamart1/OBS_Control_API?tab=readme-ov-file#setup)
- ModUI support is not planned (until it adds melon preferences support)
- Chapter marker support is only available for Hybrid MP4 output. Check your OBS settings 