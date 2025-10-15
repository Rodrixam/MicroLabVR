using Unity.Netcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class AsaController : NetworkBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        
        [Header("References")]
        // Material
        [SerializeField] private MeshRenderer pointRenderer;
        private Material _pointMaterial;
        
        [SerializeField] private MeshRenderer sampleRenderer;
        private TubePickSampleTask.Samples _sample = TubePickSampleTask.Samples.None;

        // References
        private BunsenController _flameController;
        [SerializeField] ParticleSystem particle;

        
        [Header("Variables")]
        // Variables
        [SerializeField] private float timeToGlow;
        [SerializeField] private float alignMargin;

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
                        sampleRenderer.enabled = false;
                        particle.Play();
                    }

                    if (!_isOnline)
                    {
                        ChangeColor(0,_timeCounter.Value);
                    }
                }
                else if(_timeCounter.Value > 0)
                {
                    _timeCounter.Value -= Time.deltaTime;
                
                    if (_timeCounter.Value < 0)
                    {
                        _timeCounter.Value = 0;
                    }
                    
                    if (!_isOnline)
                    {
                        ChangeColor(0,_timeCounter.Value);
                    }
                }
            }
        }

        private bool IsTiltedCorrectly()
        {
            float tiltAngle = Vector3.Angle(-transform.right, Vector3.up);
            var gap = Mathf.Abs(tiltAngle - 45f);
            if (gap <= alignMargin)
            {
                Debug.Log($"[ASA]: I'm tilting correctly! :D {gap}");
                return true;
            }
            Debug.Log($"[ASA]: I'm not tilting correctly! :C {gap}");
            return false;
        }

        private void ChangeColor(float previous, float current)
        {
            float normProgress = current / timeToGlow;
            _pointMaterial.SetColor(EmissionColor, Color.red * normProgress);
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
        
        // RPC METHODS
        
        [ServerRpc(RequireOwnership = false)]
        private void RequestGrabServerRPC(ServerRpcParams rpcParams = default)
        {   
            // Change ownership
            ulong grabbingClientId = rpcParams.Receive.SenderClientId;
            GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);
        }
        public void ChangeOwnership()
        {
            RequestGrabServerRPC();
        }

        public bool AttachMicrobe(TubePickSampleTask.Samples sample, Material sampleMat)
        {
            if (_sample == TubePickSampleTask.Samples.None)
            {
                _sample = sample;
                sampleRenderer.material  = sampleMat;
                sampleRenderer.enabled = true;
                _isSterilized.Value = false;
                return true;
            }
            return false;
        }
    }
}
