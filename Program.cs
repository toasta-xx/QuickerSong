// Creds to II for QuickSong (original).
// QuickSong was redone because calling it every few seconds (or on every input) to fetch media info broke Windows SMTC:
// detection stopped, virtual media key inputs (play/pause/next/prev) stopped working system-wide, and a full PC restart was required to fix it.
// Using one persistent QuickerSong process and talking to it over stdin/stdout avoids repeated WinRT init and keeps a single session.
// Using QuickerSong in your own project: create or instantiate one process instance and keep it running; send "light", "info", "next", "prev", "playpause" over stdin and read JSON lines from stdout. One instance manages everything. see README.md for more info.

using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;

class Program
{
    static GlobalSystemMediaTransportControlsSessionManager manager;
    static GlobalSystemMediaTransportControlsSession cachedSession;

    static string Esc(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

    static void WriteInfo(string title, string artist, string thumb = null)
    {
        var t = thumb != null ? $",\"ThumbnailBase64\":\"{thumb}\"" : "";
        Console.WriteLine($"{{\"Title\":\"{Esc(title)}\",\"Artist\":\"{Esc(artist)}\"{t}}}");
        Console.Out.Flush();
    }

    static GlobalSystemMediaTransportControlsSession GetSession()
    {
        var s = manager.GetCurrentSession();
        if (s != null) { cachedSession = s; return s; }
        var all = manager.GetSessions();
        if (all.Count > 0) { cachedSession = all[0]; return all[0]; }
        return cachedSession;
    }

    static async Task Main()
    {
        manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        string line;
        while ((line = Console.ReadLine()) != null)
        {
            var cmd = line.Trim().ToLower();
            if (cmd == "exit") break;
            try
            {
                var session = GetSession();
                switch (cmd)
                {
                    case "light":
                        if (session == null) { WriteInfo("No Media Playing", ""); break; }
                        var lp = await session.TryGetMediaPropertiesAsync();
                        WriteInfo(string.IsNullOrEmpty(lp.Title) ? "No Media Playing" : lp.Title, lp.Artist ?? "", null);
                        break;
                    case "info":
                        if (session == null) { WriteInfo("No Media Playing", ""); break; }
                        var p = await session.TryGetMediaPropertiesAsync();
                        string thumb = null;
                        try
                        {
                            if (p.Thumbnail != null)
                            {
                                using var stream = await p.Thumbnail.OpenReadAsync();
                                using var ms = new MemoryStream();
                                await stream.AsStreamForRead().CopyToAsync(ms);
                                thumb = Convert.ToBase64String(ms.ToArray());
                            }
                        }
                        catch { }
                        WriteInfo(string.IsNullOrEmpty(p.Title) ? "No Media Playing" : p.Title, p.Artist ?? "", thumb);
                        break;
                    case "next": if (session != null) await session.TrySkipNextAsync(); break;
                    case "prev": if (session != null) await session.TrySkipPreviousAsync(); break;
                    case "playpause": if (session != null) await session.TryTogglePlayPauseAsync(); break;
                }
            }
            catch
            {
                if (cmd == "info" || cmd == "light") WriteInfo("No Media Playing", "");
                try { manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync(); } catch { }
            }
        }
    }
}
