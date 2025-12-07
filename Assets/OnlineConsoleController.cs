using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XRMultiplayer;
using Random = UnityEngine.Random;

// ReSharper disable AsyncVoidLambda
// ReSharper disable AsyncVoidMethod
// ReSharper disable StringLiteralTypo
public class OnlineConsoleController : NetworkBehaviour
{
    private XRINetworkGameManager _networkGameManager;

    [Header("UI References")] 
    [SerializeField] private Image iconImage;
    [SerializeField] private Text stateText;
    [SerializeField] private Text roomNameText;
    [SerializeField] private RectTransform selector;
    
    [Header("Library")]
    [SerializeField] private Sprite connectedSprite;
    [SerializeField] private Sprite disconnectedSprite;
    [SerializeField] private Sprite waitingSprite;
    [SerializeField] private Sprite noInternetSprite;
    
    [Header("Lobbies stuff")]
    [SerializeField] private GameObject lobbyHologram;
    [SerializeField] private RectTransform lobbyContent;
    [SerializeField] private GameObject lobbyDisplayPrefab;
    
    
    private string _roomName = String.Empty;
    private Lobby[] _lobbies;
    private int _selectorIndex;

    private bool _isOnline;

    ////////////    NETWORK METHODS    ////////////
    #region NETWORK METHODS
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _isOnline = true;
        
        // State
        iconImage.sprite = connectedSprite;
        stateText.text = "Conectado";
        
        // Room Name
        roomNameText.enabled = true;
        if (!IsOwner)
        {
            _roomName = _lobbies[_selectorIndex].Name;
        }

        roomNameText.text = _roomName;
        
        // Lobby Display
        lobbyHologram.SetActive(false);
        selector.gameObject.SetActive(false);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _isOnline = false;
        
        // State
        iconImage.sprite = disconnectedSprite;
        stateText.text = "Desconectado";
        
        // Room Name
        roomNameText.enabled = false;
        _roomName = String.Empty;
        
        // Lobby Display
        lobbyHologram.SetActive(true);
        _ = RefreshLobbyList();
    }

    async void Start()
    {
        roomNameText.enabled = false;
        
        _networkGameManager = FindAnyObjectByType<XRINetworkGameManager>();
        iconImage.sprite = disconnectedSprite;
        stateText.text = "Desconectado";


        /*if (await InitializeServices())
        {
            await RefreshLobbyList();   
        }//*/
        await RefreshLobbyList();   
    }
    #endregion
    
    ////////////    BUTTON METHODS    ////////////
    #region BUTTON METHODS
    
    // Quick Join
    public void TryQuickJoin()
    {
        if (_networkGameManager is null || _isOnline) return;
        
        StopAllCoroutines();
        StartCoroutine(CheckInternet(() =>
        {
            // Yes Internet
            _networkGameManager.QuickJoinLobby();
            iconImage.sprite = waitingSprite;
            stateText.text = "Conectando...";
        }));
    }
    // Create Room
    public void CreateRoom()
    {
        if (_networkGameManager is null || _isOnline) return;
        
        StopAllCoroutines();
        StartCoroutine(CheckInternet(async () => 
        {
            // Unity Services
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                if (!await InitializeServices())
                {
                    return;
                }
            }
            
            // Name
            _roomName = GetRandomLobbyName();
            
            // Yes Internet
            _networkGameManager.CreateNewLobby(_roomName, false, 3);
            iconImage.sprite = waitingSprite;
            stateText.text = "Creando Sala...";
        }));
    }
    // Join Room
    public void JoinRoom()
    {
        if (_networkGameManager is null || _isOnline) return;
        if (_lobbies.Length < 1) return;
        
        StopAllCoroutines();
        StartCoroutine(CheckInternet(async () =>
        {   
            // Unity Services
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                if (!await InitializeServices())
                {
                    return;
                }
            }
            
            // Yes Internet
            _networkGameManager.JoinLobbySpecific(_lobbies[_selectorIndex]);
            iconImage.sprite = waitingSprite;
            stateText.text = "Uniéndose a sala...";
        }));
    }
    // Disconnect
    public void TryDisconnect()
    {
        if (_networkGameManager is null || !_isOnline) return;

        _networkGameManager.Disconnect();
        iconImage.sprite = waitingSprite;
        stateText.text = "Desconectando...";
    }
    // Refresh Lobbies
    public async void RefreshLobbies()
    {
        await RefreshLobbyList();
    }
    
    // Next Room
    public void SelectNextRoom()
    {
        if (_lobbies.Length < 2) return;
        
        _selectorIndex++;
        if (_selectorIndex > _lobbies.Length - 1)
        {
            _selectorIndex = 0;
        }

        UpdateSelectorPosition();
    }
    public void SelectPrevRoom()
    {
        if (_lobbies.Length < 2) return;
        
        _selectorIndex--;
        if (_selectorIndex < 0)
        {
            _selectorIndex = _lobbies.Length - 1;
        }

        UpdateSelectorPosition();
    }
    private void UpdateSelectorPosition()
    {
        if (_selectorIndex > 5)
        {
            selector.anchoredPosition = new Vector2(0, -165);
            lobbyContent.anchoredPosition = new Vector2(0, 30 * _selectorIndex);
        }
        else
        {
            selector.anchoredPosition = new Vector2(0, -15 - 30 * _selectorIndex);
            lobbyContent.anchoredPosition = Vector2.zero;
        }
    }
    #endregion
    
    ////////////    CONNECTION METHODS    ////////////
    
    private IEnumerator CheckInternet(Action callback)
    {
        UnityWebRequest request = new UnityWebRequest("https://www.google.com");
        request.method = UnityWebRequest.kHttpVerbHEAD; // fastest

        yield return request.SendWebRequest();

        bool ok = !(request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError);
        
        // No Internet
        if (!ok)
        {
            iconImage.sprite = noInternetSprite;
            stateText.text = "Sin internet";
            yield break;
        }
        
        callback();
    }
    
    private static bool _isInitializing; // protects against double calls
    private async Task<bool> InitializeServices()
    {
        // Prevent double initialization
        if (_isInitializing) return false;
        _isInitializing = true;

        // Initialize Unity Services (safe check)
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        // Sign in only if needed
        if (!AuthenticationService.Instance.IsSignedIn &&
            !AuthenticationService.Instance.IsAuthorized)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in.");
                _isInitializing = false;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Sign-in failed: " + e);
            }
        }
        else
        {
            Debug.Log("Already signed in.");
        }

        _isInitializing = false;
        return false;
    }
    
    ////////////    LOBBY METHODS    ////////////
    
    private async Task RefreshLobbyList()
    {
        // Get current lobbies
        var lobbies = await GetAvailableLobbies();
        _lobbies = lobbies.ToArray();

        // Remove previous
        for(int i = 0; i < lobbyContent.childCount; i++)
        {
            Destroy(lobbyContent.GetChild(i).gameObject);
        }

        // Update selector state
        _selectorIndex = 0;
        if (_lobbies.Length < 1)
        {
            selector.gameObject.SetActive(false);
            return;
        }
        selector.gameObject.SetActive(true);
        UpdateSelectorPosition();
        
        // Create lobby displayers
        foreach (var lobby in _lobbies)
        {
            var lobbyUI = Instantiate(lobbyDisplayPrefab, lobbyContent);
            var controller = lobbyUI.GetComponent<LobbyDisplayController>();
            controller.SetName(lobby.Name);
            controller.SetNum(lobby.Players.Count);
        }
    }
    private async Task<List<Lobby>> GetAvailableLobbies()
    {
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(_options);
            return response.Results;
        }
        catch (Exception e)
        {
            Debug.LogError("Error getting lobbies: " + e);
            return new List<Lobby>();
        }
    }
    private readonly QueryLobbiesOptions _options = new ()
    {
        Count = 20,
        Filters = new List<QueryFilter>()
        {
            new (
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0"
            )
        },
        Order = new List<QueryOrder>
        {
            new (
                field: QueryOrder.FieldOptions.Created,
                asc: false ) 
        }
    };
    
    
    //////////////// LOBBY NAME GENERATOR ////////////////
    private string GetRandomLobbyName()
    {
        var n = Random.Range(0, _lobbyNouns.Length);
        var a = Random.Range(0, _lobbyAdjectives.Length);
        return _lobbyNouns[n] + " " + _lobbyAdjectives[a];
    }
    private readonly string[] _lobbyNouns = 
    {
        "Espora", "Plásmido", "Cultivo", "Colonia", "Biofilm", 
        "Flagelo", "Núcleo", "Citoplasma", "Membrana", "Proteína", 
        "Enzima", "Cápside", "Núcleoide", "Vector", "Microtubo",
        "Lisosoma", "Vacuola", "Microbioma", "Bacteria", "Virus"
            
    };
    private readonly string[] _lobbyAdjectives =
    {
        "Fluorescente", "Radiante", "Inerte", "Virulento", "Luminiscente",
        "Mutante", "Resistente", "Estéril", "Volátil", "Inestable",
        "Letal", "Encapsulado", "Replicante", "Polimórfico", "Termófilo",
        "Anaeróbico", "Extremo", "Oscilante", "Microscópico", "Viscoso"
    };
}
