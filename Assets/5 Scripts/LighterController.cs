using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class LighterController : NetworkBehaviour
    {
        [SerializeField] LineRenderer fire;
    
        private bool _lightOn;
        private bool _isOnline;

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
            }
            else
            {
                ActivateRPC();
            }
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
        private void ActivateRPC()
        {
            _lightOn = true;
            fire.enabled = true;
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void DeactivateRPC()
        {
            _lightOn = false;
            fire.enabled = false;
        }

        public bool IsActive()
        {
            return _lightOn;
        }

    }
}
