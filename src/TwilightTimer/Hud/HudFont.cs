using UnityEngine;

namespace TwilightTimer
{
    /// <summary>
    /// Shared dynamic-OS-font helper for the IMGUI HUD components
    /// (<see cref="TimerHud"/>, <see cref="LeaderboardHud"/>). A dynamic OS
    /// font with a broad fallback list renders Latin + CJK; the applied size
    /// is tracked so the font is only (re)created when the configured size
    /// actually changes.
    /// </summary>
    internal static class HudFont
    {
        /// <summary>
        /// (Re)create the dynamic OS font when the configured size changes.
        /// On failure the previous font (or null) is kept and a warning is
        /// logged — the GUIStyle falls back to the engine font.
        /// </summary>
        /// <param name="font">Cached font field; replaced when the size changes.</param>
        /// <param name="appliedSize">Cached applied-size field; kept in sync with <paramref name="font"/>.</param>
        /// <param name="size">The requested size (clamped to a minimum of 18 when &lt;= 0).</param>
        /// <returns>The usable font, or null when OS font creation fails.</returns>
        internal static Font EnsureDynamic(ref Font font, ref int appliedSize, int size)
        {
            if (size <= 0) size = 18;
            if (font != null && appliedSize == size) return font;
            try
            {
                // A dynamic OS font with a broad fallback list renders Latin + CJK.
                font = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "PingFang SC", "Microsoft YaHei", "Noto Sans CJK SC",
                    "Noto Sans CJK", "Heiti SC", "Arial Unicode MS", "Arial",
                }, size);
                appliedSize = size;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"TwilightTimer: dynamic font creation failed: {ex.Message}");
                font = null;
            }
            return font;
        }
    }
}
