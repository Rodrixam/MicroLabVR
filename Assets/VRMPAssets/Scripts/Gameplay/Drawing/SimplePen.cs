using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using XRMultiplayer;

public class SimplePen : NetworkBehaviour
{
    [SerializeField] PenTrail m_TrailRendererPrefab;
    [SerializeField] Transform m_PenTipTransform;
    [SerializeField] Renderer m_PenTipRenderer;

    NetworkPhysicsInteractable m_NetworkInteractable;

    PenTrail m_CurrentTrailRenderer;

    Color m_CurrentColor;

    List<PenTrail> m_PenTrails = new();
    
    
    private InputDevice rightHand;

    [SerializeField] private GameObject randomBs;
    
    void Awake()
    {
        TryGetComponent(out m_NetworkInteractable);
    }

    void Start()
    {
        SetColor();
        XRINetworkGameManager.Connected.Subscribe(ConnectedToNetworkGame);
        
        if (IsOwner) // only setup input for local player
        {
            rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }
    }

    void OnDestroy()
    {
        XRINetworkGameManager.Connected.Unsubscribe(ConnectedToNetworkGame);
    }

    void ConnectedToNetworkGame(bool connected)
    {
        if (!connected)
        {
            foreach (var trail in m_PenTrails)
            {
                Destroy(trail.gameObject);
            }
        }
    }

    public void ToggleDrawing(bool toggle)
    {
        if (toggle && m_CurrentTrailRenderer == null)
        {
            m_CurrentTrailRenderer = Instantiate(m_TrailRendererPrefab, m_PenTipTransform.position, m_PenTipTransform.rotation, m_PenTipTransform);
            m_CurrentTrailRenderer.SetColor(m_CurrentColor);
        }
        else if (!toggle && m_CurrentTrailRenderer != null)
        {
            m_CurrentTrailRenderer.transform.SetParent(null);
            m_CurrentTrailRenderer.CreateInteractableTrail();
            m_CurrentTrailRenderer = null;
        }

        Instantiate(randomBs, Vector3.up, Quaternion.identity);
    }

    public void SetColor()
    {
        if (XRINetworkGameManager.Instance.GetPlayerByID(m_NetworkInteractable.OwnerClientId, out XRINetworkPlayer player))
        {
            m_CurrentColor = player.playerColor;
        }
        // Failed to get player, might be offline
        else
        {
            m_CurrentColor = XRINetworkGameManager.LocalPlayerColor.Value;
        }
        m_PenTipRenderer.material.color = m_CurrentColor;
    }
    
    void Update()
    {
        Debug.Log("Is owner? " + IsOwner);
        if (!IsOwner) return; // prevent remote players from reading inputs
    
        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed) && pressed)
        {
            Debug.Log("Local Player pressed A button!");
    
            // Send to server so other clients can react
            SendButtonPressServerRpc();
        }
    }
    
    [ServerRpc]
    private void SendButtonPressServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("Server received button press");
        ToggleDrawing(true);
        // notify all clients
        BroadcastButtonPressClientRpc();
    }
    
    [ClientRpc]
    private void BroadcastButtonPressClientRpc(ClientRpcParams rpcParams = default)
    {
        Debug.Log("All clients know a player pressed A");
        // trigger animation, action, etc.
    }

}
