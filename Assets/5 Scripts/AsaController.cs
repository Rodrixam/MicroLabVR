using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class AsaController : NetworkBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        [Header("Renderers & UI")]
        
        [SerializeField] private MeshRenderer pointRenderer;
        private Material _pointMaterial;
        
        [SerializeField] private MeshRenderer sampleRenderer;

        [SerializeField] private GameObject warningUI;
        
        [Header("Particles")]
        [SerializeField] ParticleSystem victoryParticles;
        [SerializeField] ParticleSystem microbesParticles;
        
        [Header("Variables")]
        
        [SerializeField] private float timeToGlow;
        [SerializeField] private float alignMargin;
        private BunsenController _flameController;
        
        private TubePickSampleTask.Samples _sample = TubePickSampleTask.Samples.None;
        public TubePickSampleTask.Samples sample => _sample;
        
        private float _sampleTime = 0;
        public float sampleTime =>  _sampleTime;
        
        private bool _isOnline;
        
        // Network Variables
        private readonly NetworkVariable<float> _timeCounter = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isSterilized = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool isSterilized => _isSterilized.Value;
        
        // METHODS
        
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
        
        void Start()
        {
            _pointMaterial = pointRenderer.material;
            _pointMaterial.SetColor(EmissionColor, Color.red * 0);
            
            sampleRenderer.enabled = false;
            warningUI.SetActive(false);
        }

        void Update()
        {
            if (!_isOnline || (_isOnline && IsOwner))
            {
                // FLAME INTERACTION
                if (_flameController is not null && IsTiltedCorrectly() && _timeCounter.Value < timeToGlow)
                {
                    _timeCounter.Value += Time.deltaTime;
                
                    if (!_isSterilized.Value && _timeCounter.Value >= timeToGlow)
                    {
                        _isSterilized.Value = true;
                        warningUI.SetActive(false);
                        
                        sampleRenderer.enabled = false;
                        _sample = TubePickSampleTask.Samples.None;
                        _sampleTime = 0;
                        
                        victoryParticles.Play();
                    }

                    ChangeColor(0,_timeCounter.Value);
                }
                else if(_timeCounter.Value > 0)
                {
                    _timeCounter.Value -= Time.deltaTime;
                
                    if (_timeCounter.Value < 0)
                    {
                        _timeCounter.Value = 0;
                    }
                    
                    ChangeColor(0,_timeCounter.Value);
                }
            }
        }

        private bool IsTiltedCorrectly()
        {
            float tiltAngle = Vector3.Angle(-transform.right, Vector3.up);
            var gap = Mathf.Abs(tiltAngle - 45f);
            if (gap <= alignMargin)
            {
                return true;
            }
            return false;
        }

        private void ChangeColor(float previous, float current)
        {
            float normProgress = current / timeToGlow;
            _pointMaterial.SetColor(EmissionColor, Color.red * normProgress);
        }

        public void SetMicrobeParticlesColor(Color color)
        {
            var main = microbesParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }
        public void StartMicrobeParticles()
        {
            microbesParticles.Play();
        }
        public void StopMicrobeParticles()
        {
            microbesParticles.Stop();
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
        
        // INTERACTION METHODS
        
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
        
        // RPC METHODS
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {   
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }

        public bool AttachMicrobe(TubePickSampleTask.Samples sample, float sampleTime, Material sampleMat)
        {
            if (_sample == TubePickSampleTask.Samples.None)
            {
                _sample = sample;
                _sampleTime = sampleTime;
                
                sampleRenderer.material  = sampleMat;
                sampleRenderer.enabled = true;
                
                _isSterilized.Value = false;
                warningUI.SetActive(true);
                StopMicrobeParticles();
                
                return true;
            }
            return false;
        }
    }
}
