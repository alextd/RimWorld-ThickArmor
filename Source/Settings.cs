using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThickArmor
{
	class Settings : ModSettings
	{
		public float secondLayerEffectiveness = 0.6f;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<ThickArmor.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("TD.ArmorExplanation".Translate());
			options.Label("TD.NextLayerEffectiveness".Translate() + String.Format("{0:0}%", secondLayerEffectiveness * 100));
			secondLayerEffectiveness = options.Slider(secondLayerEffectiveness, 0.0f, 1.0f);

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref secondLayerEffectiveness, "secondLayerEffectiveness", 0.6f);
		}
	}
}