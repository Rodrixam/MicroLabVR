using System;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Scripts
{
    public class GameController : MonoBehaviour
    {

        [Header("Cotton Balls")] 
        [SerializeField] private GameObject leftHandCottonBall;
        [SerializeField] private GameObject rightHandCottonBall;
        private bool _leftFilled, _rightFilled;

        [Header("Controller Style")] 
        [SerializeField] private GameObject lHandController;
        [SerializeField] private GameObject rHandController, lRealController, rRealController, handsReference, controllerReference;
        private bool _displayHands;

        private void Start()
        {
            _displayHands = false;
            
            rHandController.SetActive(false);
            lHandController.SetActive(false);
            
            rRealController.SetActive(true);
            lRealController.SetActive(true);
            
            controllerReference.SetActive(false);
            handsReference.SetActive(true);
        }

        public bool AttachCottonBallToHand(bool isRight)
        {
            if (isRight)
            {
                if (_rightFilled) return false;
            
                rightHandCottonBall.SetActive(true);
                _rightFilled = true;
            }
            else
            {
                if (_leftFilled) return false;
            
                leftHandCottonBall.SetActive(true);
                _leftFilled = true;
            }

            return true;
        }

        public bool DetachCottonBall(bool isRight)
        {
            if (isRight)
            {
                if (!_rightFilled) return false;
            
                rightHandCottonBall.SetActive(false);
                _rightFilled = false;
            }
            else
            {
                if (!_leftFilled) return false;
            
                leftHandCottonBall.SetActive(false);
                _leftFilled = false;
            }

            return true;
        }

        public void ToggleDisplayHands()
        {
            _displayHands = !_displayHands;
            
            rHandController.SetActive(_displayHands);
            lHandController.SetActive(_displayHands);
            
            rRealController.SetActive(!_displayHands);
            lRealController.SetActive(!_displayHands);
            
            
            controllerReference.SetActive(_displayHands);
            handsReference.SetActive(!_displayHands);
        }
    }
}
