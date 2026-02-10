# QuickerSong

A persistent Windows media helper that exposes **current track info** and **playback controls** over stdin/stdout. One process instance handles everything—no repeated spawning, no SMTC corruption.

**Credits:** Original [QuickSong]([https://github.com/iiDk-the-actual](https://github.com/iiDk-the-actual/QuickSong)) by II. QuickerSong is a rework that keeps a single long-running process and talks to it via pipes instead of invoking the executable on every poll.

---

## Why QuickerSong?

QuickSong (and any tool that repeatedly calls the Windows SMTC API in new processes) can break the system when used for frequent polling:

- **Media detection stops** (e.g. "No Media Playing" and never recovers).
- **Virtual media keys** (play/pause/next/prev) stop working system-wide.
- **Fix Required a PC restart**

Cause: each new process does `GlobalSystemMediaTransportControlsSessionManager.RequestAsync()` and then exits. Doing that every few seconds (or on every input) corrupts the Windows SMTC stack.

QuickerSong **initializes WinRT once** in a single process and stays running. Your app sends text commands over **stdin** and reads **JSON lines** from **stdout**. No repeated WinRT init, so no SMTC corruption.

---

## Using QuickerSong in Your Project

**Rule: use one process instance.** Start `QuickerSong.exe` once with **stdin** and **stdout** redirected (e.g. from your game/mod/UI). Keep that process running and send all commands over the same pipe. Do **not** start a new QuickerSong process per request—that defeats the purpose and can bring back the same SMTC issues.

## Commands (stdin)

| Command     | Output   | Description |
|------------|----------|-------------|
| `light`    | JSON line| Title + artist only (no thumbnail). Use for fast, frequent updates. |
| `info`     | JSON line| Title + artist + optional `ThumbnailBase64`. Use when you need the cover image. |
| `next`     | —        | Skip to next track. |
| `prev`     | —        | Skip to previous track. |
| `playpause`| —        | Toggle play/pause. |
| `exit`     | —        | Process exits. |

Commands are case-insensitive. One line = one command.

I got tired of messing around with QuickSong. I saw that almost every mod using it had the same issue (especially the QuickSong spam issue). Enjoy!.
