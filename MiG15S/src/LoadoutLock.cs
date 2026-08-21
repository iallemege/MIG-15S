using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MiG15S
{
    /// <summary>
    /// MIG-15S stays on KH38MT + its stripped catalog (tailhook) even when
    /// UnrestrictedWeapons / Oritasy unrestricted dumps every mount onto pylons.
    /// </summary>
    internal static class LoadoutLock
    {
        internal const string Kh38Key = "KH38MT";

        private static readonly Dictionary<HardpointSet, HashSet<string>> Catalog =
            new Dictionary<HardpointSet, HashSet<string>>();
        private static readonly HashSet<int> AircraftIds = new HashSet<int>();
        private static readonly FieldInfo AircraftOnWm =
            AccessTools.Field(typeof(WeaponManager), "aircraft");

        internal static void RememberAircraft(Aircraft ac)
        {
            if (ac == null)
                return;
            try { AircraftIds.Add(ac.GetInstanceID()); }
            catch { }
        }

        internal static void RememberCatalog(HardpointSet hs)
        {
            if (hs == null)
                return;
            HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            keys.Add(Kh38Key);
            if (hs.weaponOptions != null)
            {
                for (int i = 0; i < hs.weaponOptions.Count; i++)
                    AddKey(keys, hs.weaponOptions[i]);
            }
            Catalog[hs] = keys;
        }

        internal static bool IsLockedAircraft(Aircraft ac)
        {
            if (ac == null)
                return false;
            try
            {
                if (AircraftIds.Contains(ac.GetInstanceID()))
                    return true;
            }
            catch { }
            return Service.IsOurs(ac);
        }

        internal static bool IsLockedDef(AircraftDefinition def)
        {
            return Service.IsOursDef(def);
        }

        internal static bool IsLockedSet(HardpointSet hs)
        {
            if (hs == null)
                return false;
            if (Catalog.ContainsKey(hs))
                return true;
            Aircraft ac = FindAircraft(hs);
            if (IsLockedAircraft(ac))
            {
                RememberCatalog(hs);
                return true;
            }
            return false;
        }

        internal static bool IsAllowedMount(WeaponMount mount, HardpointSet hs)
        {
            if (mount == null)
                return true;
            if (Service.IsGunMount(mount))
                return false;
            if (IsKh38(mount))
                return true;
            if (mount.tailHook)
                return true;

            HashSet<string> keys;
            if (hs != null && Catalog.TryGetValue(hs, out keys) && keys != null && KeyMatches(keys, mount))
                return true;

            if (hs != null && hs.weaponOptions != null)
            {
                for (int i = 0; i < hs.weaponOptions.Count; i++)
                {
                    WeaponMount opt = hs.weaponOptions[i];
                    if (opt == mount)
                        return true;
                    if (SameKey(opt, mount))
                        return true;
                }
            }
            return false;
        }

        internal static void FilterList(HardpointSet hs, List<WeaponMount> list)
        {
            if (list == null || !IsLockedSet(hs))
                return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                WeaponMount m = list[i];
                if (m != null && !IsAllowedMount(m, hs))
                    list.RemoveAt(i);
            }
        }

        internal static bool IsKh38(WeaponMount m)
        {
            if (m == null)
                return false;
            if (string.Equals(m.jsonKey, Kh38Key, StringComparison.OrdinalIgnoreCase))
                return true;
            if (NameHasKh38(m.mountName))
                return true;
            if (m.info != null && (NameHasKh38(m.info.weaponName) || NameHasKh38(m.info.shortName)))
                return true;
            return false;
        }

        private static bool NameHasKh38(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s.IndexOf("KH38", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Kh-38", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddKey(HashSet<string> keys, WeaponMount m)
        {
            if (m == null || keys == null)
                return;
            if (!string.IsNullOrEmpty(m.jsonKey))
                keys.Add(m.jsonKey);
            if (!string.IsNullOrEmpty(m.mountName))
                keys.Add(m.mountName);
            if (m.info != null)
            {
                if (!string.IsNullOrEmpty(m.info.weaponName))
                    keys.Add(m.info.weaponName);
                if (!string.IsNullOrEmpty(m.info.shortName))
                    keys.Add(m.info.shortName);
            }
        }

        private static bool KeyMatches(HashSet<string> keys, WeaponMount m)
        {
            if (keys == null || m == null)
                return false;
            if (!string.IsNullOrEmpty(m.jsonKey) && keys.Contains(m.jsonKey))
                return true;
            if (!string.IsNullOrEmpty(m.mountName) && keys.Contains(m.mountName))
                return true;
            if (m.info != null)
            {
                if (!string.IsNullOrEmpty(m.info.weaponName) && keys.Contains(m.info.weaponName))
                    return true;
                if (!string.IsNullOrEmpty(m.info.shortName) && keys.Contains(m.info.shortName))
                    return true;
            }
            return false;
        }

        private static bool SameKey(WeaponMount a, WeaponMount b)
        {
            if (a == null || b == null)
                return false;
            if (!string.IsNullOrEmpty(a.jsonKey) && string.Equals(a.jsonKey, b.jsonKey, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        private static Aircraft FindAircraft(HardpointSet hs)
        {
            if (hs == null)
                return null;
            Aircraft[] acs = null;
            try { acs = Resources.FindObjectsOfTypeAll<Aircraft>(); }
            catch { return null; }
            if (acs == null)
                return null;
            for (int i = 0; i < acs.Length; i++)
            {
                Aircraft ac = acs[i];
                if (ac == null || ac.weaponManager == null || ac.weaponManager.hardpointSets == null)
                    continue;
                HardpointSet[] sets = ac.weaponManager.hardpointSets;
                for (int h = 0; h < sets.Length; h++)
                {
                    if (object.ReferenceEquals(sets[h], hs))
                        return ac;
                }
            }
            if (AircraftOnWm == null)
                return null;
            WeaponManager[] wms = null;
            try { wms = Resources.FindObjectsOfTypeAll<WeaponManager>(); }
            catch { return null; }
            if (wms == null)
                return null;
            for (int i = 0; i < wms.Length; i++)
            {
                WeaponManager wm = wms[i];
                if (wm == null || wm.hardpointSets == null)
                    continue;
                bool hit = false;
                for (int h = 0; h < wm.hardpointSets.Length; h++)
                {
                    if (object.ReferenceEquals(wm.hardpointSets[h], hs))
                    {
                        hit = true;
                        break;
                    }
                }
                if (!hit)
                    continue;
                Aircraft fromField = null;
                try { fromField = AircraftOnWm.GetValue(wm) as Aircraft; }
                catch { fromField = null; }
                if (fromField != null)
                    return fromField;
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(WeaponSelector), "Initialize")]
    [HarmonyPatch(new Type[] { typeof(Aircraft), typeof(HardpointSet), typeof(FactionHQ), typeof(Airbase) })]
    internal static class Patch_WeaponSelector_LockCatalog
    {
        private static void Prefix(Aircraft aircraft, HardpointSet hardpointSet)
        {
            if (!LoadoutLock.IsLockedAircraft(aircraft))
                return;
            LoadoutLock.RememberAircraft(aircraft);
            LoadoutLock.RememberCatalog(hardpointSet);
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "GetAvailableWeaponsNonAlloc")]
    internal static class Patch_GetAvailableWeapons_Lock
    {
        [HarmonyFinalizer]
        private static void Finalizer(HardpointSet hardpointSet, List<WeaponMount> outAvailable)
        {
            LoadoutLock.FilterList(hardpointSet, outAvailable);
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "VetWeapon")]
    internal static class Patch_VetWeapon_Lock
    {
        [HarmonyFinalizer]
        private static void Finalizer(
            WeaponMount requestedMount,
            HardpointSet hardpointSet,
            ref bool __result,
            ref string failReason,
            ref int failCost)
        {
            if (!__result || !LoadoutLock.IsLockedSet(hardpointSet))
                return;
            if (LoadoutLock.IsAllowedMount(requestedMount, hardpointSet))
                return;
            __result = false;
            failReason = "MIG-15S locked loadout";
            failCost = 0;
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "MountAllowedHardpoint")]
    internal static class Patch_MountAllowedHardpoint_Lock
    {
        [HarmonyFinalizer]
        private static void Finalizer(WeaponMount mount, HardpointSet hardpointSet, ref bool __result)
        {
            if (!__result || !LoadoutLock.IsLockedSet(hardpointSet))
                return;
            if (LoadoutLock.IsAllowedMount(mount, hardpointSet))
                return;
            __result = false;
        }
    }
}
