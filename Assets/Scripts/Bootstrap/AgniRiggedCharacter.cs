using System.Collections.Generic;
using UnityEngine;

namespace AGNIDAWN.Bootstrap
{
    /// <summary>
    /// Assembles Agni from 20 individual PNG body parts stored in
    /// Resources/Characters/AgniParts/.
    ///
    /// Builds a pivot-bone hierarchy:
    ///   hip → spine → neck → head / hair
    ///            └─ shoulders → upper-arm → elbow → wrist → hand
    ///   hip → l_hip / r_hip → knee → ankle → boot
    ///
    /// Drives procedural idle (breathing, hair flicker) and
    /// run (leg/arm swing, body bob) via sin-wave math — no Animator required.
    ///
    /// Attach to a child GO of the Player. Reads parent Rigidbody2D for velocity.
    /// </summary>
    public class AgniRiggedCharacter : MonoBehaviour
    {
        // ── Visual scale ────────────────────────────────────────────────────
        // Parts are authored at PPU = 100. SCALE maps them to game world size.
        // At 0.20 the assembled character is ~2 u tall; camera orthoSize = 6 → 17% of screen.
        private const float SCALE = 0.20f;

        // ── Bone pivot references ────────────────────────────────────────────
        private Transform _hip;
        private Transform _spine;
        private Transform _neck;
        private Transform _hairTop;

        private Transform _lShoulder, _lUArm, _lElbow, _lWrist;
        private Transform _rShoulder, _rUArm, _rElbow, _rWrist;

        private Transform _lHip, _lKnee, _lAnkle;
        private Transform _rHip, _rKnee, _rAnkle;

        // ── Runtime state ────────────────────────────────────────────────────
        private Rigidbody2D _rb;
        private float       _t;
        private float       _runBlend;    // 0 = idle, 1 = run (smooth lerp)
        private bool        _facingLeft;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody2D>();
            BuildRig();
        }

        private void Update()
        {
            _t += Time.deltaTime;

            // Blend toward run when moving, back toward idle when still
            float speed = (_rb != null) ? _rb.linearVelocity.magnitude : 0f;
            _runBlend = Mathf.MoveTowards(_runBlend,
                speed > 0.35f ? 1f : 0f,
                Time.deltaTime * 7f);

            // Face movement direction (flip entire visual root on X)
            if (_rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
                _facingLeft = _rb.linearVelocity.x < 0f;

            transform.localScale = new Vector3(
                _facingLeft ? -SCALE : SCALE,
                SCALE, SCALE);

            // Drive both states; each reads its blend weight
            AnimateIdle(1f - _runBlend);
            AnimateRun(_runBlend);
        }

        // ── Idle animation ───────────────────────────────────────────────────

        private void AnimateIdle(float b)
        {
            if (b < 0.01f) return;

            // Chest breathing: small Y offset on spine
            float breath = Mathf.Sin(_t * 1.3f) * 0.04f * b;
            if (_spine != null)
                _spine.localPosition = new Vector3(0f, 1.73f + breath, 0f);

            // Hair flame: organic flicker
            if (_hairTop != null)
            {
                float flick = Mathf.Sin(_t * 4.1f) * 9f
                            + Mathf.Sin(_t * 7.3f) * 4f
                            + Mathf.Sin(_t * 2.7f) * 5f;
                _hairTop.localEulerAngles = new Vector3(0f, 0f, flick * b);
            }

            // Arms: subtle hanging sway
            float sway = Mathf.Sin(_t * 1.6f) * 3f * b;
            if (_lUArm != null) _lUArm.localEulerAngles = new Vector3(0f, 0f, -90f + sway);
            if (_rUArm != null) _rUArm.localEulerAngles = new Vector3(0f, 0f,  90f - sway);

            // Reset hip bob that run may have applied
            if (_hip != null)
            {
                var p = _hip.localPosition;
                _hip.localPosition = Vector3.MoveTowards(p, new Vector3(p.x, 0f, p.z),
                    Time.deltaTime * 2f);
            }
        }

        // ── Run animation ────────────────────────────────────────────────────

        private void AnimateRun(float b)
        {
            if (b < 0.01f) return;

            float c = Mathf.Sin(_t * 9f);   // ~4.5 strides/sec
            float c2 = Mathf.Sin(_t * 18f); // double frequency for body bob

            // ── Legs ──────────────────────────────────────────────────────
            float legAmp = 22f * b;
            if (_lHip != null) _lHip.localEulerAngles = new Vector3(0f, 0f,  c * legAmp);
            if (_rHip != null) _rHip.localEulerAngles = new Vector3(0f, 0f, -c * legAmp);

            // Knee bend — trailing leg bends more (natural biomechanics)
            float lBend = Mathf.Max(0f, -c) * 20f * b;
            float rBend = Mathf.Max(0f,  c) * 20f * b;
            if (_lKnee != null) _lKnee.localEulerAngles = new Vector3(0f, 0f,  lBend);
            if (_rKnee != null) _rKnee.localEulerAngles = new Vector3(0f, 0f, -rBend);

            // Ankle counter-rotate slightly (foot stays level)
            float lAnkle = lBend * 0.3f;
            float rAnkle = rBend * 0.3f;
            if (_lAnkle != null) _lAnkle.localEulerAngles = new Vector3(0f, 0f, -lAnkle);
            if (_rAnkle != null) _rAnkle.localEulerAngles = new Vector3(0f, 0f,  rAnkle);

            // ── Arms (opposite phase to legs for natural cross-swing) ─────
            float armAmp = 26f * b;
            if (_lUArm != null) _lUArm.localEulerAngles = new Vector3(0f, 0f, -90f + c * armAmp);
            if (_rUArm != null) _rUArm.localEulerAngles = new Vector3(0f, 0f,  90f - c * armAmp);

            // Forearm follows with slight lag (multiply by smaller factor)
            if (_lElbow != null) _lElbow.localEulerAngles = new Vector3(0f, 0f, c * armAmp * 0.4f);
            if (_rElbow != null) _rElbow.localEulerAngles = new Vector3(0f, 0f, -c * armAmp * 0.4f);

            // ── Body bob ──────────────────────────────────────────────────
            float bob = Mathf.Abs(c2) * 0.06f * b;
            if (_hip != null)
            {
                var p = _hip.localPosition;
                _hip.localPosition = new Vector3(p.x, -bob, p.z);
            }

            // ── Hair streams back during run ──────────────────────────────
            if (_hairTop != null)
            {
                float lean = (_facingLeft ? -12f : 12f) * b;
                _hairTop.localEulerAngles = new Vector3(0f, 0f, lean);
            }
        }

        // ── Rig construction ─────────────────────────────────────────────────

        private void BuildRig()
        {
            // Apply global scale once here; Update keeps it updated for flip
            transform.localScale = Vector3.one * SCALE;

            var sp = LoadSprites();

            // ── Central spine chain ──────────────────────────────────────
            _hip     = Bone("hip",      transform, 0f,    0f);
            _spine   = Bone("spine",    _hip,      0f,    1.73f);   // top of torso_lower
            _neck    = Bone("neck",     _spine,    0f,    2.22f);   // top of torso_upper
            _hairTop = Bone("hair_top", _neck,     0f,    1.72f);   // top of head

            // ── Torso sprites ────────────────────────────────────────────
            // torso_lower: 322×173 px → half-h = 0.865  (centered between hip and spine)
            Spr("torso_lower", _hip,   0f,  0.865f, sp, 14);
            // belt: 328×102 px → centered slightly above hip
            Spr("belt",        _hip,   0f,  0.51f,  sp, 15);
            // torso_upper: 344×222 px → half-h = 1.11  (centered between spine and neck)
            Spr("torso_upper", _spine, 0f,  1.11f,  sp, 16);

            // ── Head ─────────────────────────────────────────────────────
            // head: 240×172 px → half-h = 0.86
            Spr("head",       _neck,    0f,  0.86f, sp, 19);
            // hair_flame: 214×90 px → half-h = 0.45
            Spr("hair_flame", _hairTop, 0f,  0.45f, sp, 20);

            // ── Left arm — front layer (higher sort) ─────────────────────
            // Shoulder attachment at x=−(torso_upper_half_w−shoulder_half_w) ≈ −1.72
            _lShoulder = Bone("l_shldr", _spine,     -1.72f, 1.80f);
            Spr("shoulder_L", _lShoulder, 0f, 0f, sp, 17);

            // Outer edge of shoulder (shoulder_L = 147px wide → half = 0.735)
            _lUArm = Bone("l_uarm", _lShoulder, -1.47f, 0f);
            _lUArm.localEulerAngles = new Vector3(0f, 0f, -90f);  // hang down from T-pose
            // upper_arm_L: 132px wide → center 0.66 from pivot
            Spr("upper_arm_L", _lUArm, -0.66f, 0f, sp, 17);

            // Elbow: 1.32 units along arm (full width of upper_arm_L)
            _lElbow = Bone("l_elbow", _lUArm, -1.32f, 0f);
            // lower_arm_L: 175px wide → center 0.875
            Spr("lower_arm_L", _lElbow, -0.875f, 0f, sp, 17);

            // Wrist: 1.75 units along forearm
            _lWrist = Bone("l_wrist", _lElbow, -1.75f, 0f);
            // hand_L: 120px wide → center 0.60
            Spr("hand_L", _lWrist, -0.60f, 0f, sp, 17);

            // ── Right arm — back layer (lower sort) ──────────────────────
            _rShoulder = Bone("r_shldr", _spine,    1.72f, 1.80f);
            Spr("shoulder_R", _rShoulder, 0f, 0f, sp, 11);

            _rUArm = Bone("r_uarm", _rShoulder, 1.47f, 0f);
            _rUArm.localEulerAngles = new Vector3(0f, 0f, 90f);
            // upper_arm_R: 134px wide → center 0.67
            Spr("upper_arm_R", _rUArm, 0.67f, 0f, sp, 11);

            // r_elbow: 1.34 units along upper arm
            _rElbow = Bone("r_elbow", _rUArm, 1.34f, 0f);
            // lower_arm_R: 176px wide → center 0.88
            Spr("lower_arm_R", _rElbow, 0.88f, 0f, sp, 11);

            _rWrist = Bone("r_wrist", _rElbow, 1.76f, 0f);
            // hand_R: 106px wide → center 0.53
            Spr("hand_R",       _rWrist,  0.53f,  0f,    sp, 11);
            // Weapon hangs in right hand — offset forward and slightly down
            // weapon_sword: 292×220 px → 2.92×2.20 units
            Spr("weapon_sword", _rWrist,  1.46f, -1.10f, sp, 12);

            // ── Left leg — front layer ────────────────────────────────────
            // Hip joint: hip_width/2 = ~0.80 offset
            _lHip   = Bone("l_hip",   _hip,   -0.80f,  0f);
            // thigh_L: 170×163 px → half-h = 0.815 (hangs below joint)
            Spr("thigh_L", _lHip,   0f, -0.815f, sp, 18);
            // Knee: 1.63 units below hip joint (full thigh height)
            _lKnee  = Bone("l_knee",  _lHip,   0f, -1.63f);
            // shin_L: 160×122 px → half-h = 0.61
            Spr("shin_L",  _lKnee,  0f, -0.61f,  sp, 18);
            // Ankle: 1.22 units below knee
            _lAnkle = Bone("l_ankle", _lKnee,  0f, -1.22f);
            // boot_L: 172×92 px → half-h = 0.46
            Spr("boot_L",  _lAnkle, 0f, -0.46f,  sp, 18);

            // ── Right leg — back layer ────────────────────────────────────
            _rHip   = Bone("r_hip",   _hip,    0.80f,  0f);
            Spr("thigh_R", _rHip,   0f, -0.815f, sp, 10);
            _rKnee  = Bone("r_knee",  _rHip,   0f, -1.63f);
            // shin_R: 168×122 px → half-h = 0.61 (same as L)
            Spr("shin_R",  _rKnee,  0f, -0.61f,  sp, 10);
            _rAnkle = Bone("r_ankle", _rKnee,  0f, -1.22f);
            Spr("boot_R",  _rAnkle, 0f, -0.46f,  sp, 10);

            Debug.Log("[AgniRiggedCharacter] Rig built — 20 parts assembled.");
        }

        // ── Small factories ──────────────────────────────────────────────────

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
                if (sp != null)
                    d[n] = sp;
                else
                    Debug.LogWarning($"[AgniRig] Cannot load: Characters/AgniParts/{n}");
            }
            return d;
        }
    }
}
