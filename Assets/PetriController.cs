using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using UnityEngine;

public class PetriController : NetworkBehaviour
{
    // References
    
    [SerializeField] private List<Collider> colliders;
    [SerializeField] private PetriLineTask lineTask;
    [SerializeField] private LidPetriController lidPetri;

    public PetriLineTask LineTask => lineTask;
    
    // Network Variables

    private readonly NetworkVariable<bool> _isGrabbed =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool isGrabbed => _isGrabbed.Value;
    
    private readonly NetworkVariable<int> _activityIndex = 
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public int activityIndex => _activityIndex.Value;
    
    
    private bool _isOnline;
    
    // NETWORK METHODS
        
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
    
    // INTERACTION METHODS

    public void Grab()
    {
        Debug.Log("[PETRI CONTROLLER]: I'm grabbing");
        // ------------------------- LOCAL -------------------------
        if (!_isOnline)
        {
            // Set grabbed true
            _isGrabbed.Value = true;
        }
        // ------------------------- ONLINE -------------------------
        else
        {
            // If online, it must request ownership first
            RequestGrabServerRPC(true);
        }
    }

    public void Drop()
    {
        // ------------------------- LOCAL -------------------------
        if (!_isOnline || (_isOnline && IsOwner))
        {
            // Set grabbed false
            _isGrabbed.Value = false;
        }
        else
        {
            Debug.LogError("Petri dropped by not owner... a fix for delay in ownership assignation might be needed");
        }
    }
    
    // ACCESS METHODS

    public void CompleteActivity()
    {
        if (_isOnline && !IsOwner) return;
        
        _activityIndex.Value++;

        switch (_activityIndex.Value)
        {
            case 1:
                lineTask.enabled = false;
                lidPetri.SetDetachable(true);
                
                if (lidPetri.isGrabbed)
                {
                    if (!_isOnline)
                    {
                        SetCollidersState(true);
                        lidPetri.SetCollidersState(false);
                    }
                    else
                    {
                        SetLidPetriCollidersStateRPC(true);
                        SetCollidersStateRPC(false);
                    }
                }
                break;
        }
    }
    
    public void SetCollidersState(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }

    public void SetLidReference(LidPetriController lidController)
    {
        lidPetri = lidController;
    }
    
    // RPC METHODS

    [ServerRpc(RequireOwnership = false)]
    private void RequestGrabServerRPC(bool value, ServerRpcParams rpcParams = default)
    {
        // Change ownership
        ulong grabbingClientId = rpcParams.Receive.SenderClientId;
        GetComponent<NetworkObject>().ChangeOwnership(grabbingClientId);

        // Change variables
        SetGrabbedClientRpc(value);
    }

    [Rpc(SendTo.Owner)]
    private void SetGrabbedClientRpc(bool value)
    {
        // Change grab state
        _isGrabbed.Value = value;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetCollidersStateRPC(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    private void SetLidPetriCollidersStateRPC(bool state)
    {
        lidPetri.SetCollidersState(state);
    }
}
