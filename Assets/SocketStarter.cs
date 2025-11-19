using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketStarter : NetworkBehaviour
{
    public XRSocketInteractor socket;
    public XRGrabInteractable item;

    public override void OnNetworkSpawn()
    {
        // Only server runs attachment
        if (IsServer)
        {
            // Force the interaction so XRIT knows the object is in the socket
            var interactor = socket as IXRSelectInteractor;
            var interactable = item as IXRSelectInteractable;

            socket.interactionManager.SelectEnter(interactor, interactable);
/*
            // Ensure physics state is correct
            var rb = item.GetComponent<Rigidbody>();
            rb.isKinematic = true;//*/
        }
    }
}

