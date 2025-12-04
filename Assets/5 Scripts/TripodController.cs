using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TripodController : NetworkBehaviour
    {
        // References
        [SerializeField] private XRSocketInteractor glassSocket;
        private BeakGlassController _glass;
        
        // Variables
        private readonly NetworkVariable<bool> _onFire = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool onFire => _onFire.Value;

        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
        private bool _isOnline;
        
        // NETWORK BEHAVIOUR METHODS

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

        private void Awake()
        {
            glassSocket.selectEntered.AddListener(OnObjectPlaced);
            glassSocket.selectExited.AddListener(OnObjectRemoved);
        }
        public override void OnDestroy()
        {
            glassSocket.selectEntered.RemoveListener(OnObjectPlaced);
            glassSocket.selectExited.RemoveListener(OnObjectRemoved);
            base.OnDestroy();
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
        
        // INTERACTION METHODS

        public void SetOnFireSynced(bool state)
        {   
            // Set onFire value
            if (!_isOnline)
            {
                _onFire.Value = state;
            }
            else
            {
                SetOnFireStateRPC(state);
            }
            
            // Set glass on fire if attached
            if (_glass is not null)
            {
                _glass.SetOnFireStateSynced(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetOnFireStateRPC(bool state)
        {
            _onFire.Value = state;
        }
        
        // SOCKET EVENTS
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            _glass = args.interactableObject.transform.GetComponent<BeakGlassController>();
            
            if (_onFire.Value)
            {
                _glass.SetOnFireStateSynced(true);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            _glass.SetOnFireStateSynced(false);
            _glass = null;
        }
        
        // OTHER
        
        public void RestoreToStartConditions()
        {
            // Physics
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            ResetComponentsSynced();
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
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                _glass = null;
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            _glass = null;
        }
        
        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _onFire.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _onFire.Value = false;
        }
        
    }
}
