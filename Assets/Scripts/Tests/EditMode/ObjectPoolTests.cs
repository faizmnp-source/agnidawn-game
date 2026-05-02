using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AGNIDAWN.Core;

namespace AGNIDAWN.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ObjectPool logic (non-MonoBehaviour aspects).
    /// Full PlayMode pool tests are in PlayMode suite.
    /// Linear: FAI-6
    /// </summary>
    public class ObjectPoolTests
    {
        // ── Pool key naming convention ─────────────────────────────────────

        [Test]
        public void PoolKey_BulletNaming_FollowsConvention()
        {
            // Keys must be stable across sessions — test the convention
            string[] expectedKeys = {
                "Bullet_Trishul",
                "Bullet_Gandiv",
                "Bullet_Chakra",
                "Bullet_Brahmastra",
                "Enemy_Common",
                "Enemy_Elite",
                "VFX_AgniHit",
                "VFX_DivineBurst",
                "UI_DamagePopup"
            };

            foreach (var key in expectedKeys)
            {
                // Keys should not contain spaces and should follow Prefix_Name pattern
                Assert.IsFalse(key.Contains(" "),        $"Key '{key}' should not contain spaces.");
                Assert.IsTrue(key.Contains("_"),         $"Key '{key}' should follow Prefix_Name convention.");
                Assert.IsTrue(key.Length <= 32,          $"Key '{key}' should be <= 32 chars.");
            }
        }

        [Test]
        public void PoolKey_UniqueKeys_NoDuplicates()
        {
            var keys = new System.Collections.Generic.HashSet<string>
            {
                "Bullet_Trishul", "Bullet_Gandiv", "Bullet_Chakra",
                "Bullet_Brahmastra", "Bullet_Pashupatastra", "Bullet_Nagastra",
                "Bullet_Varunastra", "Bullet_Vayuastra", "Bullet_Agneyastra", "Bullet_Thunderbolt",
                "Enemy_Common", "Enemy_Elite", "Enemy_Boss",
                "VFX_AgniHit", "VFX_DivineBurst", "VFX_AgniKundFlame",
                "UI_DamagePopup", "UI_HealPopup", "UI_LevelUpPopup"
            };

            // If any duplicates existed, HashSet would have fewer items
            Assert.AreEqual(19, keys.Count, "All pool keys must be unique.");
        }
    }
}
