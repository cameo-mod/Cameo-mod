# Porting guide: flashing notifications

Make a transient notification line **pulse** for a few cycles when it appears, to draw the
eye to priority alerts (a unit under attack, construction complete, a superweapon ready).
Off by default, opt-in per call, and fully YAML-configurable.

This is a **porting guide, not a git patch** — it describes exactly what to add and where.
Follow it by hand, or hand it to a coding agent to apply against whatever engine version
your mod runs. (A `git am`-able `0001-flash-notifications.patch` is included alongside for
engines close to the base commit below.)

## What you get

- Any transient line can flash on arrival by passing `flash: true`.
- Effect modes (compose freely): text-colour pulse, bold-font swap, an underline rule, and a
  whole-line background-block pulse (the boldest).
- A player-facing on/off setting; zero cost when nothing is flashing.

## How it works

Notifications carry a new `Flash` flag. When the display widget adds a flashing line it wraps
the line's colour/font/background getters in a square-wave that reads real time, and tracks
the line in a small list that `Tick` drives and prunes. No simulation state changes — it's
render-only chrome.

## Compatibility

- Written against OpenRA **`bleed` @ `b0b0544d4a`**. **Build-verified on pristine bleed** (0 errors).
- **4 files, ~146 lines**, all additive — passing `flash` defaults to `false`, so nothing
  flashes until a caller opts in.
- No new files, no framework, no threading.

## Package contents

| File | Role |
|---|---|
| this guide | The 4 file edits + how to trigger and configure a flash. |
| `0001-flash-notifications.patch` | Optional `git am`/`git apply` form for engines near the base commit. |

---

## Step 1 — Add the `Flash` flag to the notification record

`OpenRA.Game/TextNotification.cs` — add a `Flash` field and thread it through the constructor:

```csharp
public readonly bool Flash;

// add `bool flash = false` as the last constructor parameter:
public TextNotification(TextNotificationPool pool, int clientId, string prefix, string text,
    Color? prefixColor, Color? textColor, bool flash = false)
{
    // ... existing assignments ...
    Flash = flash;
}
```

## Step 2 — Thread `flash` through the manager

`OpenRA.Game/TextNotificationsManager.cs` — add an optional `flash` arg to the transient and
core add methods (defaults keep every existing caller unchanged):

```csharp
public static void AddTransientLine(Player player, string text, bool flash = false)
{
    // ... existing guard ...
    AddTextNotification(TextNotificationPool.Transients, SystemClientId, SystemMessageLabel,
        FluentProvider.GetMessage(text), flash: flash);
}

static void AddTextNotification(TextNotificationPool pool, int clientId, string prefix, string text,
    Color? prefixColor = null, Color? textColor = null, bool flash = false)
{
    // ... build the notification passing flash through:
    var textNotification = new TextNotification(pool, clientId, prefix, text, prefixColor, textColor, flash);
    // ... existing cache/dispatch ...
}
```

## Step 3 — Add the on/off setting

`OpenRA.Game/Settings.cs`, inside the **`GameSettings`** class:

```csharp
[Desc("Flash priority game-event notification lines (e.g. base/unit under attack, superweapons) to draw attention.")]
public bool FlashTransientNotifications = true;
```

The display widget gates on `Game.Settings.Game.FlashTransientNotifications`, so this is the
player-facing master switch. (Optionally bind a Display-settings checkbox to it.)

## Step 4 — Render the flash

All in `OpenRA.Mods.Common/Widgets/TextNotificationsDisplayWidget.cs`.

**4a. Configurable fields** — add near the other `public readonly` widget fields:

```csharp
[Desc("Colour the text is drawn in during the \"on\" phase of a flashing notification.")]
public readonly Color FlashColor = Color.White;

[Desc("Font swapped in during the \"on\" phase (e.g. a bold sibling). Empty = keep the normal font.")]
public readonly string FlashFont = "Bold";

[Desc("Draw a rule under the text during the \"on\" phase.")]
public readonly bool FlashUnderline = false;

[Desc("Duration in milliseconds of one flash cycle (one \"on\" phase plus one \"off\" phase).")]
public readonly int FlashIntervalMs = 500;

[Desc("Duration in milliseconds of the \"on\" phase within each cycle.")]
public readonly int FlashOnMs = 250;

[Desc("How many times a flashing notification flashes before settling.")]
public readonly int FlashCount = 3;

[Desc("Pulse the whole line's background block bright during the \"on\" phase (whole-line flash).")]
public readonly bool FlashHighlight = false;

[Desc("Id of the ColorBlock child in the line template whose colour is pulsed when FlashHighlight is set.")]
public readonly string FlashHighlightBlock = "BACKGROUND";

[Desc("Colour the background block is pulsed to during the \"on\" phase when FlashHighlight is set.")]
public readonly Color FlashBackgroundColor = Color.White;

[Desc("Text colour during the \"on\" phase when FlashHighlight is set (overrides FlashColor in highlight mode).")]
public readonly Color FlashTextColor = Color.Black;
```

**4b. State + timing helpers** — add near the other private fields (e.g. after the
`expirations` list):

```csharp
sealed class FlashLine
{
    public readonly LabelWidget Label;
    public readonly string BaseFont;
    public readonly long Start;

    public FlashLine(LabelWidget label, string baseFont, long start)
    {
        Label = label;
        BaseFont = baseFont;
        Start = start;
    }
}

readonly List<FlashLine> flashing = [];

// Total flash lifetime; after this the line settles back to its normal appearance.
long FlashDurationMs => (long)FlashIntervalMs * FlashCount;

bool FlashActive(long start) => Game.RunTime - start < FlashDurationMs;

// Square wave: "on" for the first FlashOnMs of every FlashIntervalMs cycle.
bool FlashOn(long start)
{
    var elapsed = Game.RunTime - start;
    return elapsed < FlashDurationMs && elapsed % FlashIntervalMs < FlashOnMs;
}
```

**4c. Wrap the getters when a flashing line is added** — in `AddNotification`, right after the
line widget is cloned + set up:

```csharp
if (notification.Flash && Game.Settings.Game.FlashTransientNotifications)
{
    var textLabel = notificationWidget.GetOrNull<LabelWidget>("TEXT");
    if (textLabel != null)
    {
        var start = Game.RunTime;
        var baseColor = textLabel.GetColor;
        var onColor = FlashHighlight ? FlashTextColor : FlashColor;
        textLabel.GetColor = () => FlashOn(start) ? onColor : baseColor();
        flashing.Add(new FlashLine(textLabel, textLabel.Font, start));

        // Whole-line flash: pulse the line's background block. It draws before the text, so the
        // bright fill lands behind the (contrast-recoloured) text with no draw-order change.
        if (FlashHighlight && !string.IsNullOrEmpty(FlashHighlightBlock))
        {
            var block = notificationWidget.GetOrNull<ColorBlockWidget>(FlashHighlightBlock);
            if (block != null)
            {
                var baseBlockColor = block.GetColor;
                block.GetColor = () => FlashOn(start) ? FlashBackgroundColor : baseBlockColor();
            }
        }
    }
}
```

**4d. Drive the font swap + prune** — at the top of `Tick()`:

```csharp
// Drive the bold font swap and prune finished flashes. Runs regardless of DisplayDurationMs.
for (var i = flashing.Count - 1; i >= 0; i--)
{
    var f = flashing[i];
    if (!FlashActive(f.Start))
    {
        if (!string.IsNullOrEmpty(FlashFont))
            f.Label.Font = f.BaseFont;

        flashing.RemoveAt(i);
        continue;
    }

    if (!string.IsNullOrEmpty(FlashFont))
        f.Label.Font = FlashOn(f.Start) ? FlashFont : f.BaseFont;
}
```

**4e. (Optional) underline** — only if you want `FlashUnderline`. In `DrawOuter`, after the
existing draw, and add the helper:

```csharp
// end of DrawOuter():
if (FlashUnderline)
    DrawFlashUnderlines();

void DrawFlashUnderlines()
{
    foreach (var f in flashing)
    {
        if (!FlashOn(f.Start))
            continue;

        if (!Game.Renderer.Fonts.TryGetValue(f.Label.Font, out var font))
            continue;

        var text = f.Label.GetText();
        if (string.IsNullOrEmpty(text))
            continue;

        var size = font.Measure(text);
        var origin = f.Label.RenderOrigin;

        // TEXT uses VAlign Middle; place the rule just below the glyphs.
        var y = origin.Y + (f.Label.Bounds.Height + size.Y) / 2 + 1;
        WidgetUtils.FillRectWithColor(new Rectangle(origin.X, y, size.X, 1), FlashColor);
    }
}
```

## Step 5 — Make a notification flash (C#)

From any trait/logic, pass `flash: true`:

```csharp
TextNotificationsManager.AddTransientLine(player, "notification-unit-under-attack", flash: true);
```

Existing non-flashing calls need no change.

## Step 6 — Style it (YAML)

On your `TextNotificationsDisplayWidget` (in the in-game chrome), set any of:

| Field | Default | Effect |
|---|---|---|
| `FlashHighlight` | `false` | Pulse the **whole line's background block** (boldest). |
| `FlashHighlightBlock` | `BACKGROUND` | Id of the `ColorBlock` child to pulse. |
| `FlashBackgroundColor` | white | Block colour during the pulse. |
| `FlashTextColor` | black | Text colour during the pulse (highlight mode). |
| `FlashColor` | white | Text colour during the pulse (non-highlight mode). |
| `FlashFont` | `Bold` | Font swapped in during the pulse; empty = keep normal font. |
| `FlashUnderline` | `false` | Rule under the text during the pulse. |
| `FlashIntervalMs` | `500` | One full cycle (on + off). |
| `FlashOnMs` | `250` | Length of the "on" phase. |
| `FlashCount` | `3` | How many times it flashes before settling. |

## Step 7 — Build & verify

```sh
dotnet build OpenRA.Mods.Common/OpenRA.Mods.Common.csproj -c Release
```

0 errors (verified on bleed). In-game: raise a priority alert with `flash: true` and confirm
the line pulses, then settles; toggle `FlashTransientNotifications` off and confirm it stops.

## Notes

- `flash` defaults `false` everywhere → no behaviour change until a caller opts in.
- The getters read `FlashOn`, so lines self-settle to their base look when the flash ends —
  no per-frame teardown needed beyond the `Tick` prune.
