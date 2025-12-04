using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TubeRackController : NetworkBehaviour
{
    [SerializeField] private List<XRSocketInteractor> tubeSockets;
    [SerializeField] private List<XRSocketInteractor> asaSockets;
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
        // Register listeners for tube sockets
        for (int i = 0; i < tubeSockets.Count; i++)
        {
            int index = i;
            tubeSockets[index].selectEntered.AddListener((_) => OnTubeSocketSelectedSynced(index));
            tubeSockets[index].selectExited.AddListener((_) => OnTubeSocketRemovedSynced(index));
        }
        // Register listeners for asa sockets
        for (int i = 0; i < asaSockets.Count; i++)
        {
            int index = i;
            asaSockets[index].selectEntered.AddListener((_) => OnAsaSocketSelectedSynced(index));
            asaSockets[index].selectExited.AddListener((_) => OnAsaSocketRemovedSynced(index));
        }

        StartSynchronize();
    }

    private void StartSynchronize()
    {
        for (int i = 0; i < tubeSockets.Count; i++)
        {
            if (tubeSockets[i].hasSelection)
            {
                OnTubeSocketSelectedSynced(i);
            }
        }
        for (int i = 0; i < asaSockets.Count; i++)
        {
            if (asaSockets[i].hasSelection)
            {
                OnAsaSocketSelectedSynced(i);
            }
        }
    }

    // TUBE SOCKET SELECTED
    private void OnTubeSocketSelectedSynced(int index)
    {
        if (!_isOnline)
        {
            asaSockets[index].gameObject.SetActive(false); 
        }
        else
        {
            OnTubeSocketSelectedRPC(index);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OnTubeSocketSelectedRPC(int index)
    {
        asaSockets[index].gameObject.SetActive(false); 
    }
    // ASA SOCKET SELECTED
    private void OnAsaSocketSelectedSynced(int index)
    {
        if (!_isOnline)
        {
            tubeSockets[index].gameObject.SetActive(false); 
        }
        else
        {
            OnAsaSocketSelectedRPC(index);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OnAsaSocketSelectedRPC(int index)
    {
        tubeSockets[index].gameObject.SetActive(false); 
    }
    
    // TUBE SOCKET REMOVED
    private void OnTubeSocketRemovedSynced(int index)
    {
        if (!_isOnline)
        {
            asaSockets[index].gameObject.SetActive(true); 
        }
        else
        {
            OnTubeSocketRemovedRPC(index);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OnTubeSocketRemovedRPC(int index)
    {
        asaSockets[index].gameObject.SetActive(true); 
    }
    // ASA SOCKET REMOVED
    private void OnAsaSocketRemovedSynced(int index)
    {
        if (!_isOnline)
        {
            tubeSockets[index].gameObject.SetActive(true); 
        }
        else
        {
            OnAsaSocketRemovedRPC(index);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OnAsaSocketRemovedRPC(int index)
    {
        tubeSockets[index].gameObject.SetActive(true); 
    }
    
    /////////////      RESTORATION     /////////////
    public void RestoreToStartConditions()
    {
        if (!_isOnline)
        {
            SetEverythingActive();
            StartSynchronize();
        }
        else
        {
            RestoreToStartConditionsRPC();
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void RestoreToStartConditionsRPC()
    {
        SetEverythingActive();
        StartSynchronize();
    }
    
    
    private void SetEverythingActive()
    {
        foreach (var t in tubeSockets)
        {
            t.gameObject.SetActive(true);
        }
        foreach (var t in asaSockets)
        {
            t.gameObject.SetActive(true);
        }
    }
}
