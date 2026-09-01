using UnityEngine;

namespace UnityMeta.Samples.Clamp
{
    public sealed class CombatExample : MonoBehaviour
    {
        [Header("Combat gauges")]
        [Clamp(0, nameof(hpMax))]
        public int hp;

        public int hpMax = 100;

        [Clamp(0f, nameof(energyMax))]
        public float energy;

        public float energyMax = 100f;

        private void Start()
        {
            hp = 900;      // Becomes hpMax.
            energy = -10f; // Becomes 0.
        }
    }
}
