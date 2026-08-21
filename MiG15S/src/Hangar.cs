using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace MiG15S
{
    /// <summary>
    /// Blueprinter hangar ids (revetment1__revetment1) do not match 0.34 instance
    /// names, so the definition shows up greyed-out. Put MiG-15S on every live hangar.
    /// </summary>
    internal static class HangarInject
    {
        private static readonly FieldInfo AvailableAircraftField =
            AccessTools.Field(typeof(Hangar), "availableAircraft");
        private static readonly FieldInfo SelectionField =
            AccessTools.Field(typeof(AircraftSelectionMenu), "aircraftSelection");
        private static readonly FieldInfo MenuAirbaseField =
            AccessTools.Field(typeof(AircraftSelectionMenu), "airbase");
        private static readonly FieldInfo ButtonLabelField =
            AccessTools.Field(typeof(AircraftSelectionButton), "label");

        internal static void ApplyShortIcon(AircraftSelectionButton btn)
        {
            if (btn == null)
                return;
            AircraftDefinition def = null;
            try { def = btn.definition; }
            catch { def = null; }
            if (!Service.IsOursDef(def))
                return;
            if (ButtonLabelField == null)
                return;
            Text label = ButtonLabelField.GetValue(btn) as Text;
            if (label != null)
                label.text = Service.ShortName;
        }

        internal static AircraftDefinition FindDef()
        {
            if (Encyclopedia.Lookup != null)
            {
                UnitDefinition u;
                if (Encyclopedia.Lookup.TryGetValue(Service.JsonKey, out u))
                {
                    AircraftDefinition hit = u as AircraftDefinition;
                    if (hit != null)
                        return hit;
                }
                if (Encyclopedia.Lookup.TryGetValue(Service.DonorJsonKey, out u))
                {
                    AircraftDefinition hit = u as AircraftDefinition;
                    if (hit != null)
                        return hit;
                }
            }
            AircraftDefinition[] all = null;
            try { all = Resources.FindObjectsOfTypeAll<AircraftDefinition>(); }
            catch { all = null; }
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                if (Service.IsOursDef(all[i]))
                    return all[i];
            }
            return null;
        }

        internal static void RegisterNetwork(AircraftDefinition def)
        {
            if (def == null)
                return;
            try
            {
                if (!string.IsNullOrEmpty(def.jsonKey) && Encyclopedia.Lookup != null)
                    Encyclopedia.Lookup[def.jsonKey] = def;
            }
            catch { }
            Encyclopedia enc = null;
            try { enc = Encyclopedia.i; }
            catch { enc = null; }
            if (enc == null || enc.IndexLookup == null)
                return;
            INetworkDefinition nd = def;
            try
            {
                int? existing = nd.LookupIndex;
                if (existing.HasValue
                    && existing.Value >= 0
                    && existing.Value < enc.IndexLookup.Count
                    && object.ReferenceEquals(enc.IndexLookup[existing.Value], nd))
                    return;
                int idx = enc.IndexLookup.IndexOf(nd);
                if (idx >= 0)
                {
                    nd.LookupIndex = idx;
                    return;
                }
                enc.IndexLookup.Add(nd);
                nd.LookupIndex = enc.IndexLookup.Count - 1;
            }
            catch { }
        }

        internal static void EnsureOnHangar(Hangar hangar)
        {
            if (hangar == null || AvailableAircraftField == null)
                return;
            if (IsRubble(hangar.name))
                return;
            try
            {
                if (hangar.Disabled)
                    return;
            }
            catch { }
            AircraftDefinition def = FindDef();
            if (def == null)
                return;
            RegisterNetwork(def);
            Service.ApplyEncyclopedia(def);
            AircraftDefinition[] cur = AvailableAircraftField.GetValue(hangar) as AircraftDefinition[];
            if (ArrayContains(cur, def))
                return;
            int n = cur != null ? cur.Length : 0;
            AircraftDefinition[] next = new AircraftDefinition[n + 1];
            if (n > 0)
                Array.Copy(cur, next, n);
            next[n] = def;
            AvailableAircraftField.SetValue(hangar, next);
        }

        private static float _nextScan;

        internal static void Tick()
        {
            if (Time.unscaledTime < _nextScan)
                return;
            _nextScan = Time.unscaledTime + 1f;
            EnsureAllHangars();
        }

        internal static void EnsureAllHangars()
        {
            Hangar[] all = null;
            try { all = Resources.FindObjectsOfTypeAll<Hangar>(); }
            catch { all = null; }
            if (all == null)
                return;
            for (int i = 0; i < all.Length; i++)
                EnsureOnHangar(all[i]);
        }

        internal static void MergeInto(List<AircraftDefinition> dest, Airbase airbase)
        {
            if (dest == null || airbase == null)
                return;
            List<Hangar> hangars = null;
            try { hangars = airbase.hangars; }
            catch { hangars = null; }
            if (hangars != null)
            {
                for (int i = 0; i < hangars.Count; i++)
                    EnsureOnHangar(hangars[i]);
            }
            AircraftDefinition def = FindDef();
            if (def == null)
                return;
            if (ListContains(dest, def))
                return;
            dest.Add(def);
        }

        internal static void InjectMenu(AircraftSelectionMenu menu, Airbase airbase)
        {
            if (menu == null || SelectionField == null)
                return;
            List<AircraftDefinition> sel = SelectionField.GetValue(menu) as List<AircraftDefinition>;
            if (sel == null)
            {
                sel = new List<AircraftDefinition>(8);
                SelectionField.SetValue(menu, sel);
            }
            if (airbase != null)
                MergeInto(sel, airbase);
            else
            {
                AircraftDefinition def = FindDef();
                if (def != null && !ListContains(sel, def))
                    sel.Add(def);
            }
        }

        internal static bool HangarHasOurs(Hangar hangar)
        {
            if (hangar == null || AvailableAircraftField == null)
                return false;
            AircraftDefinition def = FindDef();
            if (def == null)
                return false;
            AircraftDefinition[] arr = AvailableAircraftField.GetValue(hangar) as AircraftDefinition[];
            return ArrayContains(arr, def);
        }

        internal static bool ListContains(List<AircraftDefinition> list, AircraftDefinition def)
        {
            if (list == null || def == null)
                return false;
            string key = def.jsonKey;
            for (int i = 0; i < list.Count; i++)
            {
                AircraftDefinition cur = list[i];
                if (cur == null)
                    continue;
                if (object.ReferenceEquals(cur, def))
                    return true;
                if (!string.IsNullOrEmpty(key) && string.Equals(cur.jsonKey, key, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool ArrayContains(AircraftDefinition[] arr, AircraftDefinition def)
        {
            if (arr == null || def == null)
                return false;
            string key = def.jsonKey;
            for (int i = 0; i < arr.Length; i++)
            {
                AircraftDefinition cur = arr[i];
                if (cur == null)
                    continue;
                if (object.ReferenceEquals(cur, def))
                    return true;
                if (!string.IsNullOrEmpty(key) && string.Equals(cur.jsonKey, key, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool IsRubble(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string n = name.ToLowerInvariant();
            return n.IndexOf("rubble", StringComparison.Ordinal) >= 0
                || n.IndexOf("destroy", StringComparison.Ordinal) >= 0
                || n.IndexOf("wreck", StringComparison.Ordinal) >= 0;
        }

        internal static Airbase MenuAirbase(AircraftSelectionMenu menu)
        {
            if (menu == null || MenuAirbaseField == null)
                return null;
            try { return MenuAirbaseField.GetValue(menu) as Airbase; }
            catch { return null; }
        }
    }

    [HarmonyPatch(typeof(Airbase), "AddHangar")]
    internal static class Patch_MiG15S_AddHangar
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Hangar hangar)
        {
            HangarInject.EnsureOnHangar(hangar);
        }
    }

    [HarmonyPatch(typeof(Hangar), "GetAvailableAircraft")]
    internal static class Patch_MiG15S_HangarAvailable
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Hangar __instance)
        {
            HangarInject.EnsureOnHangar(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Hangar __instance, ref AircraftDefinition[] __result)
        {
            HangarInject.EnsureOnHangar(__instance);
            if (AvailableAircraftField() == null)
                return;
            AircraftDefinition[] field = AvailableAircraftField().GetValue(__instance) as AircraftDefinition[];
            if (field != null && field.Length > 0)
                __result = field;
        }

        private static FieldInfo AvailableAircraftField()
        {
            return AccessTools.Field(typeof(Hangar), "availableAircraft");
        }
    }

    [HarmonyPatch(typeof(Hangar), "CanSpawnAircraft")]
    internal static class Patch_MiG15S_HangarCanSpawn
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Hangar __instance)
        {
            HangarInject.EnsureOnHangar(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Hangar __instance, AircraftDefinition definition, ref bool __result)
        {
            if (__result || definition == null || __instance == null)
                return;
            if (!Service.IsOursDef(definition))
                return;
            try
            {
                if (__instance.Disabled)
                    return;
            }
            catch { }
            HangarInject.EnsureOnHangar(__instance);
            if (HangarInject.HangarHasOurs(__instance))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Airbase), "GetAvailableAircraft")]
    internal static class Patch_MiG15S_AirbaseAvailable
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Airbase __instance, List<AircraftDefinition> __result)
        {
            if (__instance == null)
                return;
            try
            {
                if (__instance.disabled)
                    return;
            }
            catch { }
            HangarInject.MergeInto(__result, __instance);
        }
    }

    [HarmonyPatch(typeof(Airbase), "CanSpawnAircraft")]
    internal static class Patch_MiG15S_AirbaseCanSpawn
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Airbase __instance, AircraftDefinition definition, ref bool __result)
        {
            if (__result || __instance == null || definition == null)
                return;
            if (!Service.IsOursDef(definition))
                return;
            List<Hangar> hangars = null;
            try { hangars = __instance.hangars; }
            catch { hangars = null; }
            if (hangars == null)
                return;
            for (int i = 0; i < hangars.Count; i++)
            {
                Hangar h = hangars[i];
                if (h == null)
                    continue;
                try
                {
                    if (h.Disabled)
                        continue;
                }
                catch { }
                HangarInject.EnsureOnHangar(h);
                if (HangarInject.HangarHasOurs(h))
                {
                    __result = true;
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionMenu), "Refresh")]
    internal static class Patch_MiG15S_MenuRefresh
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AircraftSelectionMenu __instance, Airbase airbase)
        {
            HangarInject.InjectMenu(__instance, airbase);
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionMenu), "CanFlyAircraft")]
    internal static class Patch_MiG15S_CanFly
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AircraftSelectionMenu __instance, AircraftDefinition definition, ref bool __result)
        {
            if (__result || definition == null)
                return;
            if (!Service.IsOursDef(definition))
                return;
            HangarInject.InjectMenu(__instance, HangarInject.MenuAirbase(__instance));
            __result = true;
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionButton), "CheckAvailable")]
    internal static class Patch_MiG15S_ButtonAvailable
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AircraftSelectionButton __instance, ref bool __result)
        {
            if (__result || __instance == null)
                return;
            if (Service.IsOursDef(__instance.definition))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(UnitDefinition), "IsAllowed")]
    internal static class Patch_MiG15S_IsAllowed
    {
        [HarmonyPostfix]
        private static void Postfix(UnitDefinition __instance, ref bool __result)
        {
            if (__result || __instance == null)
                return;
            if (Service.IsOursUnit(__instance))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(UnitDefinition), "NotAllowed")]
    internal static class Patch_MiG15S_NotAllowed
    {
        [HarmonyPostfix]
        private static void Postfix(UnitDefinition __instance, ref bool __result)
        {
            if (!__result || __instance == null)
                return;
            if (Service.IsOursUnit(__instance))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionButton), "Setup")]
    internal static class Patch_MiG15S_HangarIconSetup
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AircraftSelectionButton __instance)
        {
            HangarInject.ApplyShortIcon(__instance);
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionButton), "Update")]
    internal static class Patch_MiG15S_HangarIconUpdate
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AircraftSelectionButton __instance)
        {
            HangarInject.ApplyShortIcon(__instance);
        }
    }
}
