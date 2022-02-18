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
	public delegate void ApplyArmorDelegate(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor);

	[HarmonyPatch(typeof(ArmorUtility))]
	[HarmonyPatch("GetPostArmorDamage")]
	//public static float GetPostArmorDamage(Pawn pawn, float amount, float armorPenetration, BodyPartRecord part, ref DamageDef damageDef, out bool deflectedByMetalArmor, out bool diminishedByMetalArmor)
	public static class GetPostArmorDamage_Patch
	{
		public static string lastShotlog = "";
		public static void LogMessage(string s)
		{
			Log.Message(s);
			lastShotlog += s + "\n";
		}

		public static void Prefix()
		{
			lastShotlog = "";
		}


		public static ApplyArmorDelegate ApplyArmorInfo = AccessTools.MethodDelegate<ApplyArmorDelegate>(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"));

		//private static void ApplyArmor(ref float damAmount, float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor)
		public static void ApplyArmorLayered(ref float damAmount, ref float armorPenetration, float armorRating, Thing armorThing, ref DamageDef damageDef, Pawn pawn, out bool metalArmor)
		{
			int layers = armorThing?.def.apparel.layers.Count ?? 1;
			LogMessage($"Doing ThickArmor for {pawn} wearing {armorThing?.LabelCap ?? "nothing"}");
			LogMessage($"{layers} layers		Damage		Penetration	Armor		Type		Metal");
			LogMessage($"START		{damAmount:0.0}		{armorPenetration:0.0%}		{armorRating :0.0%}		{damageDef}		False");

			do
			{
				//float previousAmount = damAmount;

				//Call it
				ApplyArmorInfo.Invoke(ref damAmount, armorPenetration, armorRating, armorThing, ref damageDef, pawn, out metalArmor);

				//if (previousAmount == damAmount) //only if damage not reduced
				if (Mod.settings.penetrationReduction &&
					armorRating > 2*armorPenetration)
					armorPenetration *= 2*armorPenetration / armorRating;
				//If armor rating is <= 2xpenetration, no penetration reduction
				//If armor rating is e.g. 4x penetration, reduce penetration 50%
				//If armor rating >> penetration, reduce penetration to near-0

				//Dampen armor rating
				armorRating *= Mod.settings.secondLayerEffectiveness;

				LogMessage($"After {layers}:		{damAmount:0.0}		{armorPenetration:0.0%}		{armorRating:0.0%}		{damageDef}		{metalArmor}");
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
				if (instruction.opcode == OpCodes.Ldarg_2)  //change armorPenetration to ref armorPenetration (big problem if later patches use armorPenentration elswhere)
					yield return new CodeInstruction(OpCodes.Ldarga_S, 2);
				else if (instruction.Calls(ApplyArmorInfo))
					yield return new CodeInstruction(OpCodes.Call, ApplyArmorLayeredInfo);
				else 
					yield return instruction;
			}
		}
	}
}
