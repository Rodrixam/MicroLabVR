using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

// ReSharper disable StringLiteralTypo
public class OnlineConsoleController : NetworkBehaviour
{
    private XRINetworkGameManager _networkGameManager;
    
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Text stateText;
    
    [Header("Library")]
    [SerializeField] private Sprite connectedSprite;
    [SerializeField] private Sprite disconnectedSprite;
    [SerializeField] private Sprite waitingSprite;

    private bool _isOnline;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isOnline = true;
        
        iconImage.sprite = connectedSprite;
        stateText.text = "Conectado";
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isOnline = false;
        
        iconImage.sprite = disconnectedSprite;
        stateText.text = "Desconectado";
    }

    void Start()
    {
        _networkGameManager = FindAnyObjectByType<XRINetworkGameManager>();
        iconImage.sprite = disconnectedSprite;
        stateText.text = "Desconectado";
    }

    public void CreateRoom()
    {
        _networkGameManager.CreateNewLobby();
    }
    
    public void TryQuickJoin()
    {
        if (_networkGameManager is null || _isOnline) return;

        _networkGameManager.QuickJoinLobby();
        iconImage.sprite = waitingSprite;
        stateText.text = "Conectando...";
    }
    
    public void TryDisconnect()
    {
        if (_networkGameManager is null || !_isOnline) return;

        _networkGameManager.Disconnect();
        iconImage.sprite = waitingSprite;
        stateText.text = "Desconectando...";
    }
}
