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

		[Desc("Selected cyberintel UI colour theme. On change, the matching baked chrome sheets are ",
			"copied over the active ones (applies after restart). Options live in uibits/cyberintel-themes/. ",
			"\"random\" re-picks a colour on every boot.")]
		public string UITheme = "random";
	}
}
