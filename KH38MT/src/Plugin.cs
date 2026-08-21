using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace KH38MT
{
    internal static class PluginInfo
    {
        public const string GUID = "com.iallemege.kh38mt";
        public const string Name = "KH38MT";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency("com.iallemege.oritasy", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Plugin Instance;
        internal static ConfigEntry<bool> DebugLog;
        private static Harmony _harmony;
        private static float _nextEnsure;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            DebugLog = Config.Bind("General", "DebugLog", false, "Verbose KH38MT logs.");
            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll();
            SilenceWeXonKh38();
            Log.LogInfo(PluginInfo.Name + " v" + PluginInfo.Version
                + " standalone (AAM-36 rail, AGM-68 warhead, Mach 8).");
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextEnsure)
                return;
            bool ready = Kh38MtWeapon.IsInjected && Kh38MtWeapon.HasUsableClones();
            _nextEnsure = Time.unscaledTime + (ready ? 90f : 2f);
            Kh38MtWeapon.Ensure();
        }

        internal static Encyclopedia GetEncyclopedia()
        {
            try
            {
                PropertyInfo p = AccessTools.Property(typeof(Encyclopedia), "i");
                if (p != null)
                {
                    Encyclopedia via = p.GetValue(null, null) as Encyclopedia;
                    if (IsEncyclopediaPopulated(via))
                        return via;
                    if (via != null)
                        return via;
                }
            }
            catch { }

            Encyclopedia[] all = Resources.FindObjectsOfTypeAll<Encyclopedia>();
            if (all == null || all.Length == 0)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                if (IsEncyclopediaPopulated(all[i]))
                    return all[i];
            }
            return all[0];
        }

        internal static bool IsEncyclopediaPopulated(Encyclopedia enc)
        {
            return enc != null
                && enc.missiles != null && enc.missiles.Count > 0
                && enc.weaponMounts != null && enc.weaponMounts.Count > 0;
        }

        internal static bool IsNavalHardpoint(HardpointSet hs)
        {
            if (hs == null || string.IsNullOrEmpty(hs.name))
                return false;
            string n = hs.name;
            if (n.IndexOf("VLS", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Ship", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Naval", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Cell", StringComparison.OrdinalIgnoreCase) >= 0
                && (n.IndexOf("Launch", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("VLS", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("Mk", StringComparison.OrdinalIgnoreCase) >= 0))
                return true;
            return false;
        }

        internal static WeaponMount GetWeaponMount(Weapon weapon)
        {
            if (weapon == null)
                return null;
            try
            {
                FieldInfo f = AccessTools.Field(typeof(Weapon), "mount");
                if (f != null)
                    return f.GetValue(weapon) as WeaponMount;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 197C Oritasy still ships WeXon.Kh38MtWeapon. Skip those statics so this
        /// submod owns the mount (no double inject / stacked thrust).
        /// </summary>
        private static void SilenceWeXonKh38()
        {
            Harmony hush = new Harmony(PluginInfo.GUID + ".hush-wexon");
            PatchSkipType(hush, AccessTools.TypeByName("WeXon.Kh38MtWeapon"));
            PatchSkipType(hush, AccessTools.TypeByName("WeXon.Kh38MtVisual"));
        }

        private static void PatchSkipType(Harmony hush, Type t)
        {
            if (hush == null || t == null)
                return;
            MethodInfo skip = AccessTools.Method(typeof(Plugin), "HarmonySkip");
            MethodInfo[] methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            int n = 0;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m == null || m.DeclaringType != t || m.IsSpecialName)
                    continue;
                try
                {
                    hush.Patch(m, new HarmonyMethod(skip));
                    n++;
                }
                catch { }
            }
            if (Log != null)
                Log.LogInfo("KH38MT: silenced " + n + " WeXon." + t.Name + " methods");
        }

        private static bool HarmonySkip()
        {
            return false;
        }

        internal static T TryAddBehaviour<T>(GameObject go) where T : MonoBehaviour
        {
            if (go == null)
                return null;
            try
            {
                T existing = go.GetComponent<T>();
                if (existing != null)
                    return existing;
                return go.AddComponent<T>();
            }
            catch (Exception ex)
            {
                if (Log != null)
                    Log.LogWarning("KH38MT AddComponent " + typeof(T).Name + ": " + ex.Message);
                return null;
            }
        }
    }
}
