using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class UnitInfantry : MonoBehaviour
    {
        public Animator animator;

        /// <summary>
        ///     Cached animator bool id.
        /// </summary>
        private static readonly int IsMoving = Animator.StringToHash("IsMoving");

        /// <summary>
        ///     Cached animator bool id.
        /// </summary>
        private static readonly int IsShooting = Animator.StringToHash("IsShooting");

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public IEnumerator MoveTo(Vector3 target)
        {
            animator.SetBool(IsMoving, true);

            Vector3 start = transform.position;
            const float duration = 0.50f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
            animator.SetBool(IsMoving, false);
        }

        public IEnumerator Attack(Vector3 target)
        {
            animator.SetBool(IsShooting, true);

            const float duration = 0.50f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            animator.SetBool(IsShooting, false);
        }
    }
}