using System;
using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    public System.Action<Collider> onTriggerEntered;
    public System.Action<Collider> onTriggerExited;

    void OnTriggerEnter(Collider other)
    {
        onTriggerEntered?.Invoke(other);
    }

    void OnTriggerExit(Collider other)
    {
        onTriggerExited?.Invoke(other);
    }
}
