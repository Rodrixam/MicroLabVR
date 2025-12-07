using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    [RequireComponent(typeof(Rigidbody))]
    public class PetriController : NetworkBehaviour
    {
        // References
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private ClientNetworkTransform networkTransform;
        [SerializeField] private NetworkRigidbody networkRigidbody;
        public XRGrabInteractable GrabInteractable => grabInteractable;

        [SerializeField] private MeshRenderer _petriRenderer;
        private Material _agarMat;
        [SerializeField] private List<Collider> colliders;
        [SerializeField] private LidPetriController lidPetri;
        private LidPetriController originalLid;

        [SerializeField] private PetriLineTask lineTask;
        [SerializeField] private SeedMicrobesTask seedTask;

        private readonly NetworkVariable<bool> _isGrabbed =
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool IsGrabbed => _isGrabbed.Value;
        public PetriLineTask LineTask => lineTask;
        public SeedMicrobesTask SeedTask => seedTask;

        private Rigidbody rBody;

        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
        private bool _isOnline;


        // NETWORK METHODS
        
        
        private void Start()
        {
            rBody = GetComponent<Rigidbody>();
            
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            
            _agarMat = _petriRenderer.materials[1];
            originalLid = lidPetri;
            
            grabInteractable.selectEntered.AddListener(Grab);
            grabInteractable.selectExited.AddListener(Drop);
        }

        public override void OnDestroy()
        {
            grabInteractable.selectEntered.RemoveListener(Grab);
            grabInteractable.selectExited.RemoveListener(Drop);
            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            _isOnline = true;
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            _isOnline = false;
            base.OnNetworkDespawn();
        }
    
        // INTERACTION METHODS

        private void Grab(SelectEnterEventArgs args)
        {
            // Ignore if not picked by hand
            if (!IsPickedByHand(args) || !_isOnline) return;
            
            // Claim Ownership
            RequestOwnershipServerRPC();
            // Change isGrabbed value
            SetIsGrabbedRPC(true);
            // Deactivate physics
            SetPhysicsStateSynced(false);
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }

        private void Drop(SelectExitEventArgs args)
        {
            if (_isOnline && !IsOwner) return;
            
            // Change isGrabbed value
            SetIsGrabbedRPC(false);
            // Activate physics
            SetPhysicsStateSynced(true);
        }
        [Rpc(SendTo.Owner)]
        private void SetIsGrabbedRPC(bool state)
        {
            _isGrabbed.Value = state;
        }
        
        public void SetPhysicsStateSynced(bool state)
        {
            if (!_isOnline)
            {
                rBody.isKinematic = !state;
                rBody.useGravity = state;
            }
            else
            {
                SetPhysicsStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetPhysicsStateRPC(bool state)
        {
            rBody.isKinematic = !state;
            rBody.useGravity = state;
        }
    
        // ------------------- RPC METHODS -------------------

        public void CompleteTaskSynced()
        {
            ActivateNextTaskSynced();
            lidPetri.SetDetachableStateSynced(true);
                
            if (lidPetri.isGrabbed)
            {
                SetCollidersStateSynced(true);
                lidPetri.SetCollidersStateSynced(false);
            }
        }

        // Activate the seed task
        private void ActivateNextTaskSynced()
        {
            if (!_isOnline)
            {
                lineTask.enabled = false;
                seedTask.enabled = true;
            }
            else
            {
                ActivateNextTaskRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ActivateNextTaskRPC()
        {
            lineTask.enabled = false;
            seedTask.enabled = true;
        }
    
        // Change colliders state
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
        
        // ------------------- OTHER -------------------

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
        
        
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }
        
        
        
        // ------------------- RESTORATION -------------------
        
        
        public void RestoreToStartConditions()
        {
            // Physics
            SetCollidersStateSynced(false);
            StopRigidBodySynced();
            SetPhysicsStateSynced(false);
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            ResetComponentsSynced();
            lineTask.RestoreToStartConditions();
            seedTask.RestoreToStartConditions();
        }
        
        
        // Physics
        private void StopRigidBodySynced()
        {
            if (!_isOnline)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }
            else
            {
                StopRigidBodyRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopRigidBodyRPC()
        {
            rBody.linearVelocity = Vector3.zero;
            rBody.angularVelocity = Vector3.zero;
        }
        
        
        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _isGrabbed.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _isGrabbed.Value = false;
        }
        
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                // petriRenderer
                if (!_petriRenderer.materials.Contains(_agarMat))
                {
                    List<Material> matList = new List<Material>(_petriRenderer.materials);
                    matList.Insert(1, _agarMat);
                    _petriRenderer.materials = matList.ToArray();
                }
                
                // lid reference
                lidPetri = originalLid;
            }
            else
            {
                ResetComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            // petriRenderer
            if (!_petriRenderer.materials.Contains(_agarMat))
            {
                List<Material> matList = new List<Material>(_petriRenderer.materials);
                matList.Insert(1, _agarMat);
                _petriRenderer.materials = matList.ToArray();
            }
            
            // lid reference
            lidPetri = originalLid;
        }
    }
}
