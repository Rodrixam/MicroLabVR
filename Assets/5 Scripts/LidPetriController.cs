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
        private readonly NetworkVariable<bool> _isGrabbed =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _detachable = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _petriIsAttached = 
            new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool isGrabbed => _isGrabbed.Value;

        private bool _isOnline;
    
        
        // ------------------- NETWORK BEHAVIOUR METHODS -------------------
    
        private void Start()
        {
            // Set interaction events
            grabInteractable.selectEntered.AddListener(Grab);
            grabInteractable.selectExited.AddListener(Drop);
            
            socket.selectEntered.AddListener(OnObjectPlaced);
            socket.selectExited.AddListener(OnObjectRemoved);
        }
        public override void OnDestroy()
        {
            // Remove interaction events
            grabInteractable.selectEntered.RemoveListener(Grab);
            grabInteractable.selectExited.RemoveListener(Drop);
            
            socket.selectEntered.RemoveListener(OnObjectPlaced);
            socket.selectExited.RemoveListener(OnObjectRemoved);
            
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
    
        
        // ------------------- INTERACTION METHODS -------------------

        
        private void Grab(SelectEnterEventArgs args)
        {
            // Ignore if not picked by hand
            if (!IsPickedByHand(args)) return;
        
            // Set isGrabbed true
            SetIsGrabbedStateSynced(true);
            
            // Call grab in LineTask
            petriController.LineTask.Grab();
            
            // Change colliders if littlePetri is attached
            if (_detachable.Value && _petriIsAttached.Value)
            {
                SetCollidersStateSynced(false);
                petriController.SetCollidersStateSynced(true);
            }
            
            // Request Ownership to move the object
            RequestGrabServerRPC();
        }
    
        private void Drop(SelectExitEventArgs args)
        {
            // Skip if not detachable, changes only apply when detachable
            if (!_detachable.Value) return;
            
            // Change colliders
            SetCollidersStateSynced(true);
            if (_petriIsAttached.Value)
            {
                petriController.SetCollidersStateSynced(false);   
            }
            
            // Call drop from the line task
            petriController.LineTask.Drop();
        }
    
        
        // ------------------- SOCKET METHODS -------------------
    
        
        private void OnObjectPlaced(SelectEnterEventArgs args)
        {
            Debug.Log("[LidPetri]: Hello, something fell in my socket");
            
            // Update references in LidPetri and Petri
            var petri = args.interactableObject.transform.GetComponentInChildren<PetriController>();
            if (petri is null) return;
            
            petriController = petri;
            UpdatePetriLidReferenceRPC();
            
            // Set lpIsAttached true
            SetPetriIsAttachedStateSynced(true);

            if(isGrabbed)
            {
                SetCollidersStateSynced(false);
            }
        }
        private void OnObjectRemoved(SelectExitEventArgs args)
        {
            SetPetriIsAttachedStateSynced(false);
        }
        

        // ------------------- RPC METHODS -------------------

        
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void UpdatePetriLidReferenceRPC()
        {
            petriController.SetLidReference(this);
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
        [Rpc(SendTo.ClientsAndHost)]
        private void SetCollidersStateRPC(bool state)
        {
            foreach(var col in colliders)
            {
                col.enabled = state;
            }
        }
        
        
        // ------------------- RPC ACCESS TO VARIABLES -------------------
        
        
        // _isGrabbed
        private void SetIsGrabbedStateSynced(bool state)
        {
            if (!_isOnline)
            {
                _isGrabbed.Value = state;   
            }
            else
            {
                SetIsGrabbedStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetIsGrabbedStateRPC(bool state)
        {
            _isGrabbed.Value = state;
        }
        
        // _detachable
        public void SetDetachableStateSynced(bool state)
        {
            if (!_isOnline)
            {
                _detachable.Value = state;   
            }
            else
            {
                SetPetriIsAttachedStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetDetachableStateRPC(bool state)
        {
            _detachable.Value = state;
        }
        
        // _petriIsAttached
        private void SetPetriIsAttachedStateSynced(bool state)
        {
            if (!_isOnline)
            {
                _petriIsAttached.Value = state;   
            }
            else
            {
                SetPetriIsAttachedStateRPC(state);
            }
        }
        [Rpc(SendTo.Owner)]
        private void SetPetriIsAttachedStateRPC(bool state)
        {
            _petriIsAttached.Value = state;
        }
        
        
        // ------------------- OTHERS -------------------
    
        
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }
    }
}
