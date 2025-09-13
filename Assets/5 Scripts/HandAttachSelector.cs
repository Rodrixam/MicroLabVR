using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class HandedAttachSelector : MonoBehaviour
{
    public Transform leftAttach;
    public Transform rightAttach;

    private XRGrabInteractable grab;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
    }
    
    void OnEnable() => grab.selectEntered.AddListener(OnGrab);
    void OnDisable() => grab.selectEntered.RemoveListener(OnGrab);

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRBaseInteractor interactor)
        {
            if (interactor.transform.CompareTag("LeftHand"))
            {
                grab.attachTransform = leftAttach;
            }
            else if (interactor.transform.CompareTag("RightHand"))
            {
                grab.attachTransform = rightAttach;  
            }
        }
    }
}

