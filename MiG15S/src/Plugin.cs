using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MiG15S
{
    internal static class PluginInfo
    {
        public const string GUID = "com.ial.mig15s";
        public const string Name = "MiG-15S";
        public const string Version = "1.0.0";
    }

    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Plugin Instance;
        private static Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll();
            Log.LogInfo(PluginInfo.Name + " v" + PluginInfo.Version
                + " loaded (GUID " + PluginInfo.GUID + ").");
        }

        private void Update()
        {
            Service.Tick();
            HangarInject.Tick();
        }

        private void OnGUI()
        {
            Service.Draw();
        }

        internal static bool IsRuntime(Component c)
        {
            if (c == null || c.gameObject == null)
                return false;
            try { return c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded; }
            catch { return false; }
        }
    }
}
