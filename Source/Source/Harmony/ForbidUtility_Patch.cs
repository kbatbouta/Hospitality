using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Hospitality.Harmony
{
    internal static class ForbidUtility_Patch
    {
        /// <summary>
        /// So guests will care
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.CaresAboutForbidden))]
        public class CaresAboutForbidden
        {
            private static MethodInfo firstMethod = AccessTools.Method(typeof(Thing), "get_Map");
            private static MethodInfo secondMethod = AccessTools.Method(typeof(Map), "get_IsPlayerHome");

            /*
            IL_001D: ldarg.0
            IL_001E: callvirt  instance class Verse.Map Verse.Thing::get_Map()
            IL_0023: callvirt  instance bool Verse.Map::get_IsPlayerHome()
            IL_0028: brtrue.s  IL_0059 
            */
            
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
            {
                var list = source.ToList();
                int idx = 0;

                for (int i = 0; i < list.Count - 4; i++)
                {
                    if (list[i]
                        .opcode == OpCodes.Ldarg_0 && list[i + 1]
                        .opcode == OpCodes.Callvirt && list[i + 2]
                        .opcode == OpCodes.Callvirt && list[i + 3]
                        .opcode == OpCodes.Brtrue_S)
                    {
                        if (list[i + 1]
                            .operand as MethodInfo == firstMethod && list[i + 2]
                            .operand as MethodInfo == secondMethod)
                        {
                            idx = i;
                            break;
                        }
                    }
                }

                list.RemoveRange(idx, 4);

                return list;
            }
            
            //[HarmonyPrefix]
            //public static bool Replacement(ref bool __result, Pawn pawn, bool cellTarget)
            //{
            //    // I have split up the original check to make some sense of it. Still doesn't make any sense.
            //    __result = CrazyRimWorldCheck(pawn) && !pawn.InMentalState && (!cellTarget || !ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn));
            //    return false;
            //}

            //private static bool CrazyRimWorldCheck(Pawn pawn)
            //{
            //    // Guests need this in PlayerHome
            //    return (pawn.HostFaction == null || pawn.HostFaction == Faction.OfPlayer && pawn.Spawned /*&& !pawn.Map.IsPlayerHome*/ && NotInPrison(pawn) && NotFleeingPrisoner(pawn));
            //}

            //private static bool NotFleeingPrisoner(Pawn pawn)
            //{
            //    return !pawn.IsPrisoner || pawn.guest.PrisonerIsSecure;
            //}

            //private static bool NotInPrison(Pawn pawn)
            //{
            //    return pawn.GetRoom() == null || !pawn.GetRoom().isPrisonCell;
            //}
        }

        /// <summary>
        /// Set by JobDriver_Patch and stores who is doing a toil right now, in which case we don't want to forbid things.
        /// </summary>
        public static Pawn currentToilWorker;

        /// <summary>
        /// Things dropped by guests are never forbidden
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "SetForbidden")]
        public class SetForbidden
        {
            [HarmonyPrefix]
            public static bool Prefix(bool value)
            {
                if (value && currentToilWorker.IsArrivedGuest())
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Area check for guests trying to access things outside their zone.
        /// </summary>
        [HarmonyPatch(typeof(ForbidUtility), "InAllowedArea")]
        public class InAllowedArea
        {
            [HarmonyPostfix]
            public static void Postfix(IntVec3 c, Pawn forPawn, ref bool __result)
            {
                if (!__result || forPawn == null) return; // Not ok anyway, moving on
                if (forPawn.mapIndexOrState < 0) return;
                if (GuestUtility.CachedMapComponents[forPawn.mapIndexOrState].presentGuests.Count == 0) return;
                if (!forPawn.IsArrivedGuest()) return;

                var area = forPawn.GetGuestArea();
                if (area == null) return;
                if (!c.IsValid || !area[c]) __result = false;
            }
        }
    }
}
