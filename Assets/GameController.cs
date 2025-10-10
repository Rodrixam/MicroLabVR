using UnityEngine;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject leftHandCottonBall, rightHandCottonBall;
    private bool _leftFilled, _rightFilled;
    
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
}
