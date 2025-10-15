using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class CollisionReporter : MonoBehaviour
    {
        public System.Action<Collider> OnTriggerEntered;
        public System.Action<Collider> OnTriggerExited;

        void OnTriggerEnter(Collider other)
        {
            OnTriggerEntered?.Invoke(other);
        }

        void OnTriggerExit(Collider other)
        {
            OnTriggerExited?.Invoke(other);
        }
    }
}
