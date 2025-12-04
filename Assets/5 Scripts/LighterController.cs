using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class LighterController : NetworkBehaviour
    {
        [SerializeField] private LineRenderer fire;
        [SerializeField] private AudioSource sparkAudioSource;
    
        private bool _lightOn;
        public bool lightOn => _lightOn;
        private bool _isOnline;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
        }
    

        public void Activate()
        {
            if (!_isOnline)
            {
                _lightOn = true;
                fire.enabled = true;
                sparkAudioSource.Play();
            }
            else
            {
                ActivateRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ActivateRPC()
        {
            _lightOn = true;
            fire.enabled = true;
            sparkAudioSource.Play();
        }
        
        public void Deactivate()
        {
            if (!_isOnline)
            {
                _lightOn = false;
                fire.enabled = false;
            }
            else
            {
                DeactivateRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void DeactivateRPC()
        {
            _lightOn = false;
            fire.enabled = false;
        }
        
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            //Components and References
            Deactivate();
        }

        
        // Physics
        private void StopRigidBodySynced()
        {
            if (!_isOnline)
            {
                var rBody = GetComponent<Rigidbody>();
                if (rBody is not null)
                {
                    rBody.linearVelocity = Vector3.zero;
                    rBody.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                StopRigidBodyRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopRigidBodyRPC()
        {
            var rBody = GetComponent<Rigidbody>();
            if (rBody is not null)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }
        }
    }
}
