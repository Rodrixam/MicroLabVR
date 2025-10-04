using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeController : NetworkBehaviour
    {
        [Header("Colliders")]
        [SerializeField] private Collider longCollider;
        [SerializeField] private Collider shortCollider;

        [Header("UI")] 
        [SerializeField] private GameObject clock;
        [SerializeField] private Image pieImage;
        [SerializeField] private TextMeshProUGUI pieText;
        
        private readonly float[] _timeIntervals = {60, 240, 300};

        private readonly NetworkVariable<int> _interval =
                new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _boilTime =
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        
        private bool _isOnline;
        
        private void Start()
        {
            pieImage.type = Image.Type.Filled;
            pieImage.fillMethod = Image.FillMethod.Radial360;
            pieImage.fillOrigin = (int) Image.Origin360.Right;
            pieImage.fillAmount = 1;
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

        public void ReduceCollider()
        {
            longCollider.enabled = false;
            shortCollider.enabled = true;
        }
        public void EnlargeColliders()
        {
            longCollider.enabled = true;
            shortCollider.enabled = false;
        }

        public void AddBoilTime(float time)
        {
            if (_isOnline && !IsOwner) return;
            
            _boilTime.Value += time;

            if (_boilTime.Value >= _timeIntervals[_interval.Value])
            {
                _boilTime.Value -= _timeIntervals[_interval.Value];
                
                _interval.Value++;

                if (_isOnline)
                {
                    ChangeTextRPC(((int) (_timeIntervals[_interval.Value] / 60f)).ToString());
                }
                else
                {
                    pieText.text = ((int) (_timeIntervals[_interval.Value] / 60f)).ToString();   
                }
            }
            
            if (_isOnline)
            {
                ChangeImageRPC(Mathf.Clamp01(1 - _boilTime.Value / _timeIntervals[_interval.Value]));
            }
            else
            {
                pieImage.fillAmount = Mathf.Clamp01(1 - _boilTime.Value / _timeIntervals[_interval.Value]);
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ChangeTextRPC(string text)
        {
            pieText.text = text;
        }
        
        [Rpc(SendTo.ClientsAndHost)]
        private void ChangeImageRPC(float ammount)
        {
            pieImage.fillAmount = ammount;
        }


        public void SetClockActive(bool state)
        {
            clock.SetActive(state);
        }
    }
}