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

        private bool _isOnline;
        
        // NETWORK BEHAVIOUR METHODS
        
        private void Awake()
        {
            glassSocket.selectEntered.AddListener(OnObjectPlaced);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            glassSocket.selectExited.RemoveListener(OnObjectRemoved);
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

        public void SetOnFire(bool state)
        {
            // Called through BunsenController, already synced
            if (_glass is not null)
            {
                _glass.SetOnFireState(state);
            }
            
            // Only owner changes value
            if (_isOnline && !IsOwner) return;
            _onFire.Value = state;
        }
        
        // SOCKET EVENTS
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            _glass = args.interactableObject.transform.GetComponent<BeakGlassController>();
            
            if (_onFire.Value)
            {
                _glass.SetOnFireState(true);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            _glass.SetOnFireState(false);
            _glass = null;
        }
    }
}
