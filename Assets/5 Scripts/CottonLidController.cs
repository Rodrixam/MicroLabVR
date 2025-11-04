using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class CottonLidController : NetworkBehaviour
    {
        [Header("References")] [SerializeField]
        private List<Collider> colliders;

        // Visuals
        [SerializeField] private GameObject arrowsUI;
        [SerializeField] private GameObject warningUI;
        [SerializeField] private MeshRenderer lidRenderer;

        // Transforms
        [SerializeField] private Transform body;
        [SerializeField] private Transform handler;

        private GameController _gameController;
        private Rigidbody _handlerRb;
        private XRGrabInteractable _interactable;

        [Header("Parameters")] 
        [SerializeField] private float rotationGoal;
        [SerializeField] private float maxDistance;

        private Quaternion _prevRotation;
        private float _rotationCounter;

        private bool _isOnline;
    
        private readonly NetworkVariable<bool> _isGrabbed = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isFlamed = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isOpen = 
            new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        public bool isFlamed => _isFlamed.Value;
        public bool isOpen => _isOpen.Value;


        void Start()
        {
            // Deactivate grab collider
            SetActiveCollidersState(false);
            
            // Deactivate feedback
            arrowsUI.SetActive(false);
            warningUI.SetActive(false);
        
            // Get handler references
            _handlerRb = handler.GetComponent<Rigidbody>();
            _interactable = handler.GetComponent<XRGrabInteractable>();

            // Get gameController reference
            _gameController = FindAnyObjectByType<GameController>();
            if (_gameController is null)
            {
                Debug.LogError("[COTTON LID]: GameController not found in scene.");
            }
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

        void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
        
            // Force release if handler is far from lid
            if (Vector3.Distance(body.position, handler.position) > maxDistance)
            {
                ForceRelease();
            }

            // Twist the lid to open
            if (_isGrabbed.Value && !isOpen)
            {
                // Calculate rotation gap
                float angleY = Vector3.SignedAngle(
                    _prevRotation * Vector3.forward, // previous direction
                    body.rotation * Vector3.forward, // current direction
                    Vector3.up // rotation axis (Y)
                );
                _rotationCounter += angleY;
                _prevRotation = body.rotation;

                // Fix arrows orientation
                // TODO: arrows are displayed on incorrect axis
                arrowsUI.transform.rotation = Quaternion.Euler(0, body.rotation.y, 0);

                // Check if completed
                if (Mathf.Abs(_rotationCounter) >= rotationGoal && AttachBallToHand())
                {
                    _isOpen.Value = true;
                    ForceRelease();
                    
                    // Visuals
                    if (!_isOnline)
                    {
                        // arrows turn off on ForceRelease
                        warningUI.SetActive(true);
                        lidRenderer.enabled = false;
                    }
                    else
                    {
                        OpenLidRPC();   
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (_isGrabbed.Value)
            {
                AlignBodyToHandlerRotation();
            }
        }

        private void ForceRelease()
        {
            // Loop through all interactors currently holding this object
            foreach (var interactor in _interactable.interactorsSelecting)
            {
                // Drop() is called internally
                _interactable.interactionManager.SelectExit(interactor, _interactable);
            }
        }

        public void Grab()
        {
            if (!_isOnline)
            {
                // Start opening lid
                if (!isOpen)
                {
                    _isGrabbed.Value = true;
                    _prevRotation = handler.rotation;
                
                    arrowsUI.SetActive(true);
                    AlignBodyToHandlerRotation();
                }
                // Close lid
                else if (DetachBallFromHand())
                {
                    _isOpen.Value = false;
                    _isFlamed.Value = false;
                    lidRenderer.enabled = true;
                
                    ForceRelease();
                }
            }
            else
            {
                // Logic is inside
                RequestGrabServerRPC();
            }
        }

        public void Drop()
        {
            _rotationCounter = 0;
            _isGrabbed.Value = false;

            handler.transform.position = body.position;
            handler.transform.rotation = body.rotation;

            _handlerRb.linearVelocity = Vector3.zero;
            _handlerRb.angularVelocity = Vector3.zero;

            if (!_isOnline)
            {
                arrowsUI.SetActive(false);   
            }
            else
            {
                TurnOffArrowsRPC();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Flame the tube
            if (other.CompareTag("BurnerFlame") && isOpen && !_isFlamed.Value &&
                other.GetComponentInParent<BunsenController>().onFire)
            {
                _isFlamed.Value = true;

                if (!_isOnline)
                {
                    warningUI.SetActive(false);
                }
                else
                {
                    TurnOffWarningRPC();
                }
            }
        }

        public void SetActiveCollidersState(bool state)
        {
            if (!_isOnline)
            {
                foreach (var col in colliders)
                {
                    col.enabled = state;
                }
            }
            else
            {
                SetActiveCollidersStateRPC(state);
            }
        }

        private bool AttachBallToHand()
        {
            foreach (var interactor in _interactable.interactorsSelecting)
            {
                if (interactor.transform.CompareTag("LeftHand"))
                {
                    if (_gameController.AttachCottonBallToHand(false)) return true;
                }
                else if (interactor.transform.CompareTag("RightHand"))
                {
                    if (_gameController.AttachCottonBallToHand(true)) return true;
                }
            }

            return false;
        }

        private bool DetachBallFromHand()
        {
            foreach (var interactor in _interactable.interactorsSelecting)
            {
                if (interactor.transform.CompareTag("LeftHand"))
                {
                    if (_gameController.DetachCottonBall(false)) return true;
                }
                else if (interactor.transform.CompareTag("RightHand"))
                {
                    if (_gameController.DetachCottonBall(true)) return true;
                }
            }

            return false;
        }

        private void AlignBodyToHandlerRotation()
        {
            Vector3 euler = body.rotation.eulerAngles;
            euler.y = handler.rotation.eulerAngles.y;
            body.rotation = Quaternion.Euler(euler);
        }

        // RPC METHODS

        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
            Debug.Log("[COTTON LID OWNER]: I'm the owner now!");

            // Change variables
            GrabLogicRPC();
        }
        
        [Rpc(SendTo.Owner)]
        private void GrabLogicRPC()
        {   
            // Start opening lid
            if (!isOpen)
            {
                _isGrabbed.Value = true;
                _prevRotation = handler.rotation;
            
                StartOpeningLidRPC();
            }
            // Close lid
            else if (DetachBallFromHand())
            {
                _isOpen.Value = false;
                _isFlamed.Value = false;
                CloseLidRPC();
            
                ForceRelease();
            }
        }
    
        [Rpc(SendTo.ClientsAndHost)]
        private void StartOpeningLidRPC()
        {        
            arrowsUI.SetActive(true);
            AlignBodyToHandlerRotation();
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void OpenLidRPC()
        {        
            //arrowsUI.SetActive(false);
            warningUI.SetActive(true);
            lidRenderer.enabled = false;
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void CloseLidRPC()
        {        
            lidRenderer.enabled = true;
        }
    
        [Rpc(SendTo.ClientsAndHost)]
        private void TurnOffArrowsRPC()
        {        
            arrowsUI.SetActive(false);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void TurnOffWarningRPC()
        {        
            warningUI.SetActive(false);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void SetActiveCollidersStateRPC(bool state)
        {
            foreach (var col in colliders)
            {
                col.enabled = state;
            }
        }
    }
}
