using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace KH38MT
{
    /// <summary>
    /// KH38MT: AAM-36 single-rail clone, AGM-68 warhead, Mach 8, third pylon only.
    /// </summary>
    internal static class Kh38MtWeapon
    {
        internal const string PackKey = "KH38MT";
        internal const string DisplayName = "KH38MT";
        internal const string EncyclopediaText =
            "KH38MT is a Kh-38MT-class missile on an AAM-36 single rail. "
            + "AGM-68-class warhead, Mach 8 dash, one round, third pylon only.";

        internal const float SpeedMach = 8f;
        internal const float MachToMs = 340.3f;
        internal const float RangeM = 100000f;
        internal const float GLimit = 75f;

        internal const float Agm68BlastDamage = 120f;
        internal const float Agm68PierceDamage = 700f;
        internal const float Agm68ArmorTier = 7.9f;
        internal const float Agm68AntiSurface = 0.814f;
        internal const float Agm68BlastYield = 130f;
        internal const float Agm68MissilePierce = 2500f;
        internal const float Agm68MountMass = 250f;

        private const int ThirdHardpointIndex = 2;

        private static bool _injected;
        internal static bool IsInjected { get { return _injected; } }

        private static readonly HashSet<int> InfoIds = new HashSet<int>();
        private static readonly HashSet<string> CreatedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<WeaponMount> MountClones = new List<WeaponMount>();
        private static readonly Dictionary<string, WeaponInfo> InfoByKey =
            new Dictionary<string, WeaponInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<HardpointSet> ThirdSets = new HashSet<HardpointSet>();
        private static readonly HashSet<int> ShipWmIds = new HashSet<int>();
        private static readonly HashSet<int> AirWmIds = new HashSet<int>();

        private static MissileDefinition _encyclopediaDef;
        private static float _nextHardpointInject;
        private static float _nextMaintAt;
        private static float _nextMissingLog;
        private static int _hardpointIdlePasses;

        private static readonly FieldInfo MissileInfoField = AccessTools.Field(typeof(Missile), "info");
        private static readonly FieldInfo WeaponStationField = AccessTools.Field(typeof(Weapon), "weaponStation");
        private static readonly FieldInfo BlastYieldField = AccessTools.Field(typeof(Missile), "blastYield");
        private static readonly FieldInfo PierceField = AccessTools.Field(typeof(Missile), "pierceDamage");
        private static readonly FieldInfo MotorsField = AccessTools.Field(typeof(Missile), "motors");
        private static readonly FieldInfo GLimitField = AccessTools.Field(typeof(Missile), "gLimit");
        private static readonly FieldInfo TorqueField = AccessTools.Field(typeof(Missile), "torque");
        private static readonly FieldInfo MaxTurnRateField = AccessTools.Field(typeof(Missile), "maxTurnRate");
        private static readonly FieldInfo FinAreaField = AccessTools.Field(typeof(Missile), "finArea");
        private static readonly FieldInfo ArhRadarField = AccessTools.Field(typeof(ARHSeeker), "radarParameters");
        private static readonly FieldInfo SarhRadarField = AccessTools.Field(typeof(SARHSeeker), "radarParams");

        private sealed class PendingFire
        {
            public Unit owner;
            public float time;
            public WeaponInfo info;
        }

        private static readonly List<PendingFire> PendingFires = new List<PendingFire>();

        internal static float SpeedMs()
        {
            return SpeedMach * MachToMs;
        }

        internal static bool HasUsableClones()
        {
            int n = 0;
            for (int i = 0; i < MountClones.Count; i++)
            {
                WeaponMount m = MountClones[i];
                if (m != null && m.prefab != null && m.info != null)
                    n++;
            }
            return n > 0;
        }

        internal static bool IsKey(string key)
        {
            return !string.IsNullOrEmpty(key)
                && key.StartsWith(PackKey, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsInfo(WeaponInfo info)
        {
            if (info == null)
                return false;
            if (InfoIds.Contains(info.GetInstanceID()))
                return true;
            string n = ((info.shortName != null ? info.shortName : string.Empty) + " "
                + (info.weaponName != null ? info.weaponName : string.Empty));
            return n.IndexOf("KH38MT", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsMount(WeaponMount mount)
        {
            if (mount == null)
                return false;
            if (IsKey(mount.jsonKey))
                return true;
            return IsInfo(mount.info);
        }

        internal static bool IsMissile(Missile missile)
        {
            if (missile == null)
                return false;
            try
            {
                if (missile.GetComponent<Kh38MtMark>() != null)
                    return true;
            }
            catch { }
            return IsInfo(GetMissileInfo(missile));
        }

        private static WeaponInfo GetMissileInfo(Missile missile)
        {
            if (missile == null || MissileInfoField == null)
                return null;
            try { return MissileInfoField.GetValue(missile) as WeaponInfo; }
            catch { return null; }
        }

        internal static void Ensure()
        {
            for (int i = MountClones.Count - 1; i >= 0; i--)
            {
                if (MountClones[i] == null)
                    MountClones.RemoveAt(i);
            }
            if (_injected && !HasUsableClones())
            {
                _injected = false;
                CreatedKeys.Clear();
                InfoByKey.Clear();
                InfoIds.Clear();
                MountClones.Clear();
                ThirdSets.Clear();
                _encyclopediaDef = null;
            }

            if (!_injected)
            {
                WeaponMount[] all = Resources.FindObjectsOfTypeAll<WeaponMount>();
                if (all == null || all.Length == 0)
                {
                    if (Time.unscaledTime >= _nextMissingLog)
                    {
                        _nextMissingLog = Time.unscaledTime + 8f;
                        if (Plugin.Log != null)
                            Plugin.Log.LogWarning("KH38MT: waiting for WeaponMount assets...");
                    }
                    return;
                }

                Encyclopedia enc = Plugin.GetEncyclopedia();
                EnsureEncyclopediaDef(enc);
                int added = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    WeaponMount src = all[i];
                    if (src == null || src.info == null || !src.info.missile)
                        continue;
                    if (IsKey(src.jsonKey))
                        continue;
                    if (!IsAam36SingleDonor(src))
                        continue;
                    if (CreatedKeys.Contains(PackKey))
                        continue;
                    if (CreateMountVariant(src, enc, PackKey, ref added))
                        CreatedKeys.Add(PackKey);
                }

                if (added > 0 || MountClones.Count > 0)
                {
                    _injected = true;
                    RestoreAllMountIdentities();
                    if (Plugin.Log != null)
                        Plugin.Log.LogInfo("KH38MT: injected " + added + " mounts (AGM-68 warhead, Mach 8)");
                }
                else if (Time.unscaledTime >= _nextMissingLog)
                {
                    _nextMissingLog = Time.unscaledTime + 12f;
                    if (Plugin.Log != null)
                        Plugin.Log.LogWarning("KH38MT: no AAM-36 single donor mounts yet");
                }
            }

            if (!_injected)
                return;

            if (Time.unscaledTime >= _nextMaintAt)
            {
                _nextMaintAt = Time.unscaledTime + 90f;
                RegisterWithEncyclopedia(Plugin.GetEncyclopedia());
            }
            if (Time.unscaledTime >= _nextHardpointInject)
                InjectIntoAircraftHardpoints();
        }

        internal static void InjectIntoAircraftHardpoints()
        {
            if (!_injected || MountClones.Count == 0)
                return;
            _nextHardpointInject = Time.unscaledTime + (_hardpointIdlePasses >= 2 ? 20f : 5f);
            WeaponManager[] managers = Resources.FindObjectsOfTypeAll<WeaponManager>();
            if (managers == null)
                return;
            int added = 0;
            for (int i = 0; i < managers.Length; i++)
                added += InjectIntoWeaponManager(managers[i]);
            if (added <= 0)
                _hardpointIdlePasses++;
            else
                _hardpointIdlePasses = 0;
        }

        internal static int InjectIntoWeaponManager(WeaponManager wm)
        {
            if (!_injected || MountClones.Count == 0 || wm == null || wm.hardpointSets == null)
                return 0;
            if (IsShipWeaponManager(wm))
                return 0;

            int added = 0;
            for (int h = 0; h < wm.hardpointSets.Length; h++)
            {
                HardpointSet hs = wm.hardpointSets[h];
                if (hs == null)
                    continue;
                if (hs.weaponOptions == null)
                    hs.weaponOptions = new List<WeaponMount>();
                if (h == ThirdHardpointIndex)
                {
                    ThirdSets.Add(hs);
                    if (Plugin.IsNavalHardpoint(hs) || !HardpointAcceptsMissiles(hs))
                    {
                        StripFromHardpoint(hs);
                        continue;
                    }
                    added += AddToHardpoint(hs);
                }
                else
                    StripFromHardpoint(hs);
            }
            return added;
        }

        private static bool IsShipWeaponManager(WeaponManager wm)
        {
            if (wm == null)
                return false;
            int id = wm.GetInstanceID();
            if (ShipWmIds.Contains(id))
                return true;
            if (AirWmIds.Contains(id))
                return false;
            try
            {
                if (wm.GetComponentInParent<Aircraft>() != null)
                {
                    AirWmIds.Add(id);
                    return false;
                }
            }
            catch { }
            ShipWmIds.Add(id);
            return true;
        }

        private static bool HardpointAcceptsMissiles(HardpointSet hs)
        {
            if (hs == null || hs.weaponOptions == null)
                return false;
            for (int i = 0; i < hs.weaponOptions.Count; i++)
            {
                WeaponMount m = hs.weaponOptions[i];
                if (m != null && m.info != null && m.info.missile)
                    return true;
            }
            return false;
        }

        internal static bool IsThirdHardpointSet(HardpointSet hs)
        {
            if (hs == null)
                return false;
            if (ThirdSets.Contains(hs))
                return true;
            WeaponManager[] managers = Resources.FindObjectsOfTypeAll<WeaponManager>();
            if (managers == null)
                return false;
            for (int i = 0; i < managers.Length; i++)
            {
                WeaponManager wm = managers[i];
                if (wm == null || wm.hardpointSets == null || wm.hardpointSets.Length <= ThirdHardpointIndex)
                    continue;
                HardpointSet third = wm.hardpointSets[ThirdHardpointIndex];
                if (third != null)
                    ThirdSets.Add(third);
                if (object.ReferenceEquals(third, hs))
                    return true;
            }
            return false;
        }

        private static int StripFromHardpoint(HardpointSet hs)
        {
            if (hs == null || hs.weaponOptions == null)
                return 0;
            int n = 0;
            for (int i = hs.weaponOptions.Count - 1; i >= 0; i--)
            {
                if (!IsMount(hs.weaponOptions[i]))
                    continue;
                hs.weaponOptions.RemoveAt(i);
                n++;
            }
            return n;
        }

        private static int AddToHardpoint(HardpointSet hs)
        {
            WeaponMount clone = FindClone();
            if (clone == null || clone.prefab == null)
                return 0;
            if (hs.weaponOptions.Contains(clone))
                return 0;
            hs.weaponOptions.Add(clone);
            return 1;
        }

        private static WeaponMount FindClone()
        {
            for (int i = 0; i < MountClones.Count; i++)
            {
                WeaponMount m = MountClones[i];
                if (m != null && m.prefab != null)
                    return m;
            }
            return null;
        }

        internal static void FilterAvailable(HardpointSet hs, List<WeaponMount> list, HashSet<WeaponMount> have)
        {
            if (list == null)
                return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                WeaponMount existing = list[i];
                if (!IsMount(existing))
                    continue;
                list.RemoveAt(i);
                if (have != null)
                    have.Remove(existing);
            }

            if (Plugin.IsNavalHardpoint(hs) || !IsThirdHardpointSet(hs))
                return;
            WeaponMount clone = FindClone();
            if (clone == null || clone.prefab == null)
                return;
            if (have != null && !have.Add(clone))
                return;
            if (list.Contains(clone))
                return;
            list.Add(clone);
        }

        internal static void RestoreMountIdentity(WeaponMount mount)
        {
            if (mount == null || string.IsNullOrEmpty(mount.jsonKey))
                return;
            WeaponInfo info = null;
            if (!InfoByKey.TryGetValue(mount.jsonKey, out info) || info == null)
                return;
            info.weaponName = DisplayName;
            info.shortName = DisplayName;
            ApplyAgm68Warhead(info);
            ApplyFireEnvelope(info);
            Kh38MtIcon.ApplyToInfo(info);
            mount.info = info;
            mount.mountName = DisplayName;
            mount.ammo = 1;
            mount.mass = Agm68MountMass;
        }

        internal static void RestoreAllMountIdentities()
        {
            for (int i = 0; i < MountClones.Count; i++)
                RestoreMountIdentity(MountClones[i]);
        }

        private static bool CreateMountVariant(WeaponMount src, Encyclopedia enc, string key, ref int added)
        {
            WeaponMount clone = UnityEngine.Object.Instantiate(src);
            clone.name = "KH38MT_Mount";
            clone.jsonKey = key;
            clone.hideFlags = HideFlags.DontUnloadUnusedAsset;
            clone.ammo = 1;
            clone.mass = Agm68MountMass;

            WeaponInfo infoClone = UnityEngine.Object.Instantiate(src.info);
            infoClone.name = "WeaponInfo_KH38MT";
            infoClone.hideFlags = HideFlags.DontUnloadUnusedAsset;
            infoClone.weaponName = DisplayName;
            infoClone.shortName = DisplayName;
            infoClone.description = EncyclopediaText;
            infoClone.nuclear = false;
            infoClone.strategic = false;
            infoClone.rearmShip = false;
            infoClone.missile = true;
            ApplyAgm68Warhead(infoClone);
            ApplyFireEnvelope(infoClone);
            Kh38MtIcon.ApplyToInfo(infoClone);

            clone.prefab = src.prefab;
            if (src.info != null && src.info.weaponPrefab != null)
                infoClone.weaponPrefab = src.info.weaponPrefab;
            clone.info = infoClone;
            clone.mountName = DisplayName;

            InfoIds.Add(infoClone.GetInstanceID());
            InfoByKey[key] = infoClone;
            MountClones.Add(clone);

            if (enc != null && enc.weaponMounts != null && !enc.weaponMounts.Contains(clone))
                enc.weaponMounts.Add(clone);
            if (Encyclopedia.WeaponLookup != null && !Encyclopedia.WeaponLookup.ContainsKey(key))
                Encyclopedia.WeaponLookup[key] = clone;
            try
            {
                if (enc != null && enc.IndexLookup != null && !enc.IndexLookup.Contains(clone))
                {
                    enc.IndexLookup.Add(clone);
                    INetworkDefinition nd = clone;
                    nd.LookupIndex = enc.IndexLookup.Count - 1;
                }
            }
            catch { }

            added++;
            return true;
        }

        internal static void ApplyAgm68Warhead(WeaponInfo info)
        {
            if (info == null)
                return;
            WeaponInfo src = FindAgm68Info();
            if (src != null)
            {
                info.blastDamage = src.blastDamage;
                info.pierceDamage = src.pierceDamage;
                info.armorTierEffectiveness = src.armorTierEffectiveness;
                RoleIdentity e = info.effectiveness;
                e.antiSurface = src.effectiveness.antiSurface;
                info.effectiveness = e;
            }
            else
            {
                info.blastDamage = Agm68BlastDamage;
                info.pierceDamage = Agm68PierceDamage;
                info.armorTierEffectiveness = Agm68ArmorTier;
                RoleIdentity e = info.effectiveness;
                e.antiSurface = Agm68AntiSurface;
                info.effectiveness = e;
            }
        }

        internal static void ApplyAgm68Warhead(Missile missile)
        {
            if (missile == null)
                return;
            float yield = Agm68BlastYield;
            float pierce = Agm68MissilePierce;
            Missile donor = FindAgm68MissilePrefab();
            if (donor != null)
            {
                try
                {
                    if (BlastYieldField != null)
                        yield = Convert.ToSingle(BlastYieldField.GetValue(donor));
                }
                catch { }
                try
                {
                    if (PierceField != null)
                        pierce = Convert.ToSingle(PierceField.GetValue(donor));
                }
                catch { }
            }
            try
            {
                if (BlastYieldField != null)
                    BlastYieldField.SetValue(missile, yield);
            }
            catch { }
            try
            {
                if (PierceField != null)
                    PierceField.SetValue(missile, pierce);
            }
            catch { }
        }

        private static WeaponInfo FindAgm68Info()
        {
            WeaponInfo[] all = Resources.FindObjectsOfTypeAll<WeaponInfo>();
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                WeaponInfo info = all[i];
                if (info == null)
                    continue;
                string n = ((info.shortName != null ? info.shortName : string.Empty) + " "
                    + (info.weaponName != null ? info.weaponName : string.Empty) + " "
                    + (info.name != null ? info.name : string.Empty));
                if (n.IndexOf("AGM-68", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("AGM_heavy", StringComparison.OrdinalIgnoreCase) >= 0)
                    return info;
            }
            return null;
        }

        private static Missile FindAgm68MissilePrefab()
        {
            Missile[] all = Resources.FindObjectsOfTypeAll<Missile>();
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                Missile m = all[i];
                if (m == null)
                    continue;
                string n = m.name != null ? m.name : string.Empty;
                if (n.IndexOf("AGM_heavy", StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("AGM-68", StringComparison.OrdinalIgnoreCase) >= 0)
                    return m;
            }
            return null;
        }

        private static void ApplyFireEnvelope(WeaponInfo info)
        {
            if (info == null)
                return;
            try
            {
                TargetRequirements tr = info.targetRequirements;
                tr.maxRange = RangeM;
                info.targetRequirements = tr;
            }
            catch { }
        }

        private static void EnsureEncyclopediaDef(Encyclopedia enc)
        {
            if (enc == null || enc.missiles == null || _encyclopediaDef != null)
                return;
            MissileDefinition src = FindAam36Definition(enc);
            if (src == null)
                return;
            MissileDefinition clone = UnityEngine.Object.Instantiate(src);
            clone.name = "MissileDef_KH38MT";
            clone.jsonKey = PackKey;
            clone.code = DisplayName;
            clone.unitName = DisplayName;
            clone.description = EncyclopediaText;
            clone.dontAutomaticallyAddToEncyclopedia = false;
            Kh38MtIcon.ApplyToDefinition(clone);
            if (!enc.missiles.Contains(clone))
                enc.missiles.Add(clone);
            if (Encyclopedia.Lookup != null && !Encyclopedia.Lookup.ContainsKey(PackKey))
                Encyclopedia.Lookup[PackKey] = clone;
            _encyclopediaDef = clone;
        }

        private static void RegisterWithEncyclopedia(Encyclopedia enc)
        {
            if (enc == null || !Plugin.IsEncyclopediaPopulated(enc))
                return;
            EnsureEncyclopediaDef(enc);
            for (int i = 0; i < MountClones.Count; i++)
            {
                WeaponMount clone = MountClones[i];
                if (clone == null || string.IsNullOrEmpty(clone.jsonKey))
                    continue;
                if (enc.weaponMounts != null && !enc.weaponMounts.Contains(clone))
                    enc.weaponMounts.Add(clone);
                if (Encyclopedia.WeaponLookup != null && !Encyclopedia.WeaponLookup.ContainsKey(clone.jsonKey))
                    Encyclopedia.WeaponLookup[clone.jsonKey] = clone;
                RestoreMountIdentity(clone);
            }
        }

        private static MissileDefinition FindAam36Definition(Encyclopedia enc)
        {
            if (enc != null && enc.missiles != null)
            {
                for (int i = 0; i < enc.missiles.Count; i++)
                {
                    MissileDefinition d = enc.missiles[i];
                    if (d != null && IsAam36Definition(d.jsonKey, d.unitName, d.name))
                        return d;
                }
            }
            MissileDefinition[] all = Resources.FindObjectsOfTypeAll<MissileDefinition>();
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                MissileDefinition d = all[i];
                if (d != null && IsAam36Definition(d.jsonKey, d.unitName, d.name))
                    return d;
            }
            return null;
        }

        private static bool IsAam36Definition(string jsonKey, string unitName, string name)
        {
            if (!string.IsNullOrEmpty(jsonKey)
                && jsonKey.StartsWith("AAM4", StringComparison.OrdinalIgnoreCase)
                && !IsKey(jsonKey))
                return true;
            string blob = ((unitName != null ? unitName : string.Empty) + " "
                + (name != null ? name : string.Empty) + " "
                + (jsonKey != null ? jsonKey : string.Empty));
            return blob.IndexOf("AAM-36", StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("Scimitar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAam36SingleDonor(WeaponMount m)
        {
            if (m == null || m.ammo != 1)
                return false;
            string k = m.jsonKey != null ? m.jsonKey : string.Empty;
            if (k.IndexOf("internal", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (k.IndexOf("double", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (k.IndexOf("triple", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (k.IndexOf("x8", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (!string.IsNullOrEmpty(k) && k.StartsWith("AAM4", StringComparison.OrdinalIgnoreCase)
                && !IsKey(k))
                return true;
            string sn = m.info != null && m.info.shortName != null ? m.info.shortName : string.Empty;
            string wn = m.info != null && m.info.weaponName != null ? m.info.weaponName : string.Empty;
            string blob = sn + " " + wn;
            return blob.IndexOf("AAM-36", StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("Scimitar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsAam36DonorKey(string jsonKey, string shortName, string weaponName)
        {
            if (!string.IsNullOrEmpty(jsonKey)
                && jsonKey.StartsWith("AAM4", StringComparison.OrdinalIgnoreCase)
                && !IsKey(jsonKey))
                return true;
            string blob = ((shortName != null ? shortName : string.Empty) + " "
                + (weaponName != null ? weaponName : string.Empty));
            return blob.IndexOf("AAM-36", StringComparison.OrdinalIgnoreCase) >= 0
                || blob.IndexOf("Scimitar", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static void SyncFromMount(Weapon weapon, WeaponMount mount)
        {
            if (weapon == null || mount == null || weapon is Gun || !IsMount(mount))
                return;
            RestoreMountIdentity(mount);
            if (mount.info == null)
                return;
            weapon.info = mount.info;
        }

        internal static void NoteFire(Weapon weapon)
        {
            if (weapon == null)
                return;
            WeaponMount mount = Plugin.GetWeaponMount(weapon);
            WeaponInfo stationInfo = null;
            try
            {
                if (WeaponStationField != null)
                {
                    WeaponStation st = WeaponStationField.GetValue(weapon) as WeaponStation;
                    if (st != null)
                        stationInfo = st.WeaponInfo;
                }
            }
            catch { }
            if (!IsMount(mount) && !IsInfo(weapon.info) && !IsInfo(stationInfo))
                return;
            PendingFire pf = new PendingFire();
            pf.owner = weapon.attachedUnit;
            pf.time = Time.time;
            pf.info = weapon.info;
            PendingFires.Add(pf);
        }

        internal static void OnSpawned(Missile missile, Unit spawnOwner)
        {
            if (missile == null)
                return;
            WeaponInfo pendingInfo;
            bool pending = ConsumePending(missile, spawnOwner, out pendingInfo);
            bool ours = pending || IsMissile(missile) || IsInfo(GetMissileInfo(missile));
            if (!ours)
                return;
            ApplySpawnIdentity(missile, pendingInfo);
        }

        private static bool ConsumePending(Missile missile, Unit spawnOwner, out WeaponInfo info)
        {
            info = null;
            float now = Time.time;
            for (int i = PendingFires.Count - 1; i >= 0; i--)
            {
                PendingFire pf = PendingFires[i];
                if (pf == null || now - pf.time > 8f)
                    PendingFires.RemoveAt(i);
            }
            if (PendingFires.Count <= 0)
                return false;
            Unit owner = spawnOwner != null ? spawnOwner : (missile != null ? missile.owner : null);
            int pick = -1;
            for (int i = 0; i < PendingFires.Count; i++)
            {
                PendingFire pf = PendingFires[i];
                if (pf != null && OwnerMatches(pf.owner, owner))
                {
                    pick = i;
                    break;
                }
            }
            if (pick < 0)
                return false;
            PendingFire taken = PendingFires[pick];
            PendingFires.RemoveAt(pick);
            info = taken != null ? taken.info : null;
            return true;
        }

        private static bool OwnerMatches(Unit pendingOwner, Unit spawnOwner)
        {
            if (pendingOwner == null || spawnOwner == null)
                return false;
            if (object.ReferenceEquals(spawnOwner, pendingOwner))
                return true;
            try
            {
                if (spawnOwner.transform != null && pendingOwner.transform != null
                    && spawnOwner.transform.root == pendingOwner.transform.root)
                    return true;
            }
            catch { }
            Aircraft a = pendingOwner as Aircraft;
            Aircraft b = spawnOwner as Aircraft;
            try
            {
                if (a == null)
                    a = pendingOwner.GetComponentInParent<Aircraft>();
                if (b == null)
                    b = spawnOwner.GetComponentInParent<Aircraft>();
            }
            catch { }
            return a != null && b != null && object.ReferenceEquals(a, b);
        }

        private static void ApplySpawnIdentity(Missile missile, WeaponInfo sourceInfo)
        {
            WeaponInfo info = sourceInfo;
            if (info == null || !IsInfo(info))
                info = GetMissileInfo(missile);
            try
            {
                missile.NetworkunitName = DisplayName;
                missile.name = DisplayName;
            }
            catch { }
            if (info != null && MissileInfoField != null)
            {
                try { MissileInfoField.SetValue(missile, info); }
                catch { }
            }
            if (info != null)
            {
                ApplyAgm68Warhead(info);
                ApplyFireEnvelope(info);
            }

            Kh38MtMark mark = missile.GetComponent<Kh38MtMark>();
            if (mark == null)
                mark = Plugin.TryAddBehaviour<Kh38MtMark>(missile.gameObject);
            if (mark != null && !mark.Boosted)
            {
                ApplyAgm68Warhead(missile);
                ApplyKinematics(missile);
                ApplySeekerRange(missile);
                mark.Boosted = true;
            }
            else if (mark == null)
            {
                ApplyAgm68Warhead(missile);
                ApplyKinematics(missile);
                ApplySeekerRange(missile);
            }

            Kh38MtVisual.ApplyToMissile(missile);
        }

        private static void ApplyKinematics(Missile missile)
        {
            if (missile == null || MotorsField == null)
                return;
            float wantSpeed = SpeedMs();
            float minBurn = RangeM / wantSpeed;
            if (minBurn < 20f)
                minBurn = 20f;
            float currentTop = 0f;
            try { currentTop = missile.GetTopSpeed(8000f, 8000f); }
            catch { }
            float thrustMul;
            if (currentTop < 50f)
                thrustMul = 8f;
            else if (currentTop >= wantSpeed)
                thrustMul = 1.25f;
            else
            {
                float ratio = wantSpeed / currentTop;
                thrustMul = ratio * ratio * 1.35f;
                if (thrustMul < 1.25f)
                    thrustMul = 1.25f;
                if (thrustMul > 24f)
                    thrustMul = 24f;
            }

            try
            {
                if (GLimitField != null)
                    GLimitField.SetValue(missile, GLimit);
            }
            catch { }

            try
            {
                Array motors = MotorsField.GetValue(missile) as Array;
                if (motors != null)
                {
                    for (int i = 0; i < motors.Length; i++)
                    {
                        object motor = motors.GetValue(i);
                        if (motor == null)
                            continue;
                        Type mt = motor.GetType();
                        FieldInfo fTop = mt.GetField("topSpeed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        FieldInfo fThrust = mt.GetField("thrust", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        FieldInfo fBurn = mt.GetField("burnTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (fTop != null)
                            fTop.SetValue(motor, wantSpeed);
                        if (fThrust != null)
                        {
                            float thrust = Convert.ToSingle(fThrust.GetValue(motor));
                            if (thrust > 0.01f)
                                fThrust.SetValue(motor, thrust * thrustMul);
                        }
                        if (fBurn != null)
                        {
                            float burn = Convert.ToSingle(fBurn.GetValue(motor));
                            if (burn < minBurn)
                                fBurn.SetValue(motor, minBurn);
                        }
                    }
                }
            }
            catch { }
        }

        private static void ApplySeekerRange(Missile missile)
        {
            if (missile == null)
                return;
            try
            {
                ARHSeeker arh = missile.GetComponent<ARHSeeker>();
                if (arh != null)
                    StampRadarMaxRange(ArhRadarField, arh);
            }
            catch { }
            try
            {
                SARHSeeker sarh = missile.GetComponent<SARHSeeker>();
                if (sarh != null)
                    StampRadarMaxRange(SarhRadarField, sarh);
            }
            catch { }
        }

        private static void StampRadarMaxRange(FieldInfo field, object seeker)
        {
            if (field == null || seeker == null)
                return;
            object raw = field.GetValue(seeker);
            if (raw == null || !(raw is RadarParams))
                return;
            RadarParams rp = (RadarParams)raw;
            if (rp.maxRange >= RangeM)
                return;
            rp.maxRange = RangeM;
            field.SetValue(seeker, rp);
        }
    }

    public sealed class Kh38MtMark : MonoBehaviour
    {
        public bool Boosted;
    }
}
