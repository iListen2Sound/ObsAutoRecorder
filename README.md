# ObsAutoRecorder

Automatically controls OBS to record match sessions based on your preferences.

## Features

- Auto-record matches based on a BP threshold or a custom list.
- Automatically rename recordings to include opponent names.
- Remembers the last recorded opponent for a short time after a match, allowing seamless continuation if you rematch them.
- Display OBS recording status on the health bar.
- Replay buffer support with optional chapter markers.
- Interactions are well integrated into the environment so no need to leave VR

---

## How to Add Opponents to Auto Record

- Go to your **Friends** or **Recently Met** list.
- Click their name, 
- Go to the **Selected Fighter** section and click their name there to toggle recording.
- Alternatively, edit `AutoRecordList.txt` in your **userdata** folder:

```CSharp
{playfabID} - {public name}
```

You can get this info from your `MatchInfo` userdata if you have that mod.

---

## Configuration Options

| Option | Default | Description |
|---|---|---|
| Enable Auto Rename | true | Automatically rename recorded files. |
| Auto Rename String | {date} {time} vs {player} | Format for renaming. Use {player}, {date}, {time} as placeholders. |
| Date Format | yyyy-MM-dd | Date format for renaming. [Format reference](https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings) |
| Time Format | HH-mm-ss | Time format for renaming. |
| Replay Prefix | R- | Prefix for replay buffer files. |
| Chapter Markers | false | Add chapter markers to video (Hybrid MP4 only). |
| Recording Hold Timeout | 180 | Seconds to keep recording paused before auto-stop. |
| Pause After Match | false | Pause recording when not fighting a recordable player (disables replay buffer). |
| BP Threshold | -1 | Record players with BP greater than this value. -1 disables threshold. |
| Prefer Minimal Icon | false | Use minimal OBS icon for recording indicator. |
| Clip Icon Default Visibility | true | Show replay buffer icon always; otherwise only when inactive or blinking. |
| Show Icons on Camera | true | Display icons on Rock Cam and Legacy Cam. |
| Debug Mode | true | Enable verbose logging for troubleshooting. |

---

### Example Rename Template

Default template:

```CSharp
{date} {time} vs {player}
```

Example output:

```CSharp
2025-10-17 21-30-00 vs Howard
```

You can customize this string using placeholders:

- `{player}` → Opponent name
- `{date}` → Date using your configured format
- `{time}` → Time using your configured format

---

## Notes

- End of sessions are determined by entry into a map, and then exits when you go back to your gym. 
- Uses Kalamart's OBS Control API to connect to OBS. Make sure it is [properly configured and connected](https://github.com/Kalamart1/OBS_Control_API?tab=readme-ov-file#setup)
- ModUI support is not planned.