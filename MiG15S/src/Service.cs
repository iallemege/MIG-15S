using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MiG15S
{
    internal static class Service
    {
        internal const string JsonKey = "Aryx_MiG15_KAM";
        internal const string DonorJsonKey = "Aryx_MiG-15";
        internal const string DisplayName = "MIG-15S Kamikaze Drone";
        internal const string ShortName = "MIG-15S";
        internal const string EncDescription =
            "MiG-15S is a fighter that have modernization to work perfect in the 2080s, it have upgrade  all parts from the original version , because of IAL Design Bureau's modify,the engine of MIG-15 has change to IES-200 and add suicide nuclear bomb to do the Kamikaze work.";
        internal const string FuzeHud = "[Suicide fuze armed]";
        internal const string GearUpHint = "[Press \\ to set fuze of suicide bomb]";
        internal const string EjectDenyHud = "you cannot eject because this is kamikaze drone";
        private const int HintFlashCount = 5;
        private const float HintOnSec = 0.4f;
        private const float HintOffSec = 0.25f;
        internal const float Mach099 = 0.99f * 340.3f;
        internal const float ImpactKmh = 150f;
        internal const float ImpactMps = 150f / 3.6f;
        internal const float YieldKt = 1f;
        internal const float BlastYield1kt = 1000000f;
        internal const float DemolishRadius = 15f;
        internal const float TargetTwR = 2f;
        internal const float StrengthMul = 8f;
        // Local +Z is the nose. Main gear sits near z=-0.23; keep CG ahead of it.
        internal const float CgLocalZ = 0.85f;
        internal const float EncCgLocalZ = 1.2f;
        internal const float IrMin = 1.2f;
        internal const float IrMax = 3f;
        internal const float AbThrottleStart = 0.88f;
        internal const float AbThrustMul = 0.45f;

        private static readonly FieldInfo ShockYieldField =
            AccessTools.Field(typeof(Shockwave), "yieldKilotons");
        private static readonly FieldInfo BlastYieldField =
            AccessTools.Field(typeof(Missile), "blastYield");
        private static readonly FieldInfo AeroJoints =
            AccessTools.Field(typeof(AeroPart), "joints");
        private static readonly FieldInfo UnitImpact =
            AccessTools.Field(typeof(UnitPart), "impactDamage");
        private static readonly FieldInfo UnitStructural =
            AccessTools.Field(typeof(UnitPart), "structuralThreshold");
        private static readonly FieldInfo ImpactThreshold =
            AccessTools.Field(typeof(ImpactDamage), "threshold");
        private static readonly FieldInfo ImpactMultiplier =
            AccessTools.Field(typeof(ImpactDamage), "multiplier");
        private static readonly FieldInfo FilterParams =
            AccessTools.Field(typeof(ControlsFilter), "aircraftParameters");
        private static readonly FieldInfo AircraftEjected =
            AccessTools.Field(typeof(Aircraft), "ejected");
        private static readonly FieldInfo MapBuildingSetField =
            AccessTools.Field(typeof(MapBuilding), "buildingSet");
        private static readonly FieldInfo MapBuildingIndexField =
            AccessTools.Field(typeof(MapBuilding), "index");
        private static readonly FieldInfo JetAircraft =
            AccessTools.Field(typeof(Turbojet), "aircraft");
        private static readonly FieldInfo JetThrustNow =
            AccessTools.Field(typeof(Turbojet), "thrust");
        private static readonly FieldInfo JetMinDensity =
            AccessTools.Field(typeof(Turbojet), "minDensity");
        private static readonly FieldInfo JetAltThrust =
            AccessTools.Field(typeof(Turbojet), "altitudeThrust");
        private static readonly FieldInfo JetMaxSpeed =
            AccessTools.Field(typeof(Turbojet), "maxSpeed");
        private static readonly FieldInfo JetOperable =
            AccessTools.Field(typeof(Turbojet), "operable");
        private static readonly FieldInfo TankCapacity =
            AccessTools.Field(typeof(FuelTank), "fuelCapacity");
        private static readonly FieldInfo NozzleAircraft =
            AccessTools.Field(typeof(JetNozzle), "aircraft");
        private static readonly FieldInfo NozzleIRMin =
            AccessTools.Field(typeof(JetNozzle), "IRMin");
        private static readonly FieldInfo NozzleIRMax =
            AccessTools.Field(typeof(JetNozzle), "IRMax");
        private static readonly FieldInfo NozzleAfterburners =
            AccessTools.Field(typeof(JetNozzle), "afterburners");
        private static readonly FieldInfo NozzleGlow =
            AccessTools.Field(typeof(JetNozzle), "glow");
        private static readonly FieldInfo NozzleThrustAudio =
            AccessTools.Field(typeof(JetNozzle), "thrustAudio");
        private static readonly FieldInfo NozzleIrSource =
            AccessTools.Field(typeof(JetNozzle), "irSource");
        private static readonly FieldInfo NozzleThrustXf =
            AccessTools.Field(typeof(JetNozzle), "thrustTransform");
        private static readonly FieldInfo TurbojetAbOn =
            AccessTools.Field(typeof(Turbojet), "afterburnerOn");
        private static readonly Type AfterburnerType =
            AccessTools.Inner(typeof(JetNozzle), "Afterburner");
        private static readonly FieldInfo AbFlameRenderer =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "flameRenderer") : null;
        private static readonly FieldInfo AbGlowRenderer =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "nozzleGlowRenderer") : null;
        private static readonly FieldInfo AbThrottleStartField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "throttleStart") : null;
        private static readonly FieldInfo AbThrottleEndField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "throttleEnd") : null;
        private static readonly FieldInfo AbThrustField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "thrust") : null;
        private static readonly FieldInfo AbFuelField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "fuelConsumption") : null;
        private static readonly FieldInfo AbFlameBrightField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "flameBrightness") : null;
        private static readonly FieldInfo AbGlowBrightField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "nozzleGlowBrightness") : null;
        private static readonly FieldInfo AbIRField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "IRIntensity") : null;
        private static readonly FieldInfo AbSmoothingField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "smoothing") : null;
        private static readonly FieldInfo AbSourceField =
            AfterburnerType != null ? AccessTools.Field(AfterburnerType, "source") : null;
        private static readonly Collider[] DemolishHits = new Collider[192];

        private static readonly HashSet<int> Detonated = new HashSet<int>();
        private static readonly HashSet<int> FuzeOn = new HashSet<int>();
        private static readonly HashSet<int> StrengthDone = new HashSet<int>();
        private static readonly HashSet<int> CgMassDone = new HashSet<int>();
        private static readonly HashSet<int> PilotHidden = new HashSet<int>();
        private static readonly HashSet<int> EngineTuned = new HashSet<int>();
        private static readonly HashSet<int> LiveryDone = new HashSet<int>();
        private static readonly HashSet<int> GunsStripped = new HashSet<int>();
        private static readonly Dictionary<int, float> Mtow = new Dictionary<int, float>();
        private static readonly Dictionary<int, FlightMem> Flight = new Dictionary<int, FlightMem>();
        private static bool _skipDisableBoom;
        internal static bool SkipDisableBoom
        {
            get { return _skipDisableBoom; }
        }
        private static GUIStyle _prompt;
        private static GUIStyle _hintStyle;
        private static GameObject _nukeFx;
        private static int _boundId;
        private static LandingGear.GearState _prevGear = LandingGear.GearState.Uninitialized;
        private static bool _hintUsedThisUp;
        private static float _hintStart;
        private static float _nextEnc;
        private static float _ejectDenyUntil;
        private static GUIStyle _ejectDenyStyle;
        private static float _fuzeArmedAt;

        internal static bool IsOurs(Aircraft ac)
        {
            if (ac == null)
                return false;
            return IsOursDef(ac.definition as AircraftDefinition);
        }

        internal static bool IsOursDef(AircraftDefinition def)
        {
            return IsOursUnit(def);
        }

        internal static bool IsOursUnit(UnitDefinition def)
        {
            if (def == null)
                return false;
            string key = def.jsonKey;
            if (!string.IsNullOrEmpty(key)
                && (string.Equals(key, JsonKey, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, DonorJsonKey, StringComparison.OrdinalIgnoreCase)))
                return true;
            string n = def.unitName != null ? def.unitName : string.Empty;
            if (n.IndexOf("MiG-15S", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("MIG-15S", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Kamikaze", StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("MiG-15", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private sealed class FlightMem
        {
            public float LastFastMps;
            public float LastFastAt;
            public bool GearUp;
            public bool Airborne;
        }

        private static FlightMem Mem(Aircraft ac)
        {
            int id = ac.GetInstanceID();
            FlightMem m;
            if (!Flight.TryGetValue(id, out m) || m == null)
            {
                m = new FlightMem();
                Flight[id] = m;
            }
            return m;
        }

        internal static void RememberFlight(Aircraft ac)
        {
            if (ac == null)
                return;
            FlightMem m = Mem(ac);
            float spd = 0f;
            try { spd = ac.speed; }
            catch { spd = 0f; }
            if (spd >= ImpactMps)
            {
                m.LastFastMps = spd;
                m.LastFastAt = Time.unscaledTime;
            }
            float alt = 0f;
            try { alt = ac.radarAlt; }
            catch { alt = 0f; }
            LandingGear.GearState gear = LandingGear.GearState.Uninitialized;
            try { gear = ac.gearState; }
            catch { gear = LandingGear.GearState.Uninitialized; }
            bool gearUp = gear == LandingGear.GearState.Retracting
                || gear == LandingGear.GearState.LockedRetracted;
            if (gearUp)
                m.GearUp = true;
            if (alt > 8f)
                m.Airborne = true;
            if (!gearUp && alt < 12f)
            {
                m.GearUp = false;
                m.Airborne = false;
                if (spd < ImpactMps)
                    m.LastFastAt = 0f;
            }
        }

        private static bool GearIsUp(Aircraft ac)
        {
            if (ac == null)
                return false;
            LandingGear.GearState gear = LandingGear.GearState.Uninitialized;
            try { gear = ac.gearState; }
            catch { return false; }
            return gear == LandingGear.GearState.Retracting
                || gear == LandingGear.GearState.LockedRetracted;
        }

        private static bool FuzeSettled(Aircraft ac)
        {
            if (!FuzeArmed(ac))
                return false;
            if (_fuzeArmedAt <= 0.01f)
                return false;
            return (Time.unscaledTime - _fuzeArmedAt) >= 1f;
        }

        private static bool RecentFast(Aircraft ac)
        {
            if (ac == null)
                return false;
            FlightMem m = Mem(ac);
            if (m.LastFastMps >= ImpactMps && (Time.unscaledTime - m.LastFastAt) < 2f)
                return true;
            float spd = 0f;
            try { spd = ac.speed; }
            catch { spd = 0f; }
            return spd >= ImpactMps;
        }

        private static bool WasFlying(Aircraft ac)
        {
            if (ac == null)
                return false;
            FlightMem m = Mem(ac);
            return m.GearUp || m.Airborne;
        }

        internal static void Watchdog(Aircraft ac)
        {
            if (ac == null || !IsOurs(ac) || !FuzeArmed(ac) || !FuzeSettled(ac))
                return;
            if (!GearIsUp(ac))
                return;
            if (!WasFlying(ac) || !RecentFast(ac))
                return;
            float spd = 0f;
            try { spd = ac.speed; }
            catch { spd = 0f; }
            float alt = 0f;
            try { alt = ac.radarAlt; }
            catch { alt = 0f; }
            bool dead = false;
            try { dead = ac.disabled; }
            catch { dead = false; }
            if (dead)
            {
                TryDetonate(ac, "watchdog-dead");
                return;
            }
            if (spd < 12f && alt < 40f)
                TryDetonate(ac, "watchdog-impact");
        }

        internal static void NoteHit(Aircraft ac, Collision collision)
        {
            if (ac == null || !IsOurs(ac) || !FuzeArmed(ac) || !FuzeSettled(ac))
                return;
            if (!GearIsUp(ac))
                return;
            if (!WasFlying(ac) && !RecentFast(ac))
                return;
            if (!RecentFast(ac))
                return;
            if (IsSelfHit(ac, collision))
                return;
            TryDetonate(ac, "impact");
        }

        private static bool IsSelfHit(Aircraft ac, Collision collision)
        {
            if (ac == null || collision == null)
                return false;
            Transform other = null;
            try { other = collision.collider != null ? collision.collider.transform : null; }
            catch { other = null; }
            if (other == null)
                return false;
            Aircraft otherAc = other.GetComponentInParent<Aircraft>();
            return otherAc != null && object.ReferenceEquals(otherAc, ac);
        }

        internal static bool FuzeArmed(Aircraft ac)
        {
            if (ac == null)
                return false;
            return FuzeOn.Contains(ac.GetInstanceID());
        }

        internal static void Tick()
        {
            if (Time.unscaledTime >= _nextEnc)
            {
                _nextEnc = Time.unscaledTime + 2f;
                StampAllDefs();
            }
            Aircraft ac;
            if (!GameManager.GetLocalAircraft(out ac) || ac == null)
            {
                ResetHintLocal();
                return;
            }
            if (!IsOurs(ac) || !Plugin.IsRuntime(ac) || ac.disabled)
            {
                ResetHintLocal();
                return;
            }
            PollGearHint(ac);
            if (!InEncyclopedia() && Input.GetKeyDown(KeyCode.BackQuote))
            {
                TryDetonate(ac, "manual", false);
            }
            if (!Input.GetKeyDown(KeyCode.Backslash))
                return;
            int id = ac.GetInstanceID();
            if (FuzeOn.Contains(id))
            {
                FuzeOn.Remove(id);
                _fuzeArmedAt = 0f;
            }
            else
            {
                FuzeOn.Add(id);
                _fuzeArmedAt = Time.unscaledTime;
                _hintStart = 0f;
            }
        }

        internal static void StampAllDefs()
        {
            AircraftDefinition[] defs = null;
            try { defs = Resources.FindObjectsOfTypeAll<AircraftDefinition>(); }
            catch { defs = null; }
            if (defs == null)
                return;
            for (int i = 0; i < defs.Length; i++)
                ApplyEncyclopedia(defs[i]);
        }

        private static void ResetHintLocal()
        {
            _boundId = 0;
            _prevGear = LandingGear.GearState.Uninitialized;
            _hintUsedThisUp = false;
            _hintStart = 0f;
        }

        private static void PollGearHint(Aircraft ac)
        {
            int id = ac.GetInstanceID();
            if (id != _boundId)
            {
                _boundId = id;
                _prevGear = LandingGear.GearState.Uninitialized;
                _hintUsedThisUp = false;
                _hintStart = 0f;
            }
            LandingGear.GearState now = LandingGear.GearState.Uninitialized;
            try { now = ac.gearState; }
            catch { now = LandingGear.GearState.Uninitialized; }
            LandingGear.GearState prev = _prevGear;
            _prevGear = now;
            float alt = 0f;
            try { alt = ac.radarAlt; }
            catch { alt = 0f; }
            bool down = now == LandingGear.GearState.LockedExtended
                || now == LandingGear.GearState.Extending;
            if (down && alt < 12f)
            {
                _hintUsedThisUp = false;
                return;
            }
            if (_hintUsedThisUp)
                return;
            if (FuzeArmed(ac))
                return;
            bool wasDown = prev == LandingGear.GearState.LockedExtended
                || prev == LandingGear.GearState.Extending;
            bool goingUp = now == LandingGear.GearState.Retracting
                || now == LandingGear.GearState.LockedRetracted;
            if (!wasDown || !goingUp)
                return;
            if (alt < 3f)
            {
                float spd = 0f;
                try { spd = ac.speed; }
                catch { spd = 0f; }
                if (spd < 40f)
                    return;
            }
            _hintUsedThisUp = true;
            _hintStart = Time.unscaledTime;
        }

        private static bool HintFlashVisible()
        {
            if (_hintStart <= 0.01f)
                return false;
            float cycle = HintOnSec + HintOffSec;
            float elapsed = Time.unscaledTime - _hintStart;
            if (elapsed < 0f)
                return false;
            int n = (int)(elapsed / cycle);
            if (n >= HintFlashCount)
                return false;
            float phase = elapsed - (n * cycle);
            return phase < HintOnSec;
        }

        internal static void Draw()
        {
            Aircraft ac;
            if (!GameManager.GetLocalAircraft(out ac) || ac == null)
                return;
            if (!IsOurs(ac) || !Plugin.IsRuntime(ac) || ac.disabled)
                return;
            string line = null;
            if (Time.unscaledTime < _ejectDenyUntil)
                line = EjectDenyHud;
            else if (FuzeArmed(ac))
                line = FuzeHud;
            else if (HintFlashVisible())
                line = GearUpHint;
            if (line == null)
                return;
            if (_ejectDenyStyle == null)
            {
                _ejectDenyStyle = new GUIStyle(GUI.skin.label);
                _ejectDenyStyle.alignment = TextAnchor.MiddleCenter;
                _ejectDenyStyle.fontSize = 26;
                _ejectDenyStyle.fontStyle = FontStyle.Bold;
                _ejectDenyStyle.normal.textColor = new Color(1f, 0.2f, 0.15f, 1f);
            }
            if (_prompt == null)
            {
                _prompt = new GUIStyle(GUI.skin.label);
                _prompt.alignment = TextAnchor.MiddleCenter;
                _prompt.fontSize = 22;
                _prompt.fontStyle = FontStyle.Bold;
                _prompt.normal.textColor = new Color(1f, 0.85f, 0.15f, 1f);
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label);
                _hintStyle.alignment = TextAnchor.MiddleCenter;
                _hintStyle.fontSize = 22;
                _hintStyle.fontStyle = FontStyle.Bold;
                _hintStyle.normal.textColor = new Color(1f, 0.92f, 0.35f, 1f);
            }
            float w = 720f;
            Rect r = new Rect((Screen.width - w) * 0.5f, 36f, w, 40f);
            GUIStyle st = _hintStyle;
            if (line == EjectDenyHud)
                st = _ejectDenyStyle;
            else if (FuzeArmed(ac))
                st = _prompt;
            GUI.Label(r, line, st);
        }

        internal static void ApplyEncyclopedia(AircraftDefinition def)
        {
            if (!IsOursDef(def))
                return;
            def.unitName = DisplayName;
            def.code = ShortName;
            def.bogeyName = ShortName;
            if (def.aircraftParameters != null)
            {
                def.aircraftParameters.aircraftName = DisplayName;
                def.aircraftParameters.maxSpeed = Mach099;
            }
            if (def.aircraftInfo != null)
                def.aircraftInfo.maxSpeed = Mach099;
            def.description = EncDescription;
            if (Encyclopedia.Lookup != null)
            {
                if (!string.IsNullOrEmpty(def.jsonKey))
                    Encyclopedia.Lookup[def.jsonKey] = def;
                Encyclopedia.Lookup[JsonKey] = def;
            }
            StripGunsFromDefinition(def);
        }

        internal static void ApplyPerformance(Aircraft ac)
        {
            if (!IsOurs(ac) || !Plugin.IsRuntime(ac))
                return;
            ApplyEncyclopedia(ac.definition as AircraftDefinition);
            ApplyNoseBallast(ac);
            HidePilotModel(ac);
            StripGuns(ac);
            ApplyFactionLivery(ac);
            if (InEncyclopedia())
            {
                PoseEncyclopediaAircraft(ac);
                return;
            }
            ApplyThrust(ac);
            ApplyIrAndAfterburner(ac);
            AircraftDefinition def = ac.definition as AircraftDefinition;
            if (def != null && def.aircraftParameters != null)
                def.aircraftParameters.maxSpeed = Mach099;
            if (def != null && def.aircraftInfo != null)
                def.aircraftInfo.maxSpeed = Mach099;
            if (FilterParams != null)
            {
                try
                {
                    ControlsFilter cf = ac.GetControlsFilter();
                    if (cf != null)
                    {
                        AircraftParameters p = FilterParams.GetValue(cf) as AircraftParameters;
                        if (p != null)
                            p.maxSpeed = Mach099;
                    }
                }
                catch { }
            }
            if (StrengthDone.Add(ac.GetInstanceID()))
                BuffAirframe(ac);
            ApplyCg(ac, CgLocalZ);
        }

        internal static bool IsGunMount(WeaponMount m)
        {
            if (m == null)
                return false;
            if (IsGunKey(m.jsonKey))
                return true;
            string n = m.mountName != null ? m.mountName : string.Empty;
            if (n.IndexOf("23mm", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("37mm", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("57mm", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("27mm", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (m.info != null && m.info.gun)
                return true;
            return false;
        }

        private static bool IsGunKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;
            if (key.StartsWith("Aryx_MiG15_Gun", StringComparison.OrdinalIgnoreCase))
                return true;
            if (key.IndexOf("WeaponMount_23mm", StringComparison.OrdinalIgnoreCase) >= 0
                || key.IndexOf("WeaponMount_37mm", StringComparison.OrdinalIgnoreCase) >= 0
                || key.IndexOf("WeaponMount_57mm", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return false;
        }

        private static void StripWeaponList(List<WeaponMount> list)
        {
            if (list == null)
                return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (IsGunMount(list[i]))
                    list.RemoveAt(i);
            }
        }

        private static void StripGunsFromDefinition(AircraftDefinition def)
        {
            if (def == null || def.aircraftParameters == null)
                return;
            AircraftParameters p = def.aircraftParameters;
            if (p.loadouts != null)
            {
                for (int i = 0; i < p.loadouts.Count; i++)
                {
                    NuclearOption.SavedMission.Loadout lo = p.loadouts[i];
                    if (lo != null)
                        StripWeaponList(lo.weapons);
                }
            }
            if (p.StandardLoadouts != null)
            {
                for (int i = 0; i < p.StandardLoadouts.Length; i++)
                {
                    StandardLoadout sl = p.StandardLoadouts[i];
                    if (sl == null || sl.loadout == null)
                        continue;
                    StripWeaponList(sl.loadout.weapons);
                    sl.Name = "Kamikaze";
                }
            }
        }

        private static void StripGuns(Aircraft ac)
        {
            if (ac == null)
                return;
            int id = ac.GetInstanceID();
            if (!GunsStripped.Add(id))
                return;
            StripGunsFromDefinition(ac.definition as AircraftDefinition);
            WeaponManager wm = ac.weaponManager;
            if (wm == null || wm.hardpointSets == null)
                return;
            for (int h = 0; h < wm.hardpointSets.Length; h++)
            {
                HardpointSet hs = wm.hardpointSets[h];
                if (hs == null)
                    continue;
                StripWeaponList(hs.weaponOptions);
                LoadoutLock.RememberCatalog(hs);
            }
            LoadoutLock.RememberAircraft(ac);
        }

        private static void ApplyFactionLivery(Aircraft ac)
        {
            if (ac == null)
                return;
            int id = ac.GetInstanceID();
            if (!LiveryDone.Add(id))
                return;
            try
            {
                ac.SetLiveryKey(new LiveryKey(0), true);
            }
            catch { }

            LiveryData data = FindLiveryData(ac);
            if (data == null)
                return;
            UnitPart[] parts = null;
            try { parts = ac.GetComponentsInChildren<UnitPart>(true); }
            catch { parts = null; }
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == null)
                        continue;
                    try { parts[i].SetLivery(data, null); }
                    catch { }
                }
            }
            try
            {
                if (ac.weaponManager != null)
                    ac.weaponManager.UpdateColorables(data);
            }
            catch { }
        }

        private static LiveryData FindLiveryData(Aircraft ac)
        {
            string want = "Aryx_MiG-15_Livery_BDF_1";
            try
            {
                FactionHQ hq = ac.NetworkHQ;
                if (hq == null)
                    hq = ac.MapHQ;
                Faction fac = hq != null ? hq.faction : null;
                string n = fac != null && fac.factionName != null ? fac.factionName : string.Empty;
                string tag = fac != null && fac.factionTag != null ? fac.factionTag : string.Empty;
                string blob = n + " " + tag;
                if (blob.IndexOf("Primeva", StringComparison.OrdinalIgnoreCase) >= 0
                    || blob.IndexOf("PALA", StringComparison.OrdinalIgnoreCase) >= 0)
                    want = "Aryx_MiG-15_Livery_PALA_1";
            }
            catch { }

            LiveryData[] all = null;
            try { all = Resources.FindObjectsOfTypeAll<LiveryData>(); }
            catch { all = null; }
            if (all == null)
                return null;
            LiveryData fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                LiveryData d = all[i];
                if (d == null || string.IsNullOrEmpty(d.name))
                    continue;
                if (string.Equals(d.name, want, StringComparison.OrdinalIgnoreCase))
                    return d;
                if (fallback == null && d.name.IndexOf("Aryx_MiG-15_Livery", StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = d;
            }
            return fallback;
        }

        internal static void HidePilotModel(Aircraft ac)
        {
            if (ac == null)
                return;
            int id = ac.GetInstanceID();
            if (!PilotHidden.Add(id))
                return;
            Renderer[] rs = null;
            try { rs = ac.GetComponentsInChildren<Renderer>(true); }
            catch { rs = null; }
            if (rs == null)
                return;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                if (!UnderPilotVisual(rs[i].transform))
                    continue;
                try { rs[i].enabled = false; }
                catch { }
            }
        }

        private static bool UnderPilotVisual(Transform t)
        {
            Transform c = t;
            int guard = 0;
            while (c != null && guard < 16)
            {
                guard++;
                string n = c.name != null ? c.name : string.Empty;
                if (string.Equals(n, "pilot", StringComparison.OrdinalIgnoreCase)
                    || n.IndexOf("pilot_armature", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("PilotMesh", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("pilotMesh", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                try { c = c.parent; }
                catch { break; }
            }
            return false;
        }

        internal static bool BlockEjection(Aircraft ac)
        {
            return ac != null && IsOurs(ac);
        }

        internal static void NotifyCannotEject(Aircraft ac)
        {
            if (ac == null || !IsOurs(ac))
                return;
            bool local = false;
            try { local = GameManager.IsLocalAircraft(ac); }
            catch { local = true; }
            if (!local)
            {
                try
                {
                    Aircraft mine;
                    if (GameManager.GetLocalAircraft(out mine) && mine != null
                        && object.ReferenceEquals(mine, ac))
                        local = true;
                }
                catch { }
            }
            if (!local)
                return;
            _ejectDenyUntil = Time.unscaledTime + 2.5f;
        }

        internal static void ApplyThrust(Aircraft ac)
        {
            if (ac == null)
                return;
            float thrust = FullLoadThrust(ac);
            Turbojet[] jets = null;
            try { jets = ac.GetComponentsInChildren<Turbojet>(true); }
            catch { jets = null; }
            if (jets == null)
                return;
            for (int i = 0; i < jets.Length; i++)
                ForceJetThrust(jets[i], ac, thrust);
            ApplyIrAndAfterburner(ac);
        }

        internal static Aircraft AircraftOfNozzle(JetNozzle nozzle)
        {
            if (nozzle == null)
                return null;
            Aircraft ac = null;
            try
            {
                if (NozzleAircraft != null)
                    ac = NozzleAircraft.GetValue(nozzle) as Aircraft;
            }
            catch { ac = null; }
            if (ac == null)
            {
                try { ac = nozzle.GetComponentInParent<Aircraft>(); }
                catch { ac = null; }
            }
            return ac;
        }

        internal static bool OursNozzle(JetNozzle nozzle)
        {
            return IsOurs(AircraftOfNozzle(nozzle));
        }

        internal static void ForceAllowAfterburner(JetNozzle nozzle, ref bool allowAfterburner)
        {
            if (OursNozzle(nozzle))
                allowAfterburner = true;
        }

        internal static void ClampNozzleIR(JetNozzle nozzle, bool max, ref float result)
        {
            if (!OursNozzle(nozzle))
                return;
            result = max ? IrMax : IrMin;
        }

        internal static void ApplyIrAndAfterburner(Aircraft ac)
        {
            if (ac == null)
                return;
            JetNozzle[] nozzles = null;
            try { nozzles = ac.GetComponentsInChildren<JetNozzle>(true); }
            catch { nozzles = null; }
            if (nozzles == null)
                return;
            float extra = FullLoadThrust(ac) * AbThrustMul;
            bool abOn = false;
            try
            {
                ControlInputs inputs = ac.GetInputs();
                if (inputs != null && inputs.throttle >= AbThrottleStart)
                    abOn = true;
            }
            catch { }
            for (int i = 0; i < nozzles.Length; i++)
            {
                JetNozzle n = nozzles[i];
                if (n == null)
                    continue;
                if (NozzleIRMin != null)
                {
                    try { NozzleIRMin.SetValue(n, IrMin); }
                    catch { }
                }
                if (NozzleIRMax != null)
                {
                    try { NozzleIRMax.SetValue(n, IrMax); }
                    catch { }
                }
                if (NozzleIrSource != null)
                {
                    try
                    {
                        IRSource src = NozzleIrSource.GetValue(n) as IRSource;
                        if (src != null && src.intensity > IrMax)
                            src.intensity = IrMax;
                    }
                    catch { }
                }
                EnsureAfterburner(n, ac, extra);
            }
            Turbojet[] jets = null;
            try { jets = ac.GetComponentsInChildren<Turbojet>(true); }
            catch { jets = null; }
            if (jets != null && TurbojetAbOn != null)
            {
                for (int i = 0; i < jets.Length; i++)
                {
                    if (jets[i] == null)
                        continue;
                    try { TurbojetAbOn.SetValue(jets[i], abOn); }
                    catch { }
                }
            }
            TickAbFlame(ac, abOn);
        }

        private static void EnsureAfterburner(JetNozzle nozzle, Aircraft ac, float extraThrust)
        {
            if (nozzle == null || AfterburnerType == null || NozzleAfterburners == null)
                return;
            object arrObj = null;
            try { arrObj = NozzleAfterburners.GetValue(nozzle); }
            catch { arrObj = null; }
            Array arr = arrObj as Array;
            object ab = null;
            if (arr != null && arr.Length > 0)
                ab = arr.GetValue(0);
            if (ab == null)
            {
                try { ab = AccessTools.CreateInstance(AfterburnerType); }
                catch { ab = null; }
                if (ab == null)
                    return;
                Array created = Array.CreateInstance(AfterburnerType, 1);
                created.SetValue(ab, 0);
                try { NozzleAfterburners.SetValue(nozzle, created); }
                catch { return; }
            }
            if (AbThrottleStartField != null)
            {
                try { AbThrottleStartField.SetValue(ab, AbThrottleStart); }
                catch { }
            }
            if (AbThrottleEndField != null)
            {
                try { AbThrottleEndField.SetValue(ab, 1f); }
                catch { }
            }
            if (AbThrustField != null)
            {
                try { AbThrustField.SetValue(ab, extraThrust); }
                catch { }
            }
            if (AbFuelField != null)
            {
                try { AbFuelField.SetValue(ab, extraThrust * 0.00008f); }
                catch { }
            }
            if (AbFlameBrightField != null)
            {
                try { AbFlameBrightField.SetValue(ab, 4.5f); }
                catch { }
            }
            if (AbGlowBrightField != null)
            {
                try { AbGlowBrightField.SetValue(ab, 3.5f); }
                catch { }
            }
            if (AbIRField != null)
            {
                try { AbIRField.SetValue(ab, IrMax); }
                catch { }
            }
            if (AbSmoothingField != null)
            {
                try { AbSmoothingField.SetValue(ab, 0.12f); }
                catch { }
            }
            Renderer flame = FindNamedRenderer(ac, "glowEffect");
            if (flame == null)
                flame = FindNamedRenderer(ac, "glow");
            Renderer haze = FindNamedRenderer(ac, "HeatBlur");
            if (haze == null)
                haze = FindNamedRenderer(ac, "heat");
            Renderer made = EnsureAbFlameVisual(nozzle, ac);
            if (made != null)
                flame = made;
            if (AbFlameRenderer != null && flame != null)
            {
                try { AbFlameRenderer.SetValue(ab, flame); }
                catch { }
            }
            if (AbGlowRenderer != null && haze != null)
            {
                try { AbGlowRenderer.SetValue(ab, haze); }
                catch { }
            }
            if (AbSourceField != null && NozzleThrustAudio != null)
            {
                try
                {
                    object src = NozzleThrustAudio.GetValue(nozzle);
                    if (src != null)
                        AbSourceField.SetValue(ab, src);
                }
                catch { }
            }
        }

        private static Renderer FindNamedRenderer(Aircraft ac, string part)
        {
            if (ac == null || string.IsNullOrEmpty(part))
                return null;
            Renderer[] rs = null;
            try { rs = ac.GetComponentsInChildren<Renderer>(true); }
            catch { rs = null; }
            if (rs == null)
                return null;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                string n = rs[i].gameObject.name;
                if (n != null && n.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    return rs[i];
            }
            return null;
        }

        private static Renderer EnsureAbFlameVisual(JetNozzle nozzle, Aircraft ac)
        {
            if (nozzle == null || ac == null)
                return null;
            Transform parent = nozzle.transform;
            try
            {
                if (NozzleThrustXf != null)
                {
                    Transform xf = NozzleThrustXf.GetValue(nozzle) as Transform;
                    if (xf != null)
                        parent = xf;
                }
            }
            catch { }
            Transform existing = null;
            try { existing = parent.Find("MIG15S_ABFlame"); }
            catch { existing = null; }
            if (existing == null && ac.transform != null)
            {
                try { existing = ac.transform.Find("MIG15S_ABFlame"); }
                catch { existing = null; }
            }
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "MIG15S_ABFlame";
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                    UnityEngine.Object.Destroy(col);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = new Vector3(0f, 0f, 0.55f);
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale = new Vector3(0.22f, 0.7f, 0.22f);
                go.layer = ac.gameObject.layer;
            }
            Renderer r = go.GetComponent<Renderer>();
            if (r == null)
                return null;
            if (r.sharedMaterial == null || r.sharedMaterial.name.IndexOf("MIG15S_AB") < 0)
            {
                Shader sh = Shader.Find("Legacy Shaders/Particles/Additive");
                if (sh == null)
                    sh = Shader.Find("Particles/Additive");
                if (sh == null)
                    sh = Shader.Find("Unlit/Color");
                if (sh == null)
                    sh = Shader.Find("Standard");
                if (sh != null)
                {
                    Material mat = new Material(sh);
                    mat.name = "MIG15S_AB";
                    mat.color = new Color(1f, 0.55f, 0.15f, 0.85f);
                    r.material = mat;
                }
            }
            r.enabled = false;
            return r;
        }

        private static void TickAbFlame(Aircraft ac, bool abOn)
        {
            if (ac == null)
                return;
            Transform t = null;
            try { t = ac.transform.Find("MIG15S_ABFlame"); }
            catch { t = null; }
            if (t == null)
            {
                JetNozzle[] nozzles = null;
                try { nozzles = ac.GetComponentsInChildren<JetNozzle>(true); }
                catch { nozzles = null; }
                if (nozzles != null)
                {
                    for (int i = 0; i < nozzles.Length; i++)
                    {
                        if (nozzles[i] == null)
                            continue;
                        Transform p = nozzles[i].transform;
                        Transform hit = p.Find("MIG15S_ABFlame");
                        if (hit == null && p.childCount > 0)
                        {
                            for (int c = 0; c < p.childCount; c++)
                            {
                                Transform ch = p.GetChild(c);
                                if (ch != null && ch.name == "MIG15S_ABFlame")
                                {
                                    hit = ch;
                                    break;
                                }
                            }
                        }
                        if (hit == null)
                            continue;
                        Renderer rr = hit.GetComponent<Renderer>();
                        if (rr != null)
                            rr.enabled = abOn;
                        hit.localScale = abOn
                            ? new Vector3(0.32f, 1.15f, 0.32f)
                            : new Vector3(0.18f, 0.45f, 0.18f);
                    }
                }
                return;
            }
            Renderer r = t.GetComponent<Renderer>();
            if (r != null)
                r.enabled = abOn;
        }

        internal static void ForceJetThrust(Turbojet jet)
        {
            if (jet == null)
                return;
            Aircraft ac = null;
            try
            {
                if (JetAircraft != null)
                    ac = JetAircraft.GetValue(jet) as Aircraft;
            }
            catch { ac = null; }
            if (ac == null)
            {
                try { ac = jet.GetComponentInParent<Aircraft>(); }
                catch { ac = null; }
            }
            if (ac == null || !IsOurs(ac))
                return;
            if (InEncyclopedia())
            {
                jet.maxThrust = 0f;
                return;
            }
            ForceJetThrust(jet, ac, FullLoadThrust(ac));
        }

        internal static bool TryOursMaxThrust(Turbojet jet, out float thrust)
        {
            thrust = 0f;
            if (jet == null)
                return false;
            Aircraft ac = null;
            try
            {
                if (JetAircraft != null)
                    ac = JetAircraft.GetValue(jet) as Aircraft;
            }
            catch { ac = null; }
            if (ac == null)
            {
                try { ac = jet.GetComponentInParent<Aircraft>(); }
                catch { ac = null; }
            }
            if (ac == null || !IsOurs(ac) || InEncyclopedia())
                return false;
            thrust = FullLoadThrust(ac);
            return thrust > 1f;
        }

        private static void ForceJetThrust(Turbojet jet, Aircraft ac, float thrust)
        {
            if (jet == null || thrust < 1f)
                return;
            jet.maxThrust = thrust;
            int id = jet.GetInstanceID();
            if (EngineTuned.Add(id))
            {
                if (JetMinDensity != null)
                {
                    try { JetMinDensity.SetValue(jet, 0.01f); }
                    catch { }
                }
                if (JetMaxSpeed != null)
                {
                    try { JetMaxSpeed.SetValue(jet, Mach099); }
                    catch { }
                }
                if (JetAltThrust != null)
                {
                    try { JetAltThrust.SetValue(jet, AnimationCurve.Linear(0f, 1f, 30000f, 1f)); }
                    catch { }
                }
            }
            if (JetOperable != null)
            {
                try { JetOperable.SetValue(jet, true); }
                catch { }
            }
        }

        private static float FullLoadThrust(Aircraft ac)
        {
            float mass = FullLoadMass(ac);
            float thrust = TargetTwR * mass * 9.81f;
            if (thrust < 40000f)
                thrust = 40000f;
            return thrust;
        }

        private static float FullLoadMass(Aircraft ac)
        {
            if (ac == null)
                return 5055f;
            int id = ac.GetInstanceID();
            float cached;
            if (Mtow.TryGetValue(id, out cached) && cached >= 4000f)
                return cached;
            float parts = 0f;
            UnitPart[] up = null;
            try { up = ac.GetComponentsInChildren<UnitPart>(true); }
            catch { up = null; }
            if (up != null)
            {
                for (int i = 0; i < up.Length; i++)
                {
                    if (up[i] == null)
                        continue;
                    if (up[i].mass > 0f)
                        parts += up[i].mass;
                }
            }
            float fuel = 0f;
            FuelTank[] tanks = null;
            try { tanks = ac.GetComponentsInChildren<FuelTank>(true); }
            catch { tanks = null; }
            if (tanks != null)
            {
                for (int i = 0; i < tanks.Length; i++)
                {
                    if (tanks[i] == null)
                        continue;
                    float cap = 0f;
                    try { cap = tanks[i].GetCapacity(); }
                    catch { cap = 0f; }
                    if (cap < 1f && TankCapacity != null)
                    {
                        try { cap = (float)TankCapacity.GetValue(tanks[i]); }
                        catch { cap = 0f; }
                    }
                    if (cap >= 1f && cap < 1200f)
                        fuel += cap;
                }
            }
            float mass = parts + fuel;
            float live = 0f;
            try
            {
                if (ac.rb != null)
                    live = ac.rb.mass;
            }
            catch { live = 0f; }
            if (live > mass)
                mass = live;
            if (mass < 4000f)
                mass = 5055f;
            Mtow[id] = mass;
            return mass;
        }

        internal static bool InEncyclopedia()
        {
            try
            {
                return GameManager.gameState == GameState.Encyclopedia;
            }
            catch
            {
                return false;
            }
        }

        internal static void PoseEncyclopediaAircraft(Aircraft ac)
        {
            if (ac == null || !IsOurs(ac))
                return;
            ApplyNoseBallast(ac);
            HidePilotModel(ac);
            ApplyIrAndAfterburner(ac);
            ApplyCg(ac, EncCgLocalZ);
            try { ac.SetGear(true); }
            catch { }
            Turbojet[] jets = null;
            try { jets = ac.GetComponentsInChildren<Turbojet>(true); }
            catch { jets = null; }
            if (jets != null)
            {
                for (int i = 0; i < jets.Length; i++)
                {
                    if (jets[i] == null)
                        continue;
                    jets[i].maxThrust = 0f;
                }
            }
            try
            {
                ControlInputs inputs = ac.GetInputs();
                if (inputs != null)
                    inputs.throttle = 0f;
            }
            catch { }
            if (ac.rb != null)
            {
                try
                {
                    ac.rb.automaticCenterOfMass = false;
                    Vector3 c = ac.rb.centerOfMass;
                    ac.rb.centerOfMass = new Vector3(0f, c.y, EncCgLocalZ);
                    ac.rb.velocity = Vector3.zero;
                    ac.rb.angularVelocity = Vector3.zero;
                    ac.rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                catch { }
            }
            try
            {
                Vector3 e = ac.transform.eulerAngles;
                ac.transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            }
            catch { }
        }

        private static void ApplyNoseBallast(Aircraft ac)
        {
            if (ac == null)
                return;
            if (!CgMassDone.Add(ac.GetInstanceID()))
                return;
            UnitPart[] parts = null;
            try { parts = ac.GetComponentsInChildren<UnitPart>(true); }
            catch { parts = null; }
            if (parts == null)
                return;
            for (int i = 0; i < parts.Length; i++)
            {
                UnitPart p = parts[i];
                if (p == null)
                    continue;
                string n = p.name != null ? p.name : string.Empty;
                if (n.IndexOf("Intake", StringComparison.OrdinalIgnoreCase) >= 0)
                    p.mass += 650f;
                else if (n.IndexOf("Cockpit", StringComparison.OrdinalIgnoreCase) >= 0)
                    p.mass += 250f;
                else if (n.IndexOf("Fuselage_Tailpipe", StringComparison.OrdinalIgnoreCase) >= 0)
                    p.mass *= 0.4f;
                else if (n.IndexOf("Wing_Elevator", StringComparison.OrdinalIgnoreCase) >= 0)
                    p.mass *= 0.45f;
                else if (n.IndexOf("Aryx_MiG15_Tail", StringComparison.OrdinalIgnoreCase) >= 0)
                    p.mass *= 0.45f;
            }
        }

        private static void ApplyCg(Aircraft ac)
        {
            ApplyCg(ac, CgLocalZ);
        }

        private static void ApplyCg(Aircraft ac, float localZ)
        {
            if (ac == null || ac.rb == null)
                return;
            try
            {
                ac.rb.automaticCenterOfMass = false;
                Vector3 c = ac.rb.centerOfMass;
                if (c.z < localZ - 0.02f || c.z > localZ + 0.02f)
                    ac.rb.centerOfMass = new Vector3(0f, c.y, localZ);
            }
            catch
            {
                try
                {
                    Vector3 c = ac.rb.centerOfMass;
                    ac.rb.centerOfMass = new Vector3(0f, c.y, localZ);
                }
                catch { }
            }
        }

        private static float ReadFullMass(Aircraft ac)
        {
            float mass = 0f;
            try
            {
                if (ac.rb != null)
                    mass = ac.rb.mass;
            }
            catch { mass = 0f; }
            if (mass >= 1500f)
                return mass;
            float sum = 0f;
            UnitPart[] parts = null;
            try { parts = ac.GetComponentsInChildren<UnitPart>(true); }
            catch { parts = null; }
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == null)
                        continue;
                    if (parts[i].mass > 0f)
                        sum += parts[i].mass;
                }
            }
            FuelTank[] tanks = null;
            try { tanks = ac.GetComponentsInChildren<FuelTank>(true); }
            catch { tanks = null; }
            if (tanks != null)
            {
                for (int i = 0; i < tanks.Length; i++)
                {
                    if (tanks[i] == null)
                        continue;
                    if (tanks[i].fuelMass > 0f)
                        sum += tanks[i].fuelMass;
                }
            }
            if (sum < 400f)
                sum = 5055f;
            return sum;
        }

        private static void BuffAirframe(Aircraft aircraft)
        {
            if (aircraft == null)
                return;
            AeroPart[] aeros = aircraft.GetComponentsInChildren<AeroPart>(true);
            for (int i = 0; i < aeros.Length; i++)
                BuffAeroPart(aeros[i], StrengthMul);
            UnitPart[] parts = aircraft.GetComponentsInChildren<UnitPart>(true);
            for (int i = 0; i < parts.Length; i++)
            {
                UnitPart part = parts[i];
                if (part == null || part is AeroPart)
                    continue;
                BuffUnitPartDurability(part, StrengthMul);
            }
        }

        private static void BuffAeroPart(AeroPart part, float mul)
        {
            if (part == null)
                return;
            BuffUnitPartDurability(part, mul);
            if (AeroJoints == null)
                return;
            PartJoint[] joints = null;
            try { joints = AeroJoints.GetValue(part) as PartJoint[]; }
            catch { return; }
            if (joints == null)
                return;
            for (int i = 0; i < joints.Length; i++)
            {
                PartJoint pj = joints[i];
                if (pj == null)
                    continue;
                if (pj.breakForce > 0f && !float.IsInfinity(pj.breakForce))
                    pj.breakForce *= mul;
                if (pj.breakTorque > 0f && !float.IsInfinity(pj.breakTorque))
                    pj.breakTorque *= mul;
                Joint j = pj.joint;
                if (j == null)
                    continue;
                if (j.breakForce > 0f && !float.IsInfinity(j.breakForce))
                    j.breakForce *= mul;
                if (j.breakTorque > 0f && !float.IsInfinity(j.breakTorque))
                    j.breakTorque *= mul;
            }
        }

        private static void BuffUnitPartDurability(UnitPart part, float mul)
        {
            if (part == null)
                return;
            if (UnitStructural != null)
            {
                try
                {
                    float th = (float)UnitStructural.GetValue(part);
                    if (th > 0.01f)
                        UnitStructural.SetValue(part, th / mul);
                }
                catch { }
            }
            if (UnitImpact == null || ImpactThreshold == null)
                return;
            try
            {
                object impact = UnitImpact.GetValue(part);
                if (impact == null)
                    return;
                float th = (float)ImpactThreshold.GetValue(impact);
                if (th > 0.01f)
                    ImpactThreshold.SetValue(impact, th * mul);
                if (ImpactMultiplier != null)
                {
                    float m = (float)ImpactMultiplier.GetValue(impact);
                    if (m > 0.01f)
                        ImpactMultiplier.SetValue(impact, m / mul);
                }
            }
            catch { }
        }

        internal static bool TryDetonate(Aircraft ac, string reason)
        {
            return TryDetonate(ac, reason, true);
        }

        internal static bool TryDetonate(Aircraft ac, string reason, bool requireFuze)
        {
            if (ac == null || ac.transform == null)
                return false;
            if (!IsOurs(ac))
                return false;
            if (InEncyclopedia())
                return false;
            if (requireFuze)
            {
                if (!FuzeArmed(ac))
                    return false;
                if (!FuzeSettled(ac))
                    return false;
                if (!GearIsUp(ac))
                    return false;
            }
            int id = ac.GetInstanceID();
            if (Detonated.Contains(id))
                return false;
            bool sim = true;
            try
            {
                sim = !ac.networked || ac.IsServer || GameManager.IsLocalAircraft(ac);
            }
            catch
            {
                sim = true;
            }
            if (!sim)
                return false;
            Detonated.Add(id);
            Vector3 pos = ac.transform.position;
            CrashAsPilot(ac);
            DemolishAround(pos, ac);
            SpawnTenKt(pos, ac);
            if (Plugin.Log != null)
                Plugin.Log.LogInfo("MiG-15S 1kt (" + reason + ")");
            return true;
        }

        private static void CrashAsPilot(Aircraft ac)
        {
            if (ac == null)
                return;
            if (AircraftEjected != null)
            {
                try { AircraftEjected.SetValue(ac, false); }
                catch { }
            }
            Pilot[] pilots = null;
            try { pilots = ac.pilots; }
            catch { pilots = null; }
            if (pilots != null)
            {
                PersistentID none = default(PersistentID);
                for (int i = 0; i < pilots.Length; i++)
                {
                    Pilot p = pilots[i];
                    if (p == null)
                        continue;
                    try { p.ejected = false; }
                    catch { }
                    try { p.TakeDamage(0f, 0f, 1f, 0f, 1e9f, none); }
                    catch
                    {
                        try { p.ApplyDamage(0f, 0f, 0f, 1e9f); }
                        catch { }
                    }
                }
            }
            try
            {
                if (!ac.disabled)
                {
                    _skipDisableBoom = true;
                    ac.DisableUnit();
                    _skipDisableBoom = false;
                }
            }
            catch { _skipDisableBoom = false; }
            try { ac.CmdDisableUnit(); }
            catch { }
        }

        private static void DemolishAround(Vector3 pos, Aircraft self)
        {
            int n = 0;
            try { n = Physics.OverlapSphereNonAlloc(pos, DemolishRadius, DemolishHits); }
            catch
            {
                Collider[] hits = null;
                try { hits = Physics.OverlapSphere(pos, DemolishRadius); }
                catch { hits = null; }
                if (hits == null)
                    return;
                for (int i = 0; i < hits.Length; i++)
                    DemolishCollider(hits[i], self);
                return;
            }
            for (int i = 0; i < n && i < DemolishHits.Length; i++)
                DemolishCollider(DemolishHits[i], self);
        }

        private static void DemolishCollider(Collider col, Aircraft self)
        {
            if (col == null)
                return;
            Transform t = col.transform;
            if (t == null)
                return;
            if (self != null && t.IsChildOf(self.transform))
                return;
            MapBuilding mb = t.GetComponentInParent<MapBuilding>();
            if (mb != null)
                DemolishMapBuilding(mb);
            Unit u = t.GetComponentInParent<Unit>();
            if (u != null && (self == null || !object.ReferenceEquals(u, self)))
                DemolishUnit(u);
        }

        private static void DemolishMapBuilding(MapBuilding mb)
        {
            if (mb == null)
                return;
            PersistentID none = default(PersistentID);
            try { mb.TakeDamage(0f, 0f, 1f, 0f, 1e9f, none); }
            catch { }
            try { mb.ApplyDamage(0f, 0f, 0f, 1e9f); }
            catch { }
            if (MapBuildingSetField == null || MapBuildingIndexField == null)
                return;
            try
            {
                MapBuildingSet set = MapBuildingSetField.GetValue(mb) as MapBuildingSet;
                if (set == null)
                    return;
                int idx = (int)MapBuildingIndexField.GetValue(mb);
                set.DestroyBuilding(idx);
            }
            catch { }
        }

        private static void DemolishUnit(Unit u)
        {
            if (u == null)
                return;
            try
            {
                if (u.disabled)
                    return;
            }
            catch { }
            PersistentID none = default(PersistentID);
            UnitPart[] parts = null;
            try { parts = u.GetComponentsInChildren<UnitPart>(true); }
            catch { parts = null; }
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    UnitPart p = parts[i];
                    if (p == null)
                        continue;
                    try { p.TakeDamage(0f, 0f, 1f, 0f, 1e9f, none); }
                    catch { }
                    try { p.SpawnFragments(); }
                    catch { }
                    try { p.Detach(Vector3.zero, Vector3.zero); }
                    catch { }
                }
            }
            Aircraft other = u as Aircraft;
            if (other != null && other.pilots != null)
            {
                for (int i = 0; i < other.pilots.Length; i++)
                {
                    Pilot pl = other.pilots[i];
                    if (pl == null)
                        continue;
                    try { pl.TakeDamage(0f, 0f, 1f, 0f, 1e9f, none); }
                    catch
                    {
                        try { pl.ApplyDamage(0f, 0f, 0f, 1e9f); }
                        catch { }
                    }
                }
            }
            try { u.DisableUnit(); }
            catch { }
            try { u.CmdDisableUnit(); }
            catch { }
        }

        private static void SpawnTenKt(Vector3 pos, Aircraft ac)
        {
            PersistentID owner = default(PersistentID);
            try
            {
                if (ac != null)
                    owner = ac.persistentID;
            }
            catch { }
            Quaternion rot = Quaternion.identity;
            try
            {
                if (ac != null && ac.transform != null)
                    rot = ac.transform.rotation;
            }
            catch { }
            GameObject fxPrefab = ResolveNukeFx();
            if (fxPrefab == null)
                return;
            GameObject fx = null;
            try { fx = UnityEngine.Object.Instantiate(fxPrefab, pos, Quaternion.identity); }
            catch { fx = null; }
            PaintTenKt(fx, owner);
        }

        private static void PaintTenKt(GameObject root, PersistentID owner)
        {
            if (root == null)
                return;
            try
            {
                MushroomCloud[] clouds = root.GetComponentsInChildren<MushroomCloud>(true);
                if (clouds != null)
                {
                    for (int i = 0; i < clouds.Length; i++)
                    {
                        if (clouds[i] != null)
                            clouds[i].yield = YieldKt;
                    }
                }
            }
            catch { }
            if (ShockYieldField == null)
                return;
            try
            {
                Shockwave[] waves = root.GetComponentsInChildren<Shockwave>(true);
                if (waves == null)
                    return;
                for (int i = 0; i < waves.Length; i++)
                {
                    if (waves[i] == null)
                        continue;
                    waves[i].enabled = true;
                    ShockYieldField.SetValue(waves[i], YieldKt);
                    try { waves[i].SetOwner(owner, YieldKt); }
                    catch { }
                }
            }
            catch { }
        }

        private static GameObject ResolveNukeFx()
        {
            if (_nukeFx != null)
                return _nukeFx;
            GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
            if (all == null)
                return null;
            GameObject oneKt = null;
            GameObject twenty = null;
            for (int i = 0; i < all.Length; i++)
            {
                GameObject go = all[i];
                if (go == null)
                    continue;
                string n = go.name;
                bool isOne = n == "explosion_1kt" || n == "explosion_1kt(Clone)";
                bool isTwenty = n == "explosion_20kt" || n == "explosion_20kt(Clone)";
                if (!isOne && !isTwenty)
                    continue;
                if (!go.scene.IsValid())
                {
                    if (isOne)
                    {
                        _nukeFx = go;
                        return _nukeFx;
                    }
                    if (twenty == null)
                        twenty = go;
                }
                else if (isOne && oneKt == null)
                    oneKt = go;
                else if (isTwenty && twenty == null)
                    twenty = go;
            }
            if (oneKt != null)
                _nukeFx = oneKt;
            else
                _nukeFx = twenty;
            return _nukeFx;
        }

        private static GameObject PrefabFromLookup(string key)
        {
            if (string.IsNullOrEmpty(key) || Encyclopedia.Lookup == null)
                return null;
            UnitDefinition def;
            if (!Encyclopedia.Lookup.TryGetValue(key, out def) || def == null)
                return null;
            return def.unitPrefab;
        }

        internal static bool CollisionShouldBoom(Aircraft ac, Collision collision)
        {
            if (ac == null)
                return false;
            if (!FuzeArmed(ac))
                return false;
            if (!FuzeSettled(ac))
                return false;
            if (!GearIsUp(ac))
                return false;
            if (!WasFlying(ac) && !RecentFast(ac))
                return false;
            if (!RecentFast(ac))
                return false;
            if (IsSelfHit(ac, collision))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "Awake")]
    internal static class Patch_MiG15S_Awake
    {
        [HarmonyPostfix]
        private static void Postfix(Aircraft __instance)
        {
            Service.ApplyPerformance(__instance);
        }
    }

    [HarmonyPatch(typeof(Aircraft), "FixedUpdate")]
    internal static class Patch_MiG15S_FixedUpdate
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Aircraft __instance)
        {
            if (!Service.IsOurs(__instance))
                return;
            Service.RememberFlight(__instance);
            Service.Watchdog(__instance);
            Service.ApplyPerformance(__instance);
        }
    }

    [HarmonyPatch(typeof(Turbojet), "FixedUpdate")]
    internal static class Patch_MiG15S_JetFixedUpdate
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Turbojet __instance)
        {
            Service.ForceJetThrust(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Turbojet __instance)
        {
            Service.ForceJetThrust(__instance);
        }
    }

    [HarmonyPatch(typeof(Turbojet), "GetMaxThrust")]
    internal static class Patch_MiG15S_JetMaxThrust
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Turbojet __instance, ref float __result)
        {
            float t;
            if (Service.TryOursMaxThrust(__instance, out t))
                __result = t;
        }
    }

    [HarmonyPatch(typeof(JetNozzle), "Thrust")]
    internal static class Patch_MiG15S_NozzleThrust
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(JetNozzle __instance, ref bool allowAfterburner)
        {
            Service.ForceAllowAfterburner(__instance, ref allowAfterburner);
        }
    }

    [HarmonyPatch(typeof(JetNozzle), "GetIRMax")]
    internal static class Patch_MiG15S_IRMax
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(JetNozzle __instance, ref float __result)
        {
            Service.ClampNozzleIR(__instance, true, ref __result);
        }
    }

    [HarmonyPatch(typeof(JetNozzle), "GetIRMin")]
    internal static class Patch_MiG15S_IRMin
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(JetNozzle __instance, ref float __result)
        {
            Service.ClampNozzleIR(__instance, false, ref __result);
        }
    }

    [HarmonyPatch(typeof(Aircraft), "StartEjectionSequence")]
    internal static class Patch_MiG15S_NoEject
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Aircraft __instance)
        {
            if (!Service.BlockEjection(__instance))
                return true;
            Service.NotifyCannotEject(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "CmdStartEjectionSequence")]
    internal static class Patch_MiG15S_NoEjectCmd
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Aircraft __instance)
        {
            if (!Service.BlockEjection(__instance))
                return true;
            Service.NotifyCannotEject(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(Pilot), "CommandEjection")]
    internal static class Patch_MiG15S_NoPilotEject
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Pilot __instance)
        {
            Aircraft ac = null;
            try
            {
                if (__instance != null)
                    ac = __instance.aircraft;
            }
            catch { ac = null; }
            if (ac == null && __instance != null)
                ac = __instance.GetComponentInParent<Aircraft>();
            if (!Service.BlockEjection(ac))
                return true;
            Service.NotifyCannotEject(ac);
            return false;
        }
    }

    [HarmonyPatch(typeof(RadialMenuAction), "TriggerAction")]
    internal static class Patch_MiG15S_NoRadialEject
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(RadialMenuAction __instance, Aircraft aircraft)
        {
            if (__instance == null)
                return true;
            RadialMenuAction.ActionType t = RadialMenuAction.ActionType.Gear;
            try { t = __instance.GetActionType(); }
            catch { return true; }
            if (t != RadialMenuAction.ActionType.Eject)
                return true;
            if (!Service.BlockEjection(aircraft))
                return true;
            Service.NotifyCannotEject(aircraft);
            return false;
        }
    }

    [HarmonyPatch(typeof(Aircraft), "SpawnEjectingPilot")]
    internal static class Patch_MiG15S_NoEjectPilot
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Aircraft __instance)
        {
            return !Service.BlockEjection(__instance);
        }
    }

    [HarmonyPatch(typeof(Aircraft), "RpcEscapeCapsule")]
    internal static class Patch_MiG15S_NoCapsule
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(Aircraft __instance)
        {
            return !Service.BlockEjection(__instance);
        }
    }

    [HarmonyPatch(typeof(Encyclopedia), "AfterLoad", new Type[] { })]
    internal static class Patch_MiG15S_Encyclopedia
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            Service.StampAllDefs();
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "SpawnUnit")]
    internal static class Patch_MiG15S_EncSpawnUnit
    {
        [HarmonyPrefix]
        private static void Prefix(UnitDefinition definition)
        {
            Service.ApplyEncyclopedia(definition as AircraftDefinition);
        }

        [HarmonyPostfix]
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            Service.ApplyEncyclopedia(definition as AircraftDefinition);
            Unit u = null;
            try { u = __instance.GetSpawnedUnit(); }
            catch { u = null; }
            Aircraft ac = u as Aircraft;
            if (ac == null && u != null)
                ac = u.GetComponent<Aircraft>();
            Service.PoseEncyclopediaAircraft(ac);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "SpawnAircraft")]
    internal static class Patch_MiG15S_EncSpawnAircraft
    {
        [HarmonyPrefix]
        private static void Prefix(UnitDefinition definition)
        {
            Service.ApplyEncyclopedia(definition as AircraftDefinition);
        }

        [HarmonyPostfix]
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            Service.ApplyEncyclopedia(definition as AircraftDefinition);
            Unit u = null;
            try { u = __instance.GetSpawnedUnit(); }
            catch { u = null; }
            Aircraft ac = u as Aircraft;
            if (ac == null && u != null)
                ac = u.GetComponent<Aircraft>();
            Service.PoseEncyclopediaAircraft(ac);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "Update")]
    internal static class Patch_MiG15S_EncUpdate
    {
        [HarmonyPostfix]
        private static void Postfix(EncyclopediaBrowser __instance)
        {
            Unit u = null;
            try { u = __instance.GetSpawnedUnit(); }
            catch { u = null; }
            Aircraft ac = u as Aircraft;
            if (ac == null && u != null)
                ac = u.GetComponent<Aircraft>();
            if (ac != null)
                Service.PoseEncyclopediaAircraft(ac);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "DisplayUnitInfo")]
    internal static class Patch_MiG15S_EncInfo
    {
        [HarmonyPrefix]
        private static void Prefix(UnitDefinition definition)
        {
            Service.ApplyEncyclopedia(definition as AircraftDefinition);
        }
    }

    [HarmonyPatch(typeof(AeroPart), "OnCollisionEnter")]
    internal static class Patch_MiG15S_AeroCollision
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AeroPart __instance, Collision collision)
        {
            if (__instance == null)
                return;
            Aircraft ac = __instance.parentUnit as Aircraft;
            if (ac == null)
                ac = __instance.GetComponentInParent<Aircraft>();
            Service.NoteHit(ac, collision);
        }
    }

    [HarmonyPatch(typeof(AeroPart), "OnCollisionStay")]
    internal static class Patch_MiG15S_AeroStay
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(AeroPart __instance, Collision collision)
        {
            if (__instance == null)
                return;
            Aircraft ac = __instance.parentUnit as Aircraft;
            if (ac == null)
                ac = __instance.GetComponentInParent<Aircraft>();
            Service.NoteHit(ac, collision);
        }
    }

    [HarmonyPatch(typeof(FuelTank), "OnCollisionEnter")]
    internal static class Patch_MiG15S_FuelCollision
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(FuelTank __instance, Collision collision)
        {
            if (__instance == null)
                return;
            Aircraft ac = __instance.GetComponentInParent<Aircraft>();
            Service.NoteHit(ac, collision);
        }
    }

    [HarmonyPatch(typeof(Unit), "DisableUnit")]
    internal static class Patch_MiG15S_DisableUnit
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Unit __instance)
        {
            if (Service.SkipDisableBoom)
                return;
            Aircraft ac = __instance as Aircraft;
            if (ac == null)
                return;
            Service.RememberFlight(ac);
            if (!Service.IsOurs(ac) || !Service.FuzeArmed(ac))
                return;
            if (!Service.CollisionShouldBoom(ac, null))
                return;
            Service.TryDetonate(ac, "disabled");
        }
    }
}
