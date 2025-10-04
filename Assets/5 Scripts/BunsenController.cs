using System;
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
        private TripodController _tripod = null;
        private LighterController _lighter = null;

        private readonly NetworkVariable<bool> _onFire = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private bool _isOnline = false;
        
        private void Awake()
        {
            tripodSocket.selectEntered.AddListener(OnObjectPlaced);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            tripodSocket.selectExited.RemoveListener(OnObjectRemoved);
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
            if (_isOnline && !IsOwner) return;
            
            if (!_onFire.Value && valve.IsReleasingGas() && _lighter is not null && _lighter.IsActive())
            {
                _onFire.Value = true;
                fire.SetActive(true);
                
                if (_tripod is not null && !_tripod.IsOnFire())
                {
                    _tripod.SetOnFire(true);
                }
            }

            if (_onFire.Value && !valve.IsReleasingGas())
            {
                if (_isOnline)
                {
                    SetFireRPC(false);
                }
                _onFire.Value = false;
                fire.SetActive(false);
                
                if (_tripod is not null && _tripod.IsOnFire())
                {
                    _tripod.SetOnFire(false);
                }
            }
        }

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
        
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            _tripod = args.interactableObject.transform.GetComponent<TripodController>();
            
            if (_onFire.Value)
            {
                _tripod.SetOnFire(true);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            _tripod.SetOnFire(false);
            _tripod = null;
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void SetFireRPC(bool state)
        {
            fire.SetActive(state);
        }
    }
}
