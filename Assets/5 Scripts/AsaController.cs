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

        // References
        private BunsenController _flameController;
        [SerializeField] ParticleSystem particle;

        public TubeController _tubeController;
        
        [Header("Variables")]
        // Variables
        [SerializeField] private float timeToGlow;
        [SerializeField] private float alignMargin;

        private bool _isOnline;
        
        // Network Variables
        private readonly NetworkVariable<float> _timeCounter = new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _isSterilized = new (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        
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
                
                // TUBE INTERACTION
                if (IsAlignedWithTube())
                {
                    Debug.Log("I'm aligned!!");
                }
            }
        }

        private bool IsTiltedCorrectly()
        {
            float z = transform.localEulerAngles.z;

            // Convert 0–360 range into -180–180
            if (z > 180f) z -= 360f;

            if (Mathf.Abs(z - (-45f)) <= 5f) // 5° tolerance
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
        
        private bool IsAlignedWithTube()
        {
            if (_tubeController is null) return false;

            float angle = Vector3.Angle(_tubeController.transform.up, -transform.right);
            if (angle < alignMargin) return true;
            
            return false;
        }
        
        // COLLISION METHODS
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("BurnerFlame"))
            {
                _flameController = other.GetComponentInParent<BunsenController>();   
            }
            else if (other.CompareTag("TubeMouth"))
            {
                _tubeController = other.GetComponentInParent<TubeController>();
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("BurnerFlame"))
            {
                _flameController = null;
            }
            else if (other.CompareTag("TubeMouth"))
            {
                _tubeController = null;
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
        
        // ACCESS METHODS

        public bool IsSterilized()
        {
            return _isSterilized.Value;
        }
    }
}
