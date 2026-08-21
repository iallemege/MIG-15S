using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KH38MT
{
    [HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[] { })]
    internal static class Patch_Encyclopedia_AfterLoad
    {
        private static void Postfix()
        {
            Kh38MtWeapon.Ensure();
        }
    }

    [HarmonyPatch(typeof(WeaponManager), "Awake")]
    internal static class Patch_WeaponManager_Awake
    {
        private static void Postfix(WeaponManager __instance)
        {
            Kh38MtWeapon.Ensure();
            Kh38MtWeapon.InjectIntoWeaponManager(__instance);
        }
    }

    [HarmonyPatch(typeof(WeaponMount), "Initialize")]
    internal static class Patch_WeaponMount_Initialize
    {
        private static void Postfix(WeaponMount __instance)
        {
            if (__instance != null && Kh38MtWeapon.IsKey(__instance.jsonKey))
                Kh38MtWeapon.RestoreMountIdentity(__instance);
        }
    }

    [HarmonyPatch(typeof(Weapon), "AttachToHardpoint")]
    internal static class Patch_Weapon_Attach
    {
        private static void Postfix(Weapon __instance, WeaponMount weaponMount)
        {
            Kh38MtWeapon.SyncFromMount(__instance, weaponMount);
        }
    }

    [HarmonyPatch(typeof(WeaponStation), "RegisterWeapon")]
    internal static class Patch_WeaponStation_Register
    {
        private static void Postfix(WeaponStation __instance, Weapon weapon, WeaponMount weaponMount)
        {
            if (weapon is Gun)
                return;
            Kh38MtWeapon.SyncFromMount(weapon, weaponMount);
            if (__instance != null && weapon != null && weapon.info != null
                && (Kh38MtWeapon.IsMount(weaponMount) || Kh38MtWeapon.IsInfo(weapon.info)))
                __instance.WeaponInfo = weapon.info;
        }
    }

    [HarmonyPatch(typeof(WeaponManager), "RegisterWeapon")]
    internal static class Patch_WeaponManager_Register
    {
        private static void Prefix(Weapon weapon, WeaponMount weaponMount)
        {
            if (weapon == null || weaponMount == null || weapon is Gun)
                return;
            if (!Kh38MtWeapon.IsMount(weaponMount))
                return;
            try
            {
                if (!weapon.gameObject.activeSelf)
                    weapon.gameObject.SetActive(true);
            }
            catch { }
            Kh38MtWeapon.RestoreMountIdentity(weaponMount);
            Kh38MtWeapon.SyncFromMount(weapon, weaponMount);
        }
    }

    [HarmonyPatch(typeof(MountedMissile), "Fire")]
    internal static class Patch_MountedMissile_Fire
    {
        private static void Prefix(MountedMissile __instance)
        {
            Kh38MtWeapon.NoteFire(__instance);
        }
    }

    [HarmonyPatch(typeof(Spawner))]
    internal static class Patch_Spawner_SpawnMissile
    {
        [HarmonyPostfix]
        [HarmonyPatch("SpawnMissile", new Type[] { typeof(MissileDefinition), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
        private static void PostfixDef(Missile __result, Unit owner)
        {
            Kh38MtWeapon.OnSpawned(__result, owner);
        }

        [HarmonyPostfix]
        [HarmonyPatch("SpawnMissile", new Type[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
        private static void PostfixGo(Missile __result, Unit owner)
        {
            Kh38MtWeapon.OnSpawned(__result, owner);
        }
    }

    [HarmonyPatch(typeof(Hardpoint), "SpawnMount")]
    internal static class Patch_Hardpoint_SpawnMount
    {
        private static void Postfix(Hardpoint __instance, Aircraft aircraft, WeaponMount weaponMount, GameObject __result)
        {
            if (__result == null || weaponMount == null || !Kh38MtWeapon.IsMount(weaponMount))
                return;
            Kh38MtWeapon.RestoreMountIdentity(weaponMount);
            Kh38MtVisual.ApplyToHangarRack(__result);
            try
            {
                Weapon[] rails = __result.GetComponentsInChildren<Weapon>(true);
                for (int i = 0; i < rails.Length; i++)
                {
                    Weapon w = rails[i];
                    if (w == null || w is Gun)
                        continue;
                    if (!w.gameObject.activeSelf)
                        w.gameObject.SetActive(true);
                    Kh38MtWeapon.SyncFromMount(w, weaponMount);
                }
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(WeaponSelector), "Initialize")]
    [HarmonyPatch(new Type[] { typeof(Aircraft), typeof(HardpointSet), typeof(FactionHQ), typeof(Airbase) })]
    internal static class Patch_WeaponSelector_Initialize
    {
        private static void Prefix(Aircraft aircraft)
        {
            Kh38MtWeapon.Ensure();
            if (aircraft != null && aircraft.weaponManager != null)
                Kh38MtWeapon.InjectIntoWeaponManager(aircraft.weaponManager);
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "VetWeapon")]
    internal static class Patch_VetWeapon
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(
            WeaponMount requestedMount,
            HardpointSet hardpointSet,
            ref bool __result,
            ref string failReason,
            ref int failCost)
        {
            if (!Kh38MtWeapon.IsMount(requestedMount))
                return true;
            if (!Kh38MtWeapon.IsThirdHardpointSet(hardpointSet) || Plugin.IsNavalHardpoint(hardpointSet))
            {
                __result = false;
                failReason = "KH38MT is third hardpoint only";
                failCost = 0;
                return false;
            }
            __result = true;
            failReason = null;
            failCost = 0;
            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "MountAllowedHardpoint")]
    internal static class Patch_MountAllowedHardpoint
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(WeaponMount mount, HardpointSet hardpointSet, ref bool __result)
        {
            if (!Kh38MtWeapon.IsMount(mount))
                return true;
            __result = Kh38MtWeapon.IsThirdHardpointSet(hardpointSet)
                && !Plugin.IsNavalHardpoint(hardpointSet);
            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponChecker), "GetAvailableWeaponsNonAlloc")]
    internal static class Patch_GetAvailableWeapons
    {
        private static readonly HashSet<WeaponMount> HaveScratch = new HashSet<WeaponMount>();

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(HardpointSet hardpointSet, List<WeaponMount> outAvailable)
        {
            if (outAvailable == null)
                return;
            HaveScratch.Clear();
            for (int i = 0; i < outAvailable.Count; i++)
            {
                if (outAvailable[i] != null)
                    HaveScratch.Add(outAvailable[i]);
            }
            Kh38MtWeapon.Ensure();
            Kh38MtWeapon.FilterAvailable(hardpointSet, outAvailable, HaveScratch);
        }
    }

    [HarmonyPatch(typeof(Weapon), "Fire")]
    internal static class Patch_Weapon_Fire_BlockShip
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Weapon __instance, Unit owner)
        {
            if (__instance == null || !Kh38MtWeapon.IsInfo(__instance.info))
                return true;
            if (owner is Aircraft)
                return true;
            try
            {
                if (owner != null && owner.GetComponentInParent<Aircraft>() != null)
                    return true;
            }
            catch { }
            return false;
        }
    }

    [HarmonyPatch]
    internal static class Patch_HardpointPylon_MatchesMount
    {
        private static readonly FieldInfo BoundMountField;

        static Patch_HardpointPylon_MatchesMount()
        {
            Type nested = AccessTools.Inner(typeof(Hardpoint), "HardpointPylon");
            BoundMountField = nested != null ? AccessTools.Field(nested, "mount") : null;
        }

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Type nested = AccessTools.Inner(typeof(Hardpoint), "HardpointPylon");
            if (nested == null)
                return null;
            return AccessTools.Method(nested, "MatchesMount", new Type[] { typeof(WeaponMount) });
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance, WeaponMount mount, ref bool __result)
        {
            if (__result || mount == null || __instance == null || BoundMountField == null)
                return;
            if (!Kh38MtWeapon.IsMount(mount))
                return;
            WeaponMount bound = null;
            try { bound = BoundMountField.GetValue(__instance) as WeaponMount; }
            catch { return; }
            if (bound == null)
                return;
            if (Kh38MtWeapon.IsMount(bound))
            {
                __result = true;
                return;
            }
            string sn = bound.info != null && bound.info.shortName != null
                ? bound.info.shortName : string.Empty;
            string wn = bound.info != null && bound.info.weaponName != null
                ? bound.info.weaponName : string.Empty;
            if (Kh38MtWeapon.IsAam36DonorKey(bound.jsonKey, sn, wn))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(HUDMissileState), "CalcWeaponRange")]
    internal static class Patch_HUDMissileState_Range
    {
        private static readonly FieldInfo MaxRangeField = AccessTools.Field(typeof(HUDMissileState), "maxRange");
        private static readonly FieldInfo WeaponInfoField = AccessTools.Field(typeof(HUDWeaponState), "weaponInfo");

        [HarmonyPostfix]
        private static void Postfix(HUDMissileState __instance)
        {
            if (__instance == null || MaxRangeField == null || WeaponInfoField == null)
                return;
            WeaponInfo info = null;
            try { info = WeaponInfoField.GetValue(__instance) as WeaponInfo; }
            catch { }
            if (!Kh38MtWeapon.IsInfo(info))
                return;
            try
            {
                float cur = (float)MaxRangeField.GetValue(__instance);
                if (cur < Kh38MtWeapon.RangeM)
                    MaxRangeField.SetValue(__instance, Kh38MtWeapon.RangeM);
            }
            catch { }
        }
    }
}
