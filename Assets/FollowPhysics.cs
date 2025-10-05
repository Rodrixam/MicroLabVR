using UnityEngine;

public class FollowPhysics : MonoBehaviour
{
    public Transform target;
    private Rigidbody _rigidbody;
    
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        _rigidbody.MovePosition((target.transform.position));
    }
}
