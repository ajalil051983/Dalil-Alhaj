namespace KhayratAlhaj;

/// <summary>
/// Single source of truth for all theme colors used in C# code-behind.
/// Mirrors the values in Resources/Styles/Colors.xaml — change a color
/// here and it updates every page that references it.
/// </summary>
public static class ThemeColors
{
    // ── Page / scaffold backgrounds ──────────────────────────
    public static readonly Color PageBackgroundLight = Color.FromArgb("#F5F5F5");
    public static readonly Color PageBackgroundDark  = Color.FromArgb("#2C3E50");

    // ── Card / surface backgrounds ───────────────────────────
    public static readonly Color CardBackgroundLight = Colors.White;
    public static readonly Color CardBackgroundDark  = Color.FromArgb("#2C3E50");

    // ── Header bar ───────────────────────────────────────────
    public static readonly Color HeaderBackground = Color.FromArgb("#2C3E50");

    // ── Surface / muted backgrounds (countdown, etc.) ────────
    public static readonly Color SurfaceBackgroundLight = Color.FromArgb("#ECF0F1");
    public static readonly Color SurfaceBackgroundDark  = Color.FromArgb("#2C3E50");

    // ── Primary text ─────────────────────────────────────────
    public static readonly Color PrimaryTextLight = Color.FromArgb("#2C3E50");
    public static readonly Color PrimaryTextDark  = Color.FromArgb("#F5F5F5");

    // ── Secondary text ───────────────────────────────────────
    public static readonly Color SecondaryTextLight = Color.FromArgb("#7F8C8D");
    public static readonly Color SecondaryTextDark  = Color.FromArgb("#AEAEB2");

    // ── Label text (settings labels, etc.) ───────────────────
    public static readonly Color LabelTextLight = Colors.Black;
    public static readonly Color LabelTextDark  = Colors.White;

    // ── Subtle / muted text (about section, version) ─────────
    public static readonly Color SubtleTextLight = Color.FromArgb("#666666");
    public static readonly Color SubtleTextDark  = Color.FromArgb("#CCCCCC");

    // ── Placeholder text ─────────────────────────────────────
    public static readonly Color PlaceholderTextLight = Color.FromArgb("#95A5A6");
    public static readonly Color PlaceholderTextDark  = Color.FromArgb("#98989D");

    // ── Input backgrounds (slightly lighter for contrast) ────
    public static readonly Color InputBackgroundLight = Colors.White;
    public static readonly Color InputBackgroundDark  = Color.FromArgb("#34495E");

    // ── Content body text (slightly different from primary) ──
    public static readonly Color ContentTextLight = Color.FromArgb("#34495E");
    public static readonly Color ContentTextDark  = Color.FromArgb("#F5F5F5");

    // ── Convenience helpers ──────────────────────────────────
    public static Color PageBackground(bool isDark)    => isDark ? PageBackgroundDark    : PageBackgroundLight;
    public static Color CardBackground(bool isDark)    => isDark ? CardBackgroundDark    : CardBackgroundLight;
    public static Color SurfaceBackground(bool isDark) => isDark ? SurfaceBackgroundDark : SurfaceBackgroundLight;
    public static Color PrimaryText(bool isDark)       => isDark ? PrimaryTextDark       : PrimaryTextLight;
    public static Color SecondaryText(bool isDark)     => isDark ? SecondaryTextDark     : SecondaryTextLight;
    public static Color LabelText(bool isDark)         => isDark ? LabelTextDark         : LabelTextLight;
    public static Color SubtleText(bool isDark)        => isDark ? SubtleTextDark        : SubtleTextLight;
    public static Color PlaceholderText(bool isDark)   => isDark ? PlaceholderTextDark   : PlaceholderTextLight;
    public static Color InputBackground(bool isDark)    => isDark ? InputBackgroundDark   : InputBackgroundLight;
    public static Color ContentText(bool isDark)       => isDark ? ContentTextDark       : ContentTextLight;
}
