using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class AsaController : NetworkBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        [SerializeField] private XRGrabInteractable interactable;
        
        [Header("Renderers & UI")]
        [SerializeField] private MeshRenderer pointRenderer;
        [SerializeField] private MeshRenderer sampleRenderer;
        [SerializeField] private GameObject warningUI;
        
        private Material _pointMaterial;
        
        [Header("Particles")]
        [SerializeField] ParticleSystem victoryParticles;
        [SerializeField] ParticleSystem microbesParticles;
        
        [Header("Variables")]
        [SerializeField] private float timeToGlow;
        [SerializeField] private float alignMargin;
        private BunsenController _flameController;
        
        [Header("Sample references")]
        private TubePickSampleTask.Samples _sample = TubePickSampleTask.Samples.None;
        public TubePickSampleTask.Samples sample => _sample;
        
        private int _sampleInterval;
        public int sampleInterval => _sampleInterval;
        
        // Initial state references
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;
        
        private bool _isOnline;
        
        // Network Variables
        private readonly NetworkVariable<float> _timeCounter = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isSterilized = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool isSterilized => _isSterilized.Value;
        
        // NETWORK BEHAVIOURS METHODS
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isOnline = true;
            
            _timeCounter.OnValueChanged += ChangeColor;
            _isSterilized.OnValueChanged += UpdateWarningUI;
        }
        public override void OnNetworkDespawn()
        {
            _isOnline = false;
            
            _timeCounter.OnValueChanged -= ChangeColor;
            _isSterilized.OnValueChanged -= UpdateWarningUI;
            base.OnNetworkDespawn();
        }

        private void Awake()
        {
            interactable.selectEntered.AddListener(Grab);
        }
        public override void OnDestroy()
        {
            interactable.selectEntered.RemoveListener(Grab);
            base.OnDestroy();
        }

        private void Start()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
            
            // Set point emission color to zero
            _pointMaterial = pointRenderer.material;
            _pointMaterial.SetColor(EmissionColor, Color.red * 0);
            
            // Deactivate feedback elements
            sampleRenderer.enabled = false;
        }

        private void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
            
            // Heating
            if (_flameController is not null && IsTiltedCorrectly() && _timeCounter.Value < timeToGlow + 2)
            {
                _timeCounter.Value += Time.deltaTime;
                
                // Sterilization time reached
                if (!_isSterilized.Value && _timeCounter.Value >= timeToGlow)
                {
                    SterilizationSynced();
                }
            }
            // Cooling
            else if(_timeCounter.Value > 0)
            {
                _timeCounter.Value -= Time.deltaTime;
                
                if (_timeCounter.Value < 0)
                {
                    _timeCounter.Value = 0;
                }
            }

            if (!_isOnline)
            {
                // Set color according to temperature
                ChangeColor(0,_timeCounter.Value);
            }
        }

        private bool IsTiltedCorrectly()
        {
            var tiltAngle = Vector3.Angle(-transform.right, Vector3.up);
            var gap = Mathf.Abs(tiltAngle - 45f);
            
            return gap <= alignMargin;
        }

        // Already online synchronized by _timeCounter.OnValueChanged 
        private void ChangeColor(float previous, float current)
        {
            var normProgress = current / 7f;
            _pointMaterial.SetColor(EmissionColor, Color.red * normProgress);
        }
        
        // Already online synchronized by _sterilized.OnValueChanged
        private void UpdateWarningUI(bool previous, bool current)
        {
            warningUI.SetActive(current);
        }


        private void SterilizationSynced()
        {
            // Set sterilization as true
            _isSterilized.Value = true;
            
            // Feedback
            if (!_isOnline)
            {
                warningUI.SetActive(false);
                        
                // Clean sample
                sampleRenderer.enabled = false;
                _sample = TubePickSampleTask.Samples.None;
                        
                // Feedback particles
                victoryParticles.Play();
            }
            else
            {
                SterilizationRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SterilizationRPC()
        {
            warningUI.SetActive(false);
                        
            // Clean sample
            sampleRenderer.enabled = false;
            _sample = TubePickSampleTask.Samples.None;
                        
            // Feedback particles
            victoryParticles.Play();
        }
        
        // PUBLIC METHODS
        
        // already synced in TubePickSampleTask
        public bool AttachMicrobe(TubePickSampleTask.Samples newSample, int interval, Material sampleMat)
        {
            // Skip if sample reference is already set
            if (_sample != TubePickSampleTask.Samples.None) return false;

            // Set sample and time interval
            _sample = newSample;
            _sampleInterval = interval;
                
            // Enable sample renderer
            sampleRenderer.material  = sampleMat;
            sampleRenderer.enabled = true;   
                
            // Set sterilization as false
            if (!_isOnline)
            {
                _isSterilized.Value = false;
                warningUI.SetActive(true);
            }
            else
            {
                SetSterilizationStateRPC(false);
            }
                
            // Turn off tracking particles
            SetMicrobeParticlesState(false);
                
            return true;
        }
        public void SetMicrobeParticlesState(bool state)
        {
            if (state)
            {
                microbesParticles.Play();   
            }
            else
            {
                microbesParticles.Stop();
            }
        }
        
        // Microbe (sample) Renderer
        [Rpc(SendTo.ClientsAndHost)]
        public void SetMicrobeParticlesColorRPC(Color color)
        {
            var main = microbesParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }

        [Rpc(SendTo.Owner)]
        private void SetSterilizationStateRPC(bool state)
        {
            _isSterilized.Value = state;
        }
        
        // INTERACTION METHODS
        
        private void Grab(SelectEnterEventArgs args)
        {
            if (!IsPickedByHand(args)) return;
            
            RequestGrabServerRPC();
        }
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {   
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
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

        
        // COLLISION METHODS
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("BurnerFlame"))
            {
                _flameController = other.GetComponentInParent<BunsenController>();   
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("BurnerFlame"))
            {
                _flameController = null;
            }
        }
        
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditions()
        {
            // Physics
            StopRigidBodySynced();
            
            // Transform
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
            
            // Controller Variables
            ResetNetworkVariablesSynced();
            
            //Components and References
            ResetNetworkComponentsSynced();
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

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _timeCounter.Value = 0;
                _isSterilized.Value = false;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _timeCounter.Value = 0;
            _isSterilized.Value = false;
        }
        
        
        // Components and stuff
        private void ResetNetworkComponentsSynced()
        {
            if (!_isOnline)
            {
                // Set point emission color to zero
                _pointMaterial.SetColor(EmissionColor, Color.red * 0);
            
                // Deactivate feedback elements
                sampleRenderer.enabled = false;
                warningUI.SetActive(true);
                
                // Stop Particles
                victoryParticles.Stop();
                microbesParticles.Stop();
                
                // Reset references
                _flameController = null;
                _sample = TubePickSampleTask.Samples.None;
                _sampleInterval = 0;
            }
            else
            {
                ResetNetworkComponentsRPC();
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ResetNetworkComponentsRPC()
        {
            // Set point emission color to zero
            _pointMaterial.SetColor(EmissionColor, Color.red * 0);
            
            // Deactivate feedback elements
            sampleRenderer.enabled = false;
            warningUI.SetActive(true);
                
            // Stop Particles
            victoryParticles.Stop();
            microbesParticles.Stop();
                
            // Reset references
            _flameController = null;
            _sample = TubePickSampleTask.Samples.None;
            _sampleInterval = 0;
        }
    }
}
