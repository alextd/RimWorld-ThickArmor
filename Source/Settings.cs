using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThickArmor
{
	class Settings : ModSettings
	{
		public float secondLayerEffectiveness = 0.5f;

		public static Settings Get()
		{
			return LoadedModManager.GetMod<ThickArmor.Mod>().GetSettings<Settings>();
		}

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("Armor is applied for each layer of apparel, adding to the next. Power Armor takes up two layers, but its armor is only applied once. An armor vest and jacket could outperform power armor. This simply applies the armor again, for its second layer, with a % reduction in power.");
			options.Label("Effectiveness that the next layer of armor has: " +  String.Format("{0:0}%", secondLayerEffectiveness * 100));
			secondLayerEffectiveness = options.Slider(secondLayerEffectiveness, 0.0f, 1.0f);

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref secondLayerEffectiveness, "secondLayerEffectiveness", 0.5f);
		}
	}
}