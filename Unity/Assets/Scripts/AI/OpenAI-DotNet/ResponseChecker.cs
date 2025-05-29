using UnityEngine;
using OpenAI.Chat;
using System.Text.Json;
using System.Threading.Tasks;
public class ResponseChecker : MonoBehaviour
{
    private ChatClient client;
    // Start is called before the first frame update
    void Start()
    {
        client = new(model: "gpt-4o", apiKey: "sk-5DKTIE0x3IJLsRDbb_K3xA1cFVSPC9wL-48E1wqzikT3BlbkFJFYH2VXtiEgt0ZxGNXni0P4XDZl42MFwOLulZp7bx4A");

    }

    public async Task<int> CheckResponseAsync(string prompt)
    {
        Debug.Log("public async Task<int> CheckResponseAsync(string prompt)");
        ChatCompletion completion = await client.CompleteChatAsync("Bitte überprüfe die folgende Antwort" + prompt + " ob Sie in einen Museums Kontext passt"
        + "antworte bitte immer mit 0 wenn die Antwort nicht in den Museums Kontext passt und mit 1 wenn die Antwort in den Museums Kontext passt");
        string input = completion.Content[0].Text;
        int answer = 0;
        if (int.TryParse(input, out int result))
        {
            Debug.Log($"[ASSISTANT]: Parsed successfully: {result}");
            answer = result;
        }
        else
        {
            Debug.Log($"[ASSISTANT]: Parsing failed.");   
            answer = 1;
        }
        return answer;
    }
}
