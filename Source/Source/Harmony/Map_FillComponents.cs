using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace Hospitality.Harmony
{
    [HarmonyPatch(typeof(Map), nameof(Map.FillComponents))]
    class Map_FillComponents
    {
        public static void Postfix(Map __instance)
        {
            if(GuestUtility.CachedMapComponents == null) GuestUtility.CachedMapComponents = new Hospitality_MapComponent[6];

            int mapIndex = Find.Maps.Count;

            if (GuestUtility.CachedMapComponents.Length < Find.Maps.Count)
            {
                Array.Resize(ref GuestUtility.CachedMapComponents, Find.Maps.Count + 6); // This does Array.Copy for us.
            }

            GuestUtility.CachedMapComponents[mapIndex] = __instance.GetMapComponent();
        }
    }
}
