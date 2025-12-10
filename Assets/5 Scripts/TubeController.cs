using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private XRGrabInteractable _grabInteractable;
        [SerializeField] private CottonLidController cottonLid;
        [SerializeField] private TubeTimerController tubeTimer;
        [SerializeField] private TubePickSampleTask pickTask;
        [SerializeField] private ParticleSystem particles;
        private BeakGlassController _beakGlass;
        public TubeTimerController TubeTimer => tubeTimer;
        public CottonLidController CottonLid => cottonLid;

        [Header("Colliders")]
        [SerializeField] private Collider longCollider;
        [SerializeField] private Collider shortCollider;
        
        private readonly NetworkVariable<bool> _onHeat = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public bool OnHeat => _onHeat.Value;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
        private bool _isOnline;
        
        // NETWORK BEHAVIOUR METHODS
        
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

        private void Awake()
        {
            _grabInteractable.selectEntered.AddListener(Grab);
        }
        public override void OnDestroy()
        {
            _grabInteractable.selectEntered.RemoveListener(Grab);
            base.OnDestroy();
        }

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            
            _beakGlass = FindAnyObjectByType<BeakGlassController>();
        }

        private void Update()
        {
            if (_isOnline && !IsOwner) return;
            
            // Boil Timer
            if (_onHeat.Value && TubeTimer.interval < 3)
            {
                TubeTimer.AddBoilTimeSynced(Time.deltaTime);
                if(TubeTimer.interval >= 3)
                {
                    _beakGlass.AskTimerSoundSynced(false);
                }
            }
            
            // Sample Pick
            if (cottonLid.isOpen && cottonLid.isFlamed && !pickTask.tracking)
            {
                particles.Play();
                pickTask.ActivateTracking();
            }
            else if(!(cottonLid.isOpen && cottonLid.isFlamed) && pickTask.tracking)
            {
                pickTask.DeactivateTracking();
            }
        }
        
        // INTERACTION METHODS
        
        // Grab
        private void Grab(SelectEnterEventArgs args)
        {
            // Skip if wasn't picked by hand
            if (!IsPickedByHand(args)) return;
            Debug.Log($"[Tube]: I was grabbed. isOwner? {IsOwner}");
            // Cotton Lid colliders
            cottonLid.SetActiveCollidersStateSynced(true);
            
            // Ownership
            if (_isOnline)
            {
                if (!IsOwner)
                {
                    Debug.Log($"[Tube]: Since I'm not owner, I'm going to request ownership.");
                    RequestOwnershipServerRPC();
                }
                cottonLid.RequestOwnershipServerRPC();
                //cottonLid.Drop();
            }
        }
        [ServerRpc(RequireOwnership = false)]
        public void RequestOwnershipServerRPC(ServerRpcParams rpcParams = default)
        {
            Debug.Log($"[Tube (Server)]: It was requested a change of ownership.");
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            NetworkObject.ChangeOwnership(grabbingClientId);
        }
        
        // Drop
        public void Drop()
        {
            Debug.Log($"[Tube]: I was dropped. isOwner? {IsOwner}");
            if (_isOnline && !IsOwner) return;
            // Cotton Lid colliders
            cottonLid.SetActiveCollidersStateSynced(false);
        }
        
        // Set on/off fire
        public void SetOnFireSynced(bool state)
        {
            if (!_isOnline)
            {
                _onHeat.Value = state;
                if (state && TubeTimer.interval < 3)
                {
                    _beakGlass.AskTimerSoundSynced(true);
                }
            }
            else
            {
                SetOnFireRPC(state);
            }
        }
        [Rpc(SendTo.Authority)]
        private void SetOnFireRPC(bool state)
        {
            _onHeat.Value = state;
            if (state && TubeTimer.interval < 3)
            {
                _beakGlass.AskTimerSoundSynced(true);
            }
        }

        // Aux
        private bool IsPickedByHand(SelectEnterEventArgs args)
        {
            if (args.interactorObject.transform.CompareTag("LeftHand") ||
                args.interactorObject.transform.CompareTag("RightHand"))
            {
                return true;
            }
            return false;
        }

        // COLLIDER CONTROL METHODS
        
        public void SetColliderStateSynced(bool longState, bool shortState)
        {
            if (!_isOnline)
            {
                longCollider.enabled = longState;
                shortCollider.enabled = shortState;   
            }
            else
            {
                SetShortColliderStateRPC(shortState);
                SetLongColliderStateRPC(longState);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetShortColliderStateRPC(bool shortState)
        {
            shortCollider.enabled = shortState;   
            Debug.Log($"[Tube]: Setting my shortCollider? {shortState}.");
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetLongColliderStateRPC(bool longState)
        {
            longCollider.enabled = longState;
            Debug.Log($"[Tube]: Setting my colliders. longCollider? {longState}.");
        }
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            SetColliderStateSynced(true, false);
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            SetOnFireSynced(false);
            
            // Tasks
            tubeTimer.RestoreToStartConditionsSynced();
            pickTask.RestoreToStartConditionsSynced();
            cottonLid.RestoreToStartConditions();
        }

        
        // Physics
        private void StopRigidBodySynced()
        {
            if (!_isOnline)
            {
                var rBody = GetComponent<Rigidbody>();
                if (rBody is not null)
                {
                    rBody.linearVelocity = Vector3.zero;
                    rBody.angularVelocity = Vector3.zero;
                }
            }
            else
            {
                StopRigidBodyRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void StopRigidBodyRPC()
        {
            var rBody = GetComponent<Rigidbody>();
            if (rBody is not null)
            {
                rBody.linearVelocity = Vector3.zero;
                rBody.angularVelocity = Vector3.zero;
            }
        }
    }
}