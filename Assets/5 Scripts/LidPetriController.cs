using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class LidPetriController : NetworkBehaviour
    {
        // References
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private List<Collider> colliders;
        [SerializeField] private XRSocketInteractor socket;
        [SerializeField] private PetriController  petriController;
        public PetriController PetriController => petriController;
        
        // Variables
        private readonly NetworkVariable<bool> _lpDetachable = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _lpIsAttached = 
            new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isGrabbed =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool isGrabbed => _isGrabbed.Value;

        private bool _isOnline;
    
        
        // NETWORK BEHAVIOUR METHODS
    
        private void Start()
        {
            // Deactivate main petri colliders
            petriController.SetCollidersState(false);
            
            // Set interaction events
            grabInteractable.selectEntered.AddListener(Grab);
            socket.selectEntered.AddListener(OnObjectPlaced);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // Remove interaction events
            grabInteractable.selectEntered.RemoveListener(Grab);
            socket.selectEntered.RemoveListener(OnObjectPlaced);
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

        private void Grab(SelectEnterEventArgs args)
        {
            // Ignore if not picked by hand
            if (!IsPickedByHand(args)) return;
        
            // ------------------------- LOCAL -------------------------
            if (!_isOnline)
            {
                _isGrabbed.Value = true;
            
                if (_lpDetachable.Value && _lpIsAttached.Value)
                {
                    // Change colliders
                    SetCollidersState(false);
                    petriController.SetCollidersState(true);
            
                    // Call grab from main
                    //petriController.Grab();
                }
            }
            // ------------------------- ONLINE -------------------------
            else
            {
                // If online, it must request ownership first
                RequestGrabServerRPC();
            }
        }
    
        public void Drop()
        {
            // Skip if not detachable, changes only apply when detachable
            if (!_lpDetachable.Value) return;

            if (!_isOnline)
            {
                // Change colliders
                SetCollidersState(true);
                if (_lpIsAttached.Value)
                {
                    petriController.SetCollidersState(false);   
                }
            
                // Call drop from main
                //petriController.Drop();
        
                // Call grab from the line task if not completed
                if (!petriController.LineTask.allLinesDrawn)
                {
                    petriController.LineTask.Drop();
                }   
            }
            else
            {
                DropRPC();
            }
        }
    
        // SOCKET METHODS
    
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            petriController = args.interactableObject.transform.GetComponentInChildren<PetriController>();
        }
    
        public void AttachLittlePetri()
        {
            if (!_isOnline || (_isOnline && IsOwner))
            {
                _lpIsAttached.Value = true;   
            }

            petriController.SetLidReference(this);
            if(isGrabbed)
            {
                SetCollidersState(false);
            }
        }
    
        public void DetachLittlePetri()
        {
            if (!_isOnline || (_isOnline && IsOwner))
            {
                _lpIsAttached.Value = false;   
            }
        }
    
        // OTHERS
    
        public void SetCollidersState(bool state)
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

        // Called from PetriController, already set for only Owner
        public void SetDetachable(bool state)
        {
            _lpDetachable.Value = state;
        }
    
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }

        // RPC METHODS

        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

            // Change variables
            GrabClientRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void GrabClientRpc()
        {
            if (IsOwner)
            {
                _isGrabbed.Value = true;
            }
        
            if (_lpDetachable.Value && _lpIsAttached.Value)
            {
                // Change colliders
                SetCollidersState(false);
                petriController.SetCollidersState(true);
            
                // Call grab from main
                //petriController.Grab();
            }
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void DropRPC()
        {
            // Change colliders
            SetCollidersState(true);
            if (_lpIsAttached.Value)
            {
                petriController.SetCollidersState(false);   
            }
            
            // Call drop from main
            //petriController.Drop();
        
            // Call grab from the line task if not completed
            if (!petriController.LineTask.allLinesDrawn)
            {
                petriController.LineTask.Drop();
            }   
        }

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
