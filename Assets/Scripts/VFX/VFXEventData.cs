using UnityEngine;
using System.Collections.Generic;

namespace AGNIDAWN.VFX
{
    /// <summary>
    /// ScriptableObject that maps Astra types, enemy archetypes, and boss IDs
    /// to ObjectPool keys for their VFX prefabs.
    ///
    /// VFXManager reads this SO at runtime to know which pool key to request
    /// for each event. Designers configure the keys in the Inspector; programmers
    /// reference pool keys as strings (format: "VFX_<Tag>").
    ///
    /// Linear: FAI-16 (Phase 11)
    /// </summary>
    [CreateAssetMenu(menuName = "AGNIDAWN/VFX/VFX Event Data", fileName = "VFXEventData")]
    public class VFXEventData : ScriptableObject
    {
        // ── Astra Impact VFX ──────────────────────────────────────────────────────

        [System.Serializable]
        public struct AstraVFXMapping
        {
            [Tooltip("Astra script name (matches AstraData.astraId)")]
            public string astraId;
            [Tooltip("ObjectPool key for impact particle — format: VFX_<Name>")]
            public string impactPoolKey;
            [Tooltip("ObjectPool key for muzzle/cast particle (optional)")]
            public string castPoolKey;
            [Tooltip("ObjectPool key for a lingering trail (optional)")]
            public string trailPoolKey;
        }

        [Header("Astra Impact Mappings")]
        [SerializeField] private AstraVFXMapping[] astraMappings = new AstraVFXMapping[]
        {
            new AstraVFXMapping { astraId = "Trishul",          impactPoolKey = "VFX_TrishulImpact",     castPoolKey = "VFX_TrishulCast",     trailPoolKey = "" },
            new AstraVFXMapping { astraId = "Gandiv",           impactPoolKey = "VFX_GandivImpact",      castPoolKey = "VFX_GandivCast",      trailPoolKey = "" },
            new AstraVFXMapping { astraId = "SudarshanaChakra", impactPoolKey = "VFX_ChakraImpact",      castPoolKey = "VFX_ChakraCast",      trailPoolKey = "VFX_ChakraTrail" },
            new AstraVFXMapping { astraId = "Brahmastra",       impactPoolKey = "VFX_BrahmastraBlast",   castPoolKey = "VFX_BrahmaMandala",   trailPoolKey = "" },
            new AstraVFXMapping { astraId = "Pashupatastra",    impactPoolKey = "VFX_PashupataBlast",    castPoolKey = "VFX_PashupataMandala",trailPoolKey = "" },
            new AstraVFXMapping { astraId = "Nagastra",         impactPoolKey = "VFX_NagaImpact",        castPoolKey = "",                    trailPoolKey = "VFX_NagaTrail" },
            new AstraVFXMapping { astraId = "Varunastra",       impactPoolKey = "VFX_WaterImpact",       castPoolKey = "",                    trailPoolKey = "VFX_WaterTrail" },
            new AstraVFXMapping { astraId = "Vayuastra",        impactPoolKey = "VFX_WindImpact",        castPoolKey = "",                    trailPoolKey = "" },
            new AstraVFXMapping { astraId = "Agneyastra",       impactPoolKey = "VFX_FireImpact",        castPoolKey = "VFX_FireCast",        trailPoolKey = "VFX_FireTrail" },
            new AstraVFXMapping { astraId = "Vajra",            impactPoolKey = "VFX_LightningImpact",   castPoolKey = "",                    trailPoolKey = "VFX_LightningChain" },
        };

        // ── Enemy Death VFX ───────────────────────────────────────────────────────

        [System.Serializable]
        public struct EnemyDeathVFXMapping
        {
            [Tooltip("Enemy tag or type name matching EnemyData.enemyId")]
            public string enemyId;
            [Tooltip("ObjectPool key for death particle")]
            public string deathPoolKey;
        }

        [Header("Enemy Death Mappings")]
        [SerializeField] private EnemyDeathVFXMapping[] enemyDeathMappings = new EnemyDeathVFXMapping[]
        {
            new EnemyDeathVFXMapping { enemyId = "Rakshasa",       deathPoolKey = "VFX_DarkSmokeDeath" },
            new EnemyDeathVFXMapping { enemyId = "Naga",           deathPoolKey = "VFX_ScaleShatterDeath" },
            new EnemyDeathVFXMapping { enemyId = "Pisacha",        deathPoolKey = "VFX_GhostDissolveDeath" },
            new EnemyDeathVFXMapping { enemyId = "Vetala",         deathPoolKey = "VFX_DarkSmokeDeath" },
            new EnemyDeathVFXMapping { enemyId = "Yaksha",         deathPoolKey = "VFX_StoneCrumbleDeath" },
            new EnemyDeathVFXMapping { enemyId = "BrahmaRakshasa", deathPoolKey = "VFX_HolyBurstDeath" },
            new EnemyDeathVFXMapping { enemyId = "default",        deathPoolKey = "VFX_DarkSmokeDeath" },
        };

        // ── Boss Phase VFX ────────────────────────────────────────────────────────

        [System.Serializable]
        public struct BossPhaseVFXMapping
        {
            public string bossId;
            [Tooltip("ObjectPool key for the shockwave on phase transition")]
            public string phaseShockwaveKey;
            [Tooltip("ObjectPool key for boss spawn intro burst")]
            public string spawnBurstKey;
        }

        [Header("Boss Phase Mappings")]
        [SerializeField] private BossPhaseVFXMapping[] bossPhaseVFXMappings = new BossPhaseVFXMapping[]
        {
            new BossPhaseVFXMapping { bossId = "Ravana",       phaseShockwaveKey = "VFX_DemonShockwave",  spawnBurstKey = "VFX_RavanaSpawn" },
            new BossPhaseVFXMapping { bossId = "Mahishasura",  phaseShockwaveKey = "VFX_BuffaloShockwave",spawnBurstKey = "VFX_MahishaSpawn" },
            new BossPhaseVFXMapping { bossId = "Kali",         phaseShockwaveKey = "VFX_BloodShockwave",  spawnBurstKey = "VFX_KaliSpawn" },
            new BossPhaseVFXMapping { bossId = "Vritra",       phaseShockwaveKey = "VFX_StormShockwave",  spawnBurstKey = "VFX_VritraSpawn" },
        };

        // ── Fallback Keys ─────────────────────────────────────────────────────────

        [Header("Fallback / General")]
        [Tooltip("Played when no specific mapping is found")]
        public string defaultImpactPoolKey  = "VFX_GenericImpact";
        public string defaultDeathPoolKey   = "VFX_DarkSmokeDeath";
        public string defaultShockwaveKey   = "VFX_GenericShockwave";
        public string agniTierUpBurstKey    = "VFX_AgniTierUp";
        public string xpPickupKey           = "VFX_XPPickup";

        // ── Public API ────────────────────────────────────────────────────────────

        private Dictionary<string, AstraVFXMapping>    _astraDict;
        private Dictionary<string, EnemyDeathVFXMapping> _enemyDict;
        private Dictionary<string, BossPhaseVFXMapping>  _bossDict;

        public void BuildLookups()
        {
            _astraDict = new Dictionary<string, AstraVFXMapping>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var m in astraMappings)
                if (!string.IsNullOrEmpty(m.astraId)) _astraDict[m.astraId] = m;

            _enemyDict = new Dictionary<string, EnemyDeathVFXMapping>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var m in enemyDeathMappings)
                if (!string.IsNullOrEmpty(m.enemyId)) _enemyDict[m.enemyId] = m;

            _bossDict = new Dictionary<string, BossPhaseVFXMapping>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var m in bossPhaseVFXMappings)
                if (!string.IsNullOrEmpty(m.bossId)) _bossDict[m.bossId] = m;
        }

        public bool TryGetAstraMapping(string astraId, out AstraVFXMapping mapping)
        {
            if (_astraDict == null) BuildLookups();
            return _astraDict.TryGetValue(astraId, out mapping);
        }

        public bool TryGetEnemyDeathMapping(string enemyId, out EnemyDeathVFXMapping mapping)
        {
            if (_enemyDict == null) BuildLookups();
            if (_enemyDict.TryGetValue(enemyId, out mapping)) return true;
            if (_enemyDict.TryGetValue("default", out mapping)) return true;
            return false;
        }

        public bool TryGetBossPhaseMapping(string bossId, out BossPhaseVFXMapping mapping)
        {
            if (_bossDict == null) BuildLookups();
            return _bossDict.TryGetValue(bossId, out mapping);
        }

        public int AstraMappingCount  => astraMappings  != null ? astraMappings.Length  : 0;
        public int EnemyMappingCount  => enemyDeathMappings != null ? enemyDeathMappings.Length : 0;
        public int BossMappingCount   => bossPhaseVFXMappings != null ? bossPhaseVFXMappings.Length : 0;
    }
}
