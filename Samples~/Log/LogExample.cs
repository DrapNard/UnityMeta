using UnityEngine;

namespace UnityMeta.Samples.Log
{
    public sealed class LogExample : MonoBehaviour
    {
        [Log("Combat")]
        public void Attack(int damage)
        {
            Debug.Log("Damage: " + damage);
        }
    }
}
