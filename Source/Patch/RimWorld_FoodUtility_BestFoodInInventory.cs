﻿using System.Linq;
using Harmony;
using PawnRules.Data;
using RimWorld;
using Verse;

namespace PawnRules.Patch
{
    [HarmonyPatch(typeof(FoodUtility), "BestFoodInInventory")]
    internal static class RimWorld_FoodUtility_BestFoodInInventory
    {
        private static bool Prefix(ref Thing __result, Pawn holder, Pawn eater = null, FoodPreferability minFoodPref = FoodPreferability.NeverForNutrition, FoodPreferability maxFoodPref = FoodPreferability.MealLavish, float minStackNutrition = 0.0f, bool allowDrug = false)
        {
            if (!Registry.IsActive) { return true; }
            if (holder.inventory == null)
            {
                __result = null;
                return false;
            }

            if (eater == null) { eater = holder; }

            var rules = Registry.GetRules(eater);
            if (eater.InMentalState || (rules == null) || rules.GetRestriction(RestrictionType.Food).IsVoid) { return true; }

            var innerContainer = holder.inventory.innerContainer;
            foreach (var thing in innerContainer.ToArray())
            {
                // Pawn Rules - Food check below
                if (!thing.def.IsNutritionGivingIngestible || !thing.IngestibleNow || !eater.RaceProps.CanEverEat(thing) || (thing.def.ingestible.preferability < minFoodPref) || (thing.def.ingestible.preferability > maxFoodPref) || (!allowDrug && thing.def.IsDrug) || !(thing.GetStatValue(StatDefOf.Nutrition) * thing.stackCount >= (double) minStackNutrition) || !rules.GetRestriction(RestrictionType.Food).AllowsFood(thing.def, eater)) { continue; }

                __result = thing;
                return false;
            }

            __result = null;
            return false;
        }
    }
}
