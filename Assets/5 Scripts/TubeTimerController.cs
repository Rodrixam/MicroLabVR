using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class TubeTimerController : NetworkBehaviour
    {
        // References
        [SerializeField] private Image pieImage;
        [SerializeField] private TextMeshProUGUI pieText;
        [SerializeField] private GameObject visuals;
        [SerializeField] private AudioSource bellSound;
        
        // Readonly values
        private readonly float[] _timeIntervals = {60, 60, 60};
        private readonly int[] _minutes = {1, 5, 10};

        // Variables
        private readonly NetworkVariable<int> _interval =
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<float> _boilTime =
            new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public int interval => _interval.Value;
    
        private bool _isOnline;
    
  
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
    
        private void Start()
        {
            // Configuration for pie image to fill
            pieImage.type = Image.Type.Filled;
            pieImage.fillClockwise = false;
            pieImage.fillMethod = Image.FillMethod.Radial360;
            pieImage.fillOrigin = (int) Image.Origin360.Top;
            pieImage.fillAmount = 1;
        }
    
        public void AddBoilTimeSynced(float time)
        {
            // Only owner updates
            // Only updates if the interval isn't greater than the last one
            if ((_isOnline && !IsOwner) || _interval.Value > 2) return;
            
            // Add boil time
            _boilTime.Value += time;

            // Next time interval reached
            if (_boilTime.Value >= _timeIntervals[_interval.Value])
            {
                // Time counter resets
                _boilTime.Value -= _timeIntervals[_interval.Value];
                _interval.Value++;
                
                // Feedback
                if (!_isOnline)
                {
                    pieText.text = _minutes[_interval.Value - 1].ToString();
                    bellSound.Play();
                }
                else
                {
                    ChangeTextRPC(_minutes[_interval.Value - 1].ToString());
                    PlayBellRPC();
                }
            }
            
            // Update pie image
            if (!_isOnline)
            {
                pieImage.fillAmount = Mathf.Clamp01(1 - _boilTime.Value / _timeIntervals[_interval.Value]);
            }
            else
            {
                ChangeImageRPC(Mathf.Clamp01(1 - _boilTime.Value / _timeIntervals[_interval.Value]));
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ChangeTextRPC(string text)
        {
            pieText.text = text;
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void ChangeImageRPC(float amount)
        {
            pieImage.fillAmount = amount;
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void PlayBellRPC()
        {
            bellSound.Play();
        }

        private void SetVisualsActiveStateSynced(bool state)
        {
            if (!_isOnline)
            {
                visuals.SetActive(state);
            }
            else
            {
                SetVisualsActiveStateRPC(state);
            }
        }
        [Rpc(SendTo.ClientsAndHost)]
        private void SetVisualsActiveStateRPC(bool state)
        {
            visuals.SetActive(state);
        }
        
        // ------------------- RESTORATION -------------------
        
        public void RestoreToStartConditionsSynced()
        {
            // Controller Variables
            ResetNetworkVariablesSynced();
            
            // Components
            ResetComponentsSynced();
        }

        // Network Variables
        private void ResetNetworkVariablesSynced()
        {
            if (!_isOnline)
            {
                _interval.Value = 0;
                _boilTime.Value = 0;
            }
            else
            {
                ResetNetworkVariablesRPC();
            }
        }
        [Rpc(SendTo.Owner)]
        private void ResetNetworkVariablesRPC()
        {
            _interval.Value = 0;
            _boilTime.Value = 0;
        }
        
        // Components
        private void ResetComponentsSynced()
        {
            if (!_isOnline)
            {
                pieImage.fillAmount = 1;
                pieText.text = "0";
            }
            else
            {
                ChangeImageRPC(1);
                ChangeTextRPC("0");
            }

            SetVisualsActiveStateSynced(true);
        }
    }
}
