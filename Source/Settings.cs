using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ThickArmor
{
	public class Settings : ModSettings
	{
		public float secondLayerEffectiveness = 0.5f;
		public bool penetrationReduction = true;

		public void DoWindowContents(Rect wrect)
		{
			var options = new Listing_Standard();
			options.Begin(wrect);

			options.Label("TD.ArmorExplanation".Translate());
			Text.Font = GameFont.Medium;
			options.Label("TD.NextLayerEffectiveness".Translate() + String.Format("{0:0}%", secondLayerEffectiveness * 100));
			Text.Font = GameFont.Small;
			secondLayerEffectiveness = options.Slider(secondLayerEffectiveness, 0.0f, 1.0f);
			options.Label("TD.PenetrationExplanation".Translate());
			Text.Font = GameFont.Medium;
			options.CheckboxLabeled("TD.PenetrationReduction".Translate(), ref penetrationReduction);
			Text.Font = GameFont.Small;

			if (GetPostArmorDamage_Patch.lastShotlog != "")
			{
				options.Gap(50f);
				options.GapLine();
				Text.Font = GameFont.Medium;
				options.Label("Effects of Thick Armor on Last Shot:");
				Text.Font = GameFont.Small;
				options.Label(GetPostArmorDamage_Patch.lastShotlog);
			}

			options.End();
		}
		
		public override void ExposeData()
		{
			Scribe_Values.Look(ref secondLayerEffectiveness, "secondLayerEffectiveness", 0.5f);
			Scribe_Values.Look(ref penetrationReduction, "penetrationReduction", true);
		}
	}
}