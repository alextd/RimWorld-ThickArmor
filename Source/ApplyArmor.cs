using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Harmony;


namespace ThickArmor
{

	[HarmonyPatch(typeof(ArmorUtility))]
	[HarmonyPatch("GetPostArmorDamage")]
	//public static int GetPostArmorDamage(Pawn pawn, int amountInt, BodyPartRecord part, DamageDef damageDef)
	class GetPostArmorDamage_Patch
	{
		public static void ApplyArmorLayered(ref float damAmount, float armorRating, Apparel armor, DamageDef damageDef)
		{
			Log.Message("Doing it, damAmount= " + damAmount + ", armorRating = " + armorRating + ", armor = " + armor);
			int layers = armor?.def.apparel.layers.Count ?? 1;

			MethodInfo ApplyArmorInfo = AccessTools.Method(
				typeof(ArmorUtility), "ApplyArmor");
			var args = new object[] { 0.0f, armorRating, armor, damageDef };

			while (layers-- > 0)
			{
				args[0] = damAmount;
				ApplyArmorInfo.Invoke(null, args);
				damAmount = (float)args[0];

				armorRating *= Settings.Get().secondLayerEffectiveness;
				args[1] = armorRating;
				args[2] = null;//ApplyArmor only damages it ; null for extra calls
				Log.Message("Now " + damAmount);
			}
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			MethodInfo ApplyArmorInfo = AccessTools.Method(
				typeof(ArmorUtility), "ApplyArmor");
			MethodInfo ApplyArmorLayeredInfo = AccessTools.Method(
				typeof(GetPostArmorDamage_Patch), nameof(ApplyArmorLayered));

			foreach (CodeInstruction instruction in instructions)
			{
				//IL_0068: call         void Verse.ArmorUtility::ApplyArmor(float32 &, float32, class Verse.Thing, class Verse.DamageDef)
				if (instruction.opcode == OpCodes.Call && instruction.operand == ApplyArmorInfo)
					instruction.operand = ApplyArmorLayeredInfo;
				yield return instruction;
			}
		}
	}
}
