using UnityEngine;

namespace UnityMeta.Samples.ReadTransform
{
    public sealed class ReadTransformExample : MonoBehaviour
    {
        [ReadOffset(5)]
        public int score;

        private void Start()
        {
            score = 10;
            Debug.Log(score); // Observed as 15; the stored field remains 10.
        }
    }
}
