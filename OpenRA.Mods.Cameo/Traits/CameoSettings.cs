#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

namespace OpenRA.Mods.Cameo.Traits
{
	// Shared Cameo settings module. Read at runtime via world.GetSettings<CameoSettings>() /
	// modData.GetSettings<CameoSettings>(). Add Cameo-wide user toggles here rather than to the
	// engine's GameSettings, so the engine stays untouched.
	[SettingsModule.YamlNode("Cameo", shared: true)]
	public class CameoSettings : SettingsModule
	{
		[Desc("Enables Quota Mode: production buildings auto-requeue units to maintain alive count targets.")]
		public bool QuotaModeEnabled = false;

		[Desc("Single-player only: gracefully slow the game when the simulation can't keep up (instead of",
			"render-frame-drop / teleport-stutter). No effect in multiplayer or replays.")]
		public bool AdaptiveGameSpeedEnabled = false;
	}
}
