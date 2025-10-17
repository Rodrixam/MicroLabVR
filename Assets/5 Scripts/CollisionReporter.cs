using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class CollisionReporter : MonoBehaviour
    {
        public Action<Collider> OnTriggerEntered;
        public Action<Collider> OnTriggerStayed;
        public Action<Collider> OnTriggerExited;

        void OnTriggerEnter(Collider other)
        {
            OnTriggerEntered?.Invoke(other);
        }

        private void OnTriggerStay(Collider other)
        {
            OnTriggerStayed?.Invoke(other);
        }

        void OnTriggerExit(Collider other)
        {
            OnTriggerExited?.Invoke(other);
        }
    }
}
