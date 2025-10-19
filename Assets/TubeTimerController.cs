using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TubeTimerController : NetworkBehaviour
{
    [SerializeField] private Image pieImage;
    [SerializeField] private TextMeshProUGUI pieText;
        
    private readonly float[] _timeIntervals = {60, 60, 60};
    private readonly int[] _minutes = {1, 5, 10};

    private readonly NetworkVariable<int> _interval =
        new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> _boilTime =
        new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public int interval => _interval.Value;
    public float boilTime => _boilTime.Value;
    
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
        pieImage.type = Image.Type.Filled;
        pieImage.fillClockwise = false;
        pieImage.fillMethod = Image.FillMethod.Radial360;
        pieImage.fillOrigin = (int) Image.Origin360.Top;
        pieImage.fillAmount = 1;
    }
    
    public void AddBoilTime(float time)
    {
        if ((_isOnline && !IsOwner) || _interval.Value > 2) return;
            
        _boilTime.Value += time;

        if (_boilTime.Value >= _timeIntervals[_interval.Value])
        {
            _boilTime.Value -= _timeIntervals[_interval.Value];
                
            _interval.Value++;

            if (_isOnline)
            {
                ChangeTextRPC(_minutes[_interval.Value - 1].ToString());
            }
            else
            {
                pieText.text = _minutes[_interval.Value - 1].ToString();   
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
}
