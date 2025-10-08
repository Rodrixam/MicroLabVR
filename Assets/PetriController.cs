using System;
using System.Collections.Generic;
using Scripts;
using Unity.Netcode;
using UnityEngine;

public class PetriController : NetworkBehaviour
{
    // ACTIVITIES:
    // - DRAW LINES
    // - SEPARATE PLATES
    // - SEED MICROBES
    // - PLACE LID
    // - INCUBATE
    private int _activityIndex;

    private bool _lpDetachable;
    private bool _lpIsAttached;

    private bool _isOnline;
    
    // References
    
    [SerializeField] private PetriLineTask lineTask;
    [SerializeField] private LittlePetriController littlePetri;
    
    [SerializeField] private List<Collider> colliders;

    // Network Variables

    private readonly NetworkVariable<bool> _netIsGrabbed =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void OnEnable()
    {
        _activityIndex = 0;
        _lpDetachable = false;
        
        littlePetri.SetCollidersState(false);
    }
    


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

        // ------------------------- LOCAL -------------------------
        if (!_isOnline)
        {
            // Set grabbed true
            _netIsGrabbed.Value = true;
        }
        // ------------------------- ONLINE -------------------------
        else
        {
            // If online, it must request ownership first
            RequestGrabServerRPC(true);
        }

        if (_lpDetachable && _lpIsAttached)
        {
            SetCollidersState(false);
            littlePetri.SetCollidersState(true);
        }
    }

    public void Drop()
    {
        // ------------------------- LOCAL -------------------------
        if (!_isOnline || (_isOnline && IsOwner))
        {
            // Set grabbed false
            _netIsGrabbed.Value = false;
        }
        else
        {
            Debug.LogError("Petri dropped by not owner... a fix for delay in ownership assignation might be needed");
        }
        
        if (_lpDetachable && _lpIsAttached)
        {
            SetCollidersState(true);
            littlePetri.SetCollidersState(false);
        }
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
        _netIsGrabbed.Value = value;
    }
    
    // ACCESS METHODS
    
    public bool IsGrabbed()
    {
        return _netIsGrabbed.Value;
    }

    public void CompleteActivity()
    {
        _activityIndex++;

        switch (_activityIndex)
        {
            case 1:
                lineTask.enabled = false;
                _lpDetachable = true;
                
                if (_netIsGrabbed.Value)
                {
                    littlePetri.SetCollidersState(true);
                }
                break;
        }
    }

    public void DetachLittlePetri()
    {
        _lpIsAttached = false;
    }

    public void AttachLittlePetri()
    {
        _lpIsAttached = true;

        if (!_netIsGrabbed.Value)
        {
            littlePetri.SetCollidersState(false);
        }
    }
    
    public void SetCollidersState(bool state)
    {
        foreach(var col in colliders)
        {
            col.enabled = state;
        }
    }
}
