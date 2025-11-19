using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class PetriController : NetworkBehaviour
    {
        // References
        [SerializeField] private MeshRenderer _petriRenderer;
        [SerializeField] private List<Collider> colliders;
        [SerializeField] private LidPetriController lidPetri;

        [SerializeField] private PetriLineTask lineTask;
        [SerializeField] private SeedMicrobesTask seedTask;
    
        public PetriLineTask LineTask => lineTask;
        public SeedMicrobesTask SeedTask => seedTask;
    
        // Network Variables

        //private readonly NetworkVariable<bool> _isGrabbed =
            //new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
        private readonly NetworkVariable<int> _activityIndex = 
            new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public int activityIndex => _activityIndex.Value;
    
    
        private bool _isOnline;


        private void Start()
        {
            seedTask.enabled = false;
        }

        // NETWORK METHODS
        
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
/*
        public void Grab()
        {
            // TODO: _isGrabbed obsolete?
        
            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                // Set grabbed true
                _isGrabbed.Value = true;
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                // If online, it must request ownership first
                RequestGrabServerRPC(true);
            }
        }

        public void Drop()
        {
            if (!_isOnline || (_isOnline && IsOwner))
            {
                // Set grabbed false
                _isGrabbed.Value = false;
            }
            else
            {
                Debug.LogError("Petri dropped by not owner... a fix for delay in ownership assignation might be needed");
            }
        }//*/
    
        // ACCESS METHODS

        public void CompleteActivity()
        {
            if (_isOnline && !IsOwner) return;
        
            _activityIndex.Value++;

            switch (_activityIndex.Value)
            {
                case 1:
                    lineTask.enabled = false;
                    seedTask.enabled = true;
                
                    lidPetri.SetDetachableStateSynced(true);
                
                    if (lidPetri.isGrabbed)
                    {
                        SetCollidersStateSynced(true);
                        lidPetri.SetCollidersStateSynced(false);
                    }
                    break;
            }
        }
    
        public void SetCollidersStateSynced(bool state)
        {
            if (!_isOnline)
            {
                foreach(var col in colliders)
                {
                    col.enabled = state;
                }
            }
            else
            {
                SetCollidersStateRPC(state);
            }
        }

        public void SetLidReference(LidPetriController lidController)
        {
            lidPetri = lidController;
        }
        

        public void RemoveAgar()
        {
            List<Material> matList = new List<Material>(_petriRenderer.materials);
            matList.RemoveAt(1); // index 1 = second material
            _petriRenderer.materials = matList.ToArray();
        }
    
        // RPC METHODS
/*
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(bool value, ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

            // Change variables
            //SetGrabbedClientRpc(value);
        }

        [Rpc(SendTo.Owner)]
        private void SetGrabbedClientRpc(bool value)
        {
            // Change grab state
            _isGrabbed.Value = value;
        }//*/

        [Rpc(SendTo.ClientsAndHost)]
        private void SetCollidersStateRPC(bool state)
        {
            foreach(var col in colliders)
            {
                col.enabled = state;
            }
        }
    }
}
