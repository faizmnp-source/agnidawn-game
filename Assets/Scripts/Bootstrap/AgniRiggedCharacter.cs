using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Assembles Agni from 20 individual PNG body parts.
    ///
    /// ORIGIN RULE: AgniVisual local Y=0 == feet / ground contact point.
    /// Hip bone is placed at HIP_Y (3.77 local units) so the bottom of the boots
    /// sits exactly at Y=0 of this transform. Player root also spawns at feet level,
    /// so Agni never floats or sinks.
    ///
    /// ANIMATION RULE: only localEulerAngles (rotation) are modified at runtime.
    /// No localPosition changes during animation → no drift, no detached parts.
    ///
    /// FLIP RULE: transform.localScale.x is negated to face left. All rotations
    /// are defined in local space so they mirror correctly with the parent scale.
    ///
    /// Hierarchy:
    ///   AgniVisual  (this GO — SCALE=0.20)
    ///     Agni_Root  (positive-scale flip anchor)
    ///       hip  (pivot, at HIP_Y so boots bottom == Y=0)
    ///         spine → neck → hair_top
    ///         torso sprites, arm chains, leg chains
    ///         weaponHolder_R  (right wrist → weapon follows hand)
    /// </summary>
    public class AgniRiggedCharacter : MonoBehaviour
    {
        // ── Scale ────────────────────────────────────────────────────────────
        private const float SCALE = 0.20f;

        // Hip_Y: places boot bottoms exactly at Y=0 in AgniVisual local space.
        // Derivation: boot_bottom = hip_y − 3.77  →  hip_y = 3.77
        // (sum: lHip-down=0, lKnee=-1.63, lAnkle=-1.22, boot_center=-0.46, boot_half=-0.46)
        private const float HIP_Y = 3.77f;

        // ── Bone pivots ───────────────────────────────────────────────────────
        private Transform _root;          // flip anchor (child of this)

        private Transform _hip;
        private Transform _spine;
        private Transform _neck;
        private Transform _hairTop;

        private Transform _lShoulder, _lUArm, _lElbow, _lWrist;
        private Transform _rShoulder, _rUArm, _rElbow, _rWrist;
        private Transform _weaponHolder;

        private Transform _lHip, _lKnee, _lAnkle;
        private Transform _rHip, _rKnee, _rAnkle;

        // ── Runtime ───────────────────────────────────────────────────────────
        private Rigidbody2D _rb;
        private float       _t;
        private float       _runBlend;
        private bool        _facingLeft;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            BuildRig();
        }

        private void Update()
        {
            _t += Time.deltaTime;

            float speed = _rb != null ? Mathf.Abs(_rb.linearVelocity.x) : 0f;
            _runBlend   = Mathf.MoveTowards(_runBlend, speed > 0.3f ? 1f : 0f, Time.deltaTime * 8f);

            // Facing direction: flip the root anchor only (keep AgniVisual at positive scale)
            if (_rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
                _facingLeft = _rb.linearVelocity.x < 0f;

            if (_root != null)
                _root.localScale = new Vector3(_facingLeft ? -1f : 1f, 1f, 1f);

            Animate();
        }

        // ── Animation (rotation-only) ─────────────────────────────────────────

        private void Animate()
        {
            float idle = 1f - _runBlend;
            float run  = _runBlend;
            float c    = Mathf.Sin(_t * 9f);   // ~4.5 strides/sec

            // ── Spine: subtle breathing tilt (rotation, no position change) ──
            if (_spine != null)
                _spine.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(_t * 1.3f) * 2f * idle);

            // ── Hair ──────────────────────────────────────────────────────────
            if (_hairTop != null)
            {
                float hairRot = run > 0.05f
                    ? -15f * run                       // stream back while running
                    : (Mathf.Sin(_t * 4.1f) * 9f
                     + Mathf.Sin(_t * 7.3f) * 4f
                     + Mathf.Sin(_t * 2.7f) * 5f) * idle;
                _hairTop.localEulerAngles = new Vector3(0f, 0f, hairRot);
            }

            // ── Arms ─────────────────────────────────────────────────────────
            float armSwing = c * 26f * run;
            float armSway  = Mathf.Sin(_t * 1.6f) * 3f * idle;

            if (_lUArm != null) _lUArm.localEulerAngles = new Vector3(0f, 0f, -90f + armSway + armSwing);
            if (_rUArm != null) _rUArm.localEulerAngles = new Vector3(0f, 0f,  90f - armSway - armSwing);

            float elbowFlex = c * 12f * run;
            if (_lElbow != null) _lElbow.localEulerAngles = new Vector3(0f, 0f,  elbowFlex);
            if (_rElbow != null) _rElbow.localEulerAngles = new Vector3(0f, 0f, -elbowFlex);

            // ── Legs ─────────────────────────────────────────────────────────
            float legSwing = c * 22f * run;
            if (_lHip != null) _lHip.localEulerAngles = new Vector3(0f, 0f,  legSwing);
            if (_rHip != null) _rHip.localEulerAngles = new Vector3(0f, 0f, -legSwing);

            float lKnee = Mathf.Max(0f, -c) * 20f * run;
            float rKnee = Mathf.Max(0f,  c) * 20f * run;
            if (_lKnee != null) _lKnee.localEulerAngles = new Vector3(0f, 0f,  lKnee);
            if (_rKnee != null) _rKnee.localEulerAngles = new Vector3(0f, 0f, -rKnee);

            float lAnkle = lKnee * 0.3f;
            float rAnkle = rKnee * 0.3f;
            if (_lAnkle != null) _lAnkle.localEulerAngles = new Vector3(0f, 0f, -lAnkle);
            if (_rAnkle != null) _rAnkle.localEulerAngles = new Vector3(0f, 0f,  rAnkle);
        }

        // ── Rig construction ─────────────────────────────────────────────────

        private void BuildRig()
        {
            // AgniVisual stays at uniform positive scale; flip is done by _root child.
            transform.localScale = Vector3.one * SCALE;

            // Flip anchor — scale.x toggled in Update()
            _root = new GameObject("Agni_Root").transform;
            _root.SetParent(transform, false);
            _root.localPosition = Vector3.zero;

            var sp = LoadSprites();

            // ── Central spine chain ─────────────────────────────────────────
            // Hip is raised to HIP_Y so boot bottoms sit at Y=0 (feet / ground contact).
            _hip     = Bone("hip",      _root,   0f,    HIP_Y);
            _spine   = Bone("spine",    _hip,    0f,    1.73f);
            _neck    = Bone("neck",     _spine,  0f,    2.22f);
            _hairTop = Bone("hair_top", _neck,   0f,    1.72f);

            // ── Torso sprites ────────────────────────────────────────────────
            Spr("torso_lower", _hip,   0f,  0.865f, sp, 14);
            Spr("belt",        _hip,   0f,  0.51f,  sp, 15);
            Spr("torso_upper", _spine, 0f,  1.11f,  sp, 16);

            // ── Head ─────────────────────────────────────────────────────────
            Spr("head",       _neck,    0f,  0.86f, sp, 19);
            Spr("hair_flame", _hairTop, 0f,  0.45f, sp, 20);

            // ── Left arm (front layer) ────────────────────────────────────────
            _lShoulder = Bone("l_shldr",  _spine,     -1.72f,  1.80f);
            Spr("shoulder_L", _lShoulder, 0f, 0f, sp, 17);

            _lUArm = Bone("l_uarm", _lShoulder, -1.47f, 0f);
            _lUArm.localEulerAngles = new Vector3(0f, 0f, -90f);
            Spr("upper_arm_L", _lUArm, -0.66f, 0f, sp, 17);

            _lElbow = Bone("l_elbow", _lUArm, -1.32f, 0f);
            Spr("lower_arm_L", _lElbow, -0.875f, 0f, sp, 17);

            _lWrist = Bone("l_wrist", _lElbow, -1.75f, 0f);
            Spr("hand_L", _lWrist, -0.60f, 0f, sp, 17);

            // ── Right arm (back layer) ────────────────────────────────────────
            _rShoulder = Bone("r_shldr", _spine,    1.72f, 1.80f);
            Spr("shoulder_R", _rShoulder, 0f, 0f, sp, 11);

            _rUArm = Bone("r_uarm", _rShoulder, 1.47f, 0f);
            _rUArm.localEulerAngles = new Vector3(0f, 0f, 90f);
            Spr("upper_arm_R", _rUArm, 0.67f, 0f, sp, 11);

            _rElbow = Bone("r_elbow", _rUArm, 1.34f, 0f);
            Spr("lower_arm_R", _rElbow, 0.88f, 0f, sp, 11);

            _rWrist = Bone("r_wrist", _rElbow, 1.76f, 0f);
            Spr("hand_R", _rWrist, 0.53f, 0f, sp, 11);

            // ── Weapon holder: named pivot so weapon always follows right hand ─
            _weaponHolder = Bone("weaponHolder_R", _rWrist, 1.46f, 0f);
            Spr("weapon_sword", _weaponHolder, 0f, -1.10f, sp, 12);

            // ── Left leg (front layer) ────────────────────────────────────────
            _lHip   = Bone("l_hip",   _hip,   -0.80f,  0f);
            Spr("thigh_L", _lHip,   0f, -0.815f, sp, 18);

            _lKnee  = Bone("l_knee",  _lHip,   0f, -1.63f);
            Spr("shin_L",  _lKnee,  0f, -0.61f, sp, 18);

            _lAnkle = Bone("l_ankle", _lKnee,  0f, -1.22f);
            Spr("boot_L",  _lAnkle, 0f, -0.46f, sp, 18);

            // ── Right leg (back layer) ────────────────────────────────────────
            _rHip   = Bone("r_hip",   _hip,    0.80f,  0f);
            Spr("thigh_R", _rHip,   0f, -0.815f, sp, 10);

            _rKnee  = Bone("r_knee",  _rHip,   0f, -1.63f);
            Spr("shin_R",  _rKnee,  0f, -0.61f, sp, 10);

            _rAnkle = Bone("r_ankle", _rKnee,  0f, -1.22f);
            Spr("boot_R",  _rAnkle, 0f, -0.46f, sp, 10);

            Debug.Log("[AgniRiggedCharacter] Rig built — 20 parts assembled. Origin=feet.");
        }

        // ── Factories ─────────────────────────────────────────────────────────

        private static Transform Bone(string name, Transform parent, float lx, float ly)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(lx, ly, 0f);
            return go.transform;
        }

        private static void Spr(string partName, Transform parent,
            float ox, float oy,
            Dictionary<string, Sprite> sprites, int sortOrder)
        {
            var go = new GameObject($"spr_{partName}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(ox, oy, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortOrder;
            if (sprites.TryGetValue(partName, out var s))
                sr.sprite = s;
            else
                Debug.LogWarning($"[AgniRig] Missing sprite: {partName}");
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var d = new Dictionary<string, Sprite>();
            string[] names =
            {
                "belt","boot_L","boot_R","hair_flame",
                "hand_L","hand_R","head",
                "lower_arm_L","lower_arm_R",
                "shin_L","shin_R",
                "shoulder_L","shoulder_R",
                "thigh_L","thigh_R",
                "torso_lower","torso_upper",
                "upper_arm_L","upper_arm_R",
                "weapon_sword"
            };
            foreach (var n in names)
            {
                var sp = Resources.Load<Sprite>($"Characters/AgniParts/{n}");
                if (sp != null) d[n] = sp;
                else Debug.LogWarning($"[AgniRig] Cannot load: Characters/AgniParts/{n}");
            }
            return d;
        }
    }
}
