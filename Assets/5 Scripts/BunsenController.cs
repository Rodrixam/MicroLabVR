using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class BunsenController : NetworkBehaviour
    {
        [SerializeField] private ValveController valve;
        [SerializeField] private GameObject fire;
        [SerializeField] private XRSocketInteractor tripodSocket;
        [SerializeField] private AudioSource fireAudioSource;
        
        private TripodController _tripod;
        private LighterController _lighter;

        private readonly NetworkVariable<bool> _onFire = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool onFire => _onFire.Value;

        private bool _isOnline;
        
        // METHODS
        
        private void Awake()
        {
            tripodSocket.selectEntered.AddListener(OnTripodPlaced);
            tripodSocket.selectExited.AddListener(OnTripodRemoved);
        }
        public override void OnDestroy()
        {
            tripodSocket.selectEntered.RemoveListener(OnTripodPlaced);
            tripodSocket.selectExited.RemoveListener(OnTripodRemoved);
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

        void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
            
            // Turn on flame
            if (!_onFire.Value && valve.releasingGas && _lighter is not null && _lighter.lightOn)
            {
                _onFire.Value = true;
                
                // Feedback
                SetFireStateSynced(true);
            }

            // Turn off flame
            if (_onFire.Value && !valve.releasingGas)
            {
                _onFire.Value = false;
                
                // Feedback
                SetFireStateSynced(false);
            }
        }
        
        // COLLISION METHODS

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("LighterTip")) return;
            
            _lighter = other.gameObject.GetComponentInParent<LighterController>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("LighterTip")) return;

            _lighter = null;
        }
        
        // SOCKET METHODS
        
        private void OnTripodPlaced(SelectEnterEventArgs args)
        {
            // save tripod reference
            _tripod = args.interactableObject.transform.GetComponent<TripodController>();
            
            // skip if not owner
            if (_isOnline && !IsOwner) return;
            if (_onFire.Value)
            {
                _tripod.SetOnFireSynced(true);
            }
        }
        private void OnTripodRemoved(SelectExitEventArgs args)
        {
            _tripod.SetOnFireSynced(false);
            _tripod = null;
        }
        
        // RPC METHODS

        private void SetFireStateSynced(bool state)
        {
            if (!_isOnline)
            {
                fire.SetActive(state);
                fireAudioSource.Play();
                valve.SetGasSoundStateSynced(false);
                
                if (_tripod is not null && _tripod.onFire != state)
                {
                    _tripod.SetOnFireSynced(state);
                }   
            }
            else
            {
                SetFireRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetFireRPC(bool state)
        {
            fire.SetActive(state);
            fireAudioSource.Play();
            valve.SetGasSoundStateSynced(false);
            
            if (_tripod is not null && _tripod.onFire != state)
            {
                _tripod.SetOnFireSynced(state);
            }   
        }
        
        // OTHERS
        
        public void RestoreToStartConditionsSynced()
        {
            // Controller Variables
            ResetNetworkVariablesSynced();
            // Components
            ResetComponentsSynced();
            
            valve.RestoreToStartConditionsSynced();
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
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                // fire
                fire.SetActive(false);
                // socket
                var gi = tripodSocket.GetOldestInteractableSelected();
                if (gi != null)
                {
                    tripodSocket.interactionManager.CancelInteractableSelection(gi);
                }
                // references
                _lighter = null;
                _tripod = null;
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            // fire
            fire.SetActive(false);
            // socket
            var gi = tripodSocket.GetOldestInteractableSelected();
            if (gi != null)
            {
                tripodSocket.interactionManager.CancelInteractableSelection(gi);
            }
            // references
            _lighter = null;
            _tripod = null;
        }
    }
}
