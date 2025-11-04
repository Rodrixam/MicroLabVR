using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class FollowPhysics : MonoBehaviour
    {
        public Transform target;
        private Rigidbody _rigidbody;
    
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            _rigidbody.MovePosition(target.transform.position);
        }
    }
}
