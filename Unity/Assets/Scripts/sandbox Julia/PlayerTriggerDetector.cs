using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class PlayerTriggerDetector : MonoBehaviour
{
    [Tooltip("Only objects with this tag will trigger the events.")]
    public string playerTag = "Player";

    // Public events you can subscribe to
    public event Action OnPlayerEnter;
    public event Action OnPlayerExit;

    private void Start()
    {
        Debug.Log("Player Start");
        Reset();
    }

    private void Reset()
    {
        // Automatically set the collider as a trigger in the editor
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger enter");
        if (other.CompareTag(playerTag))
        {
            OnPlayerEnter?.Invoke();
            
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger exit");
        if (other.CompareTag(playerTag))
        {
            OnPlayerExit?.Invoke();
            
        }
    }
}
