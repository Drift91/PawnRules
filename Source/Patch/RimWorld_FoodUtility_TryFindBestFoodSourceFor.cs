﻿using Harmony;
using PawnRules.Data;
using RimWorld;
using Verse;

namespace PawnRules.Patch
{
    [HarmonyPatch(typeof(FoodUtility), "TryFindBestFoodSourceFor")]
    internal static class RimWorld_FoodUtility_TryFindBestFoodSourceFor
    {
        private static bool Prefix(ref bool __result, Pawn getter, Pawn eater, bool desperate, out Thing foodSource, out ThingDef foodDef, bool canRefillDispenser = true, bool canUseInventory = true, bool allowForbidden = false, bool allowCorpse = true, bool allowSociallyImproper = false, bool allowHarvest = false, bool forceScanWholeMap = false)
        {
            foodSource = null;
            foodDef = null;

            if (!Registry.IsActive) { return true; }

            var rules = Registry.GetRules(eater);
            if (eater.InMentalState || (rules == null) || rules.GetRestriction(RestrictionType.Food).IsVoid) { return true; }

            var hasInventory = getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
            var allowDrug = !eater.IsTeetotaler();
            var thing1 = (Thing) null;

            if (canUseInventory)
            {
                // Pawn Rules - Added eater to BestFoodInInventory check
                if (hasInventory) { thing1 = FoodUtility.BestFoodInInventory(getter, eater, FoodPreferability.MealAwful); }

                if (thing1 != null)
                {
                    if (getter.Faction != Faction.OfPlayer)
                    {
                        foodSource = thing1;
                        foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                        __result = true;
                        return false;
                    }

                    var comp = thing1.TryGetComp<CompRottable>();
                    if ((comp != null) && (comp.Stage == RotStage.Fresh) && (comp.TicksUntilRotAtCurrentTemp < 30000))
                    {
                        foodSource = thing1;
                        foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                        __result = true;
                        return false;
                    }
                }
            }
            var getter1 = getter;
            var eater1 = eater;
            var desperate1 = desperate;
            var allowPlant = getter == eater;
            var allowDispenserEmpty = canRefillDispenser;
            var foodSource1 = FoodUtility.BestFoodSourceOnMap(getter1, eater1, desperate1, out var foodDef1, FoodPreferability.MealLavish, allowPlant, allowDrug, allowCorpse, true, allowDispenserEmpty, allowForbidden, allowSociallyImproper, allowHarvest, forceScanWholeMap);

            if ((thing1 != null) || (foodSource1 != null))
            {
                if ((thing1 == null) && (foodSource1 != null))
                {
                    foodSource = foodSource1;
                    foodDef = foodDef1;

                    __result = true;
                    return false;
                }

                var finalIngestibleDef = FoodUtility.GetFinalIngestibleDef(thing1);
                if (foodSource1 == null)
                {
                    foodSource = thing1;
                    foodDef = finalIngestibleDef;

                    __result = true;
                    return false;
                }

                if (FoodUtility.FoodOptimality(eater, foodSource1, foodDef1, (getter.Position - foodSource1.Position).LengthManhattan) > (double) (FoodUtility.FoodOptimality(eater, thing1, finalIngestibleDef, 0f) - 32f))
                {
                    foodSource = foodSource1;
                    foodDef = foodDef1;

                    __result = true;
                    return false;
                }
                foodSource = thing1;
                foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                __result = true;
                return false;
            }

            if (canUseInventory && hasInventory)
            {
                // Pawn Rules - Added eater to BestFoodInInventory check
                var thing2 = FoodUtility.BestFoodInInventory(getter, eater, FoodPreferability.DesperateOnly, FoodPreferability.MealLavish, 0f, allowDrug);
                if (thing2 != null)
                {
                    foodSource = thing2;
                    foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                    __result = true;
                    return false;
                }
            }

            if ((foodSource1 == null) && (getter == eater) && (getter.RaceProps.predator || (getter.IsWildMan() && !getter.IsPrisoner)))
            {
                var huntForPredator = Access.Method_RimWorld_FoodUtility_BestPawnToHuntForPredator_Call(getter, forceScanWholeMap);
                if (huntForPredator != null)
                {
                    foodSource = huntForPredator;
                    foodDef = FoodUtility.GetFinalIngestibleDef(foodSource);

                    __result = true;
                    return false;
                }
            }

            foodSource = null;
            foodDef = null;

            __result = false;
            return false;
        }
    }
}
