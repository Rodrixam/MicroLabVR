using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class CottonLidControllerCopia : NetworkBehaviour
    {
        // Colliders
        [Header("Colliders")] 
        [SerializeField] private List<Collider> colliders;

        // Visual
        [Header("Visual")]
        [SerializeField] private GameObject arrowsUI;
        [SerializeField] private GameObject warningUI;
        [SerializeField] private MeshRenderer lidRenderer;
        
        // Audio
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip corkClip;
        [SerializeField] private AudioClip uncorkClip;
        
        // Transforms
        [Header("Transform")]
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
        
        
        // Initial state references
        private Vector3 _initialPositionBody;
        private Quaternion _initialRotationBody;
        private Vector3 _initialScaleBody;
        
        private Vector3 _initialPositionHandler;
        private Quaternion _initialRotationHandler;
        private Vector3 _initialScaleHandler;


        void Start()
        {
            _initialPositionBody = body.localPosition;
            _initialRotationBody = body.localRotation;
            _initialScaleBody = body.localScale;
            
            _initialPositionHandler = handler.localPosition;
            _initialRotationHandler = handler.localRotation;
            _initialScaleHandler = handler.localScale;
            
            // Deactivate grab collider
            SetActiveCollidersStateSynced(false);
            
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
                arrowsUI.transform.localRotation = Quaternion.Euler(0, 0, -_rotationCounter);

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
                    
                    // Audio
                    PlayLidSoundSynced(true);
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
            var interactors = new List<IXRSelectInteractor>(_interactable.interactorsSelecting);

            // Loop through all interactors currently holding this object
            foreach (var interactor in interactors)
            {
                // Drop() is called internally
                _interactable.interactionManager.SelectExit(interactor, _interactable);
            }
        }

        public void Grab()
        {
            if (!_isOnline)
            {
                // Start opening the lid
                if (!isOpen)
                {
                    _isGrabbed.Value = true;
                    _prevRotation = handler.rotation;
                
                    arrowsUI.SetActive(true);
                    AlignBodyToHandlerRotation();
                }
                // Close the lid
                else if (DetachBallFromHand())
                {
                    _isOpen.Value = false;
                    _isFlamed.Value = false;
                    lidRenderer.enabled = true;
                
                    ForceRelease();
                    PlayLidSoundSynced(false);
                }
            }
            else
            {
                // Logic is inside
                GrabLogicRPC();
                
                // Start opening the lid
                if (!isOpen)
                {
                    _prevRotation = handler.rotation;
                
                    arrowsUI.SetActive(true);
                    AlignBodyToHandlerRotation();
                }
                // Close the lid
                else if (DetachBallFromHand())
                {
                    _isOpen.Value = false;
                    _isFlamed.Value = false;
                    lidRenderer.enabled = true;
                
                    ForceRelease();
                    PlayLidSoundSynced(false);
                }
            }
        }
        [Rpc(SendTo.Owner)]
        private void GrabLogicRPC()
        {   
            // Start opening the lid
            if (!isOpen)
            {
                _isGrabbed.Value = true;
                _prevRotation = handler.rotation;
            
                StartOpeningLidRPC();
            }
            // Close the lid
            else if (DetachBallFromHand())
            {
                _isOpen.Value = false;
                _isFlamed.Value = false;
                CloseLidRPC();
            
                ForceRelease();
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

        // Colliders
        public void SetActiveCollidersStateSynced(bool state)
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
        [Rpc(SendTo.ClientsAndHost)]
        private void SetActiveCollidersStateRPC(bool state)
        {
            foreach (var col in colliders)
            {
                col.enabled = state;
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
            Vector3 euler = body.localRotation.eulerAngles;
            euler.y = handler.localRotation.eulerAngles.y;
            body.rotation = Quaternion.Euler(euler);
        }
        
        // AUDIO STUFF
        private void PlayLidSoundSynced(bool open)
        {
            if (!_isOnline)
            {
                if (open)
                {
                    audioSource.clip = uncorkClip;
                    audioSource.Play();
                }
                else
                {
                    audioSource.clip = corkClip;
                    audioSource.Play();
                }
            }
            else
            {
                PlayLidSoundRPC(open);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayLidSoundRPC(bool open)
        {
            if (open)
            {
                audioSource.clip = uncorkClip;
                audioSource.Play();
            }
            else
            {
                audioSource.clip = corkClip;
                audioSource.Stop();
            }
        }

        // RPC METHODS
    
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
        

        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            SetActiveCollidersStateSynced(false);
            StopRigidBodySynced();
            
            // Transform
            body.localPosition = _initialPositionBody;
            body.localRotation = _initialRotationBody;
            body.localScale = _initialScaleBody;
            
            handler.localPosition = _initialPositionHandler;
            handler.localRotation = _initialRotationHandler;
            handler.localScale = _initialScaleHandler;
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            
            // Components
            ResetComponentsSynced();
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
                
                _handlerRb.linearVelocity = Vector3.zero;
                _handlerRb.angularVelocity = Vector3.zero;
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
            
            _handlerRb.linearVelocity = Vector3.zero;
            _handlerRb.angularVelocity = Vector3.zero;
        }

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _isGrabbed.Value = false;
                _isFlamed.Value = false;
                _isOpen.Value = false;
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
            _isFlamed.Value = false;
            _isOpen.Value = false;
        }
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                arrowsUI.SetActive(false);
                warningUI.SetActive(false);
                
                lidRenderer.enabled = true;
                
                _gameController.DetachCottonBall(true);
                _gameController.DetachCottonBall(false);
                
                _rotationCounter = 0;
            }
            else
            {
                ResetComponentsRPC();
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ResetComponentsRPC()
        {
            arrowsUI.SetActive(false);
            warningUI.SetActive(false);
            
            lidRenderer.enabled = true;
            
            _gameController.DetachCottonBall(true);
            _gameController.DetachCottonBall(false);
            
            _rotationCounter = 0;
        }
    }
}
