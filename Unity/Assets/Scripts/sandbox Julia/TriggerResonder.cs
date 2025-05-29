using UnityEngine;

public class TriggerResponder : MonoBehaviour
{
    public PlayerTriggerDetector triggerDetector;
    public WebSocketClient webSocketClient;
    
    public string contextPrompt;
    public Renderer contextRenderer;
    public Color highlightColor = Color.white;
    public Color defaultColor = Color.gray; // oder was auch immer dein Startton ist

    private void OnEnable()
    {
        if (triggerDetector != null)
        {
            triggerDetector.OnPlayerEnter += HandlePlayerEnter;
            triggerDetector.OnPlayerExit += HandlePlayerExit;
        }
    }

    private void OnDisable()
    {
        if (triggerDetector != null)
        {
            triggerDetector.OnPlayerEnter -= HandlePlayerEnter;
            triggerDetector.OnPlayerExit -= HandlePlayerExit;
        }
    }

    private void HandlePlayerEnter()
    {
        Debug.Log("Player entered the area!");
        //evtl warte bis player etwas sagt
        if (contextRenderer != null)
        {
        contextRenderer.material.color = highlightColor;
        }
        
        _ = webSocketClient.SendTextSystem("Ihr steht vor dem Kunstwerk" + contextPrompt + "Warte bis Dir eine Frage gestellt wird und antworte darauf.");
        Debug.Log("Starting audio recording...");
    }
    


    private void HandlePlayerExit()
    {
        Debug.Log("Player exited the area!");
        if (contextRenderer != null)
        {
        contextRenderer.material.color = defaultColor;
        }
        if (webSocketClient != null)
        {
             _ = webSocketClient.SendInterrupt();
            Debug.LogWarning("Interrupting done");
            //_ = webSocketClient.SendTextUser("Der Besucher läuft weiter. Antworte mit einem freundlichen Satz");
            Debug.Log("Sending 'nächstes Gemälde' message.");
        }
        else
        {
            Debug.LogError("WebSocketClient is null");
        }
    }
}





