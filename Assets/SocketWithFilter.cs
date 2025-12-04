using Scripts;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketWithFilter : XRSocketInteractor
{
    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        var petri = interactable.transform.GetComponent<PetriController>();
        if (petri is null) return true;

        if (petri.IsGrabbed)
        {
            Debug.Log("[SocketWithFilter]: A grabbed petri is trying to attach, but I'm not gonna let it");
            return false;   
        }

        return base.CanSelect(interactable);
    }
}

