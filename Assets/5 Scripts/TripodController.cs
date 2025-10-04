using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TripodController : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> _onFire = 
            new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        [SerializeField] private XRSocketInteractor glassSocket;
        private BeakGlassController _glass;

        private bool _isOnline;
        
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

        public void SetOnFire(bool onFire)
        {
            if (_isOnline && !IsOwner) return;
            
            _onFire.Value = onFire;

            if (_glass is not null)
            {
                _glass.SetOnFire(onFire);
            }
        }
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            
            _glass = args.interactableObject.transform.GetComponent<BeakGlassController>();
            
            if (_onFire.Value)
            {
                _glass.SetOnFire(true);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            
            _glass.SetOnFire(false);
            _glass = null;
        }
    }
}
