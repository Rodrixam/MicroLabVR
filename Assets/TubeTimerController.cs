using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TubeTimerController : NetworkBehaviour
{
    [SerializeField] private Image pieImage;
    [SerializeField] private TextMeshProUGUI pieText;
        
    private readonly float[] _timeIntervals = {60, 240, 300};

    private readonly NetworkVariable<int> _interval =
        new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> _boilTime =
        new (0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
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
        pieImage.fillMethod = Image.FillMethod.Radial360;
        pieImage.fillOrigin = (int) Image.Origin360.Right;
        pieImage.fillAmount = 1;
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
}
