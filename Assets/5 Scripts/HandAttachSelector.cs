using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class HandlerSelector : MonoBehaviour
    {
        public Transform leftHandler;
        public Transform rightHandler;

        private XRGrabInteractable _grab;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }
    
        void OnEnable() => _grab.selectEntered.AddListener(OnGrab);
        void OnDisable() => _grab.selectEntered.RemoveListener(OnGrab);

        private void OnGrab(SelectEnterEventArgs args)
        {
            Debug.Log("Grab is called");
            if (args.interactorObject is XRBaseInteractor interactor)
            {
                // Select left or right handler
                Transform newHandler = _grab.attachTransform;
                
                if (interactor.transform.CompareTag("LeftHand"))
                    newHandler = leftHandler;
                else if (interactor.transform.CompareTag("RightHand"))
                    newHandler = rightHandler;  

                // Attach to the correct handler
                if (_grab.attachTransform == newHandler) return;
                _grab.attachTransform = newHandler;
                transform.position = Vector3.zero;//newHandler.position - newHandler.localPosition;
                transform.rotation = newHandler.rotation * Quaternion.Inverse(newHandler.localRotation);
            }
        }
    }
    /*
    // Update transform
    if (newHandler is null) return;
    transform.position = newHandler.position;
    transform.rotation = newHandler.rotation;//*/
}

