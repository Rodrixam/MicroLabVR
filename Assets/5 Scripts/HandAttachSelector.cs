using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class HandedAttachSelector : MonoBehaviour
    {
        public Transform leftAttach;
        public Transform rightAttach;

        private XRGrabInteractable _grab;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
        }
    
        void OnEnable() => _grab.selectEntered.AddListener(OnGrab);
        void OnDisable() => _grab.selectEntered.RemoveListener(OnGrab);

        private void OnGrab(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRBaseInteractor interactor)
            {
                if (interactor.transform.CompareTag("LeftHand"))
                {
                    _grab.attachTransform = leftAttach;
                }
                else if (interactor.transform.CompareTag("RightHand"))
                {
                    _grab.attachTransform = rightAttach;  
                }
                
                transform.position = _grab.attachTransform.position;
                transform.rotation = _grab.attachTransform.rotation;
            }
        }
    }
}

