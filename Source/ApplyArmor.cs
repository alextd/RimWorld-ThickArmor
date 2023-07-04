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


		public static ApplyArmorDelegate ApplyArmor = AccessTools.MethodDelegate<ApplyArmorDelegate>(AccessTools.Method(typeof(ArmorUtility), "ApplyArmor"));

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
				ApplyArmor(ref damAmount, armorPenetration, armorRating, armorThing, ref damageDef, pawn, out metalArmor);

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

		// TODO: Dynamically find what the inst to load armorPenetration for ApplyArmor is and change it to address version
		// For now I'm just gonna manually figure that out, and it happens to be the same in the both places this is patched.
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
			TranspilerManualReplace(instructions, (CodeInstruction inst) => inst.opcode == OpCodes.Ldarg_2, new CodeInstruction(OpCodes.Ldarga_S, 2));

		public static IEnumerable<CodeInstruction> TranspilerManualReplace(IEnumerable<CodeInstruction> instructions, Predicate<CodeInstruction> loadPenOldInst, CodeInstruction loadPenAddrInst)
		{
			MethodInfo ApplyArmorInfo = AccessTools.Method(typeof(ArmorUtility), "ApplyArmor");
			MethodInfo ApplyArmorLayeredInfo = AccessTools.Method(
				typeof(GetPostArmorDamage_Patch), nameof(ApplyArmorLayered));

			var instList = instructions.ToList();
			for(int i = 0; i < instList.Count; i++)
			{
				var instruction = instList[i];
				if (instruction.Calls(ApplyArmorInfo))
				{
					instruction.operand = ApplyArmorLayeredInfo;

					// change arg for loading armor penetration to the address type
					for( int prevI = i; prevI > 0; prevI--)
					{
						if (loadPenOldInst(instList[prevI]))
						{
							instList[prevI] = loadPenAddrInst;
							break;
						}
					}
				}
			}
			return instList;
		}

		static GetPostArmorDamage_Patch()
		{
			// For for Athena Framework
			if(AccessTools.Method("AthenaCombatUtility:ApplyArmor") is MethodInfo patchMethod)
			{
				Harmony harmony = new Harmony("uuugggg.rimworld.ThickArmor.AthenaSupport");

				// Luckily it's the same transpiler with same ldarg_2
				harmony.Patch(patchMethod, transpiler: new HarmonyMethod(typeof(GetPostArmorDamage_Patch), nameof(Transpiler)));
			}

		}
	}
}
