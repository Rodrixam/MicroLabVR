using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class AsaController : NetworkBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
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
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _isOnline = false;
            
            _timeCounter.OnValueChanged -= ChangeColor;
        }
        
        private void Start()
        {
            // Set point emission color to zero
            _pointMaterial = pointRenderer.material;
            _pointMaterial.SetColor(EmissionColor, Color.red * 0);
            
            // Deactivate feedback elements
            sampleRenderer.enabled = false;
            warningUI.SetActive(false);
        }

        private void Update()
        {
            // Only owner updates
            if (_isOnline && !IsOwner) return;
            
            // Heating
            if (_flameController is not null && IsTiltedCorrectly() && _timeCounter.Value < timeToGlow)
            {
                _timeCounter.Value += Time.deltaTime;
                
                // Sterilization time reached
                if (!_isSterilized.Value && _timeCounter.Value >= timeToGlow)
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

                // Set color according to temperature
                ChangeColor(0,_timeCounter.Value);
            }
            // Cooling
            else if(_timeCounter.Value > 0)
            {
                _timeCounter.Value -= Time.deltaTime;
                
                if (_timeCounter.Value < 0)
                {
                    _timeCounter.Value = 0;
                }
                    
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

        private void ChangeColor(float previous, float current)
        {
            // Already online synchronized by _timeCounter.OnValueChanged 
            var normProgress = current / timeToGlow;
            _pointMaterial.SetColor(EmissionColor, Color.red * normProgress);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {   
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
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
            _isSterilized.Value = false;
            warningUI.SetActive(true);
            StopMicrobeParticlesRPC();
                
            return true;
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void SetMicrobeParticlesColorRPC(Color color)
        {
            var main = microbesParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void StartMicrobeParticlesRPC()
        {
            microbesParticles.Play();
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        public void StopMicrobeParticlesRPC()
        {
            microbesParticles.Stop();
        }
        
        public void Grab()
        {
            RequestGrabServerRPC();

            if (!isSterilized)
            {
                warningUI.SetActive(true);
            }
        }

        public void Drop()
        {
            warningUI.SetActive(false);
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
    }
}
