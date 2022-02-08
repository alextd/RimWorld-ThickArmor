using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyLib;


namespace ThickArmor
{
	[HarmonyPatch(typeof(ArmorUtility))]
	[HarmonyPatch("GetPostArmorDamage")]
	//public static float GetPostArmorDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part, ref DamageDef damageDef, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor)
	class GetPostArmorDamage_Patch
	{
		static MethodInfo ApplyArmorInfo = AccessTools.Method(typeof(ArmorUtility), "ApplyArmor");
		//private static void ApplyArmor(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor)
		public static void ApplyArmorLayered(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor)
		{
			Log.Message($"Doing it, damAmount= {damAmount}, armorPenetration = {armorPenetration}, armorRating = {armorRating}, armorThing = {armorThing}" +
				$", damageDef = {damageDef}, pawn = {pawn}");
			int layers = armorThing?.def.apparel.layers.Count ?? 1;

			var args = new object[] { damAmount, armorPenetration, armorRating, armorThing, damageDef, pawn, false };

			do
			{
				//Call it
				ApplyArmorInfo.Invoke(null, args);
				metalArmor = (bool)args[6];
				damageDef = (DamageDef)args[4];
				damAmount = (float)args[0];
				Log.Message($"layer {layers}, damAmount= {damAmount}, damageDef = {damageDef}, pawn = {pawn}, metalArmor = {metalArmor}");

				//Dampen effects
				armorRating *= Mod.settings.secondLayerEffectiveness;

				//Apply new value
				args[2] = armorRating;
			}
			while (--layers > 0 && damAmount > 0);
		}
		
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ApplyArmorInfo = AccessTools.Method(typeof(ArmorUtility), "ApplyArmor");
			MethodInfo ApplyArmorLayeredInfo = AccessTools.Method(
				typeof(GetPostArmorDamage_Patch), nameof(ApplyArmorLayered));

			foreach (CodeInstruction instruction in instructions)
			{
				//IL_0068: call         void Verse.ArmorUtility::ApplyArmor(float32 &, float32, class Verse.Thing, class Verse.DamageDef)
				if (instruction.Calls(ApplyArmorInfo))
					yield return new CodeInstruction(OpCodes.Call, ApplyArmorLayeredInfo);
				else 
					yield return instruction;
			}
		}
	}
}
