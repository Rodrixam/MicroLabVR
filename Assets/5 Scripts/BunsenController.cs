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
        
        private TripodController _tripod;
        private LighterController _lighter;

        private readonly NetworkVariable<bool> _onFire = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool onFire => _onFire.Value;

        private bool _isOnline;
        
        // METHODS
        
        private void Awake()
        {
            tripodSocket.selectEntered.AddListener(OnTripodPlaced);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            tripodSocket.selectExited.RemoveListener(OnTripodRemoved);
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

        private void Start()
        {
            fire.SetActive(false);
        }

        void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
            
            // Turn on flame
            if (!_onFire.Value && valve.releasingGas && _lighter is not null && _lighter.IsActive())
            {
                _onFire.Value = true;
                
                // Feedback
                if (!_isOnline)
                {
                    fire.SetActive(true);
                
                    if (_tripod is not null && !_tripod.onFire)
                    {
                        _tripod.SetOnFire(true);
                    }   
                }
                else
                {
                    SetFireRPC(true);
                }
            }

            // Turn off flame
            if (_onFire.Value && !valve.releasingGas)
            {
                _onFire.Value = false;
                
                if (!_isOnline)
                {
                    fire.SetActive(false);
                
                    if (_tripod is not null && _tripod.onFire)
                    {
                        _tripod.SetOnFire(false);
                    }
                }
                else
                {
                    SetFireRPC(false);    
                }
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
            _tripod = args.interactableObject.transform.GetComponent<TripodController>();
            
            if (_onFire.Value)
            {
                _tripod.SetOnFire(true);
            }
        }
        private void OnTripodRemoved(SelectExitEventArgs args)
        {
            _tripod.SetOnFire(false);
            _tripod = null;
        }
        
        // RPC METHODS
        
        [Rpc(SendTo.ClientsAndHost)]
        private void SetFireRPC(bool state)
        {
            fire.SetActive(state);
            
            if (_tripod is not null && _tripod.onFire != state)
            {
                _tripod.SetOnFire(state);
            }   
        }
    }
}
