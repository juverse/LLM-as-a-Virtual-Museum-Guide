using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;
using SendingOpenAI;

namespace SendingOpenAI
{
    [Serializable]
    public class Request
    {
        public string model;
        public string[] modalities;
        public AudioConfig audio;
        public Message[] messages;
    }

    [Serializable]
    public class AudioConfig
    {
        public string voice;
        public string format;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }
}

namespace ReceivingOpenAI
{
    [System.Serializable]
    public class Response
    {
        public string id;
        public string @object; // Use @ to escape the keyword
        public long created;
        public string model;
        public Choice[] choices;
        public Usage usage;
        public string system_fingerprint;
    }

    [System.Serializable]
    public class Choice
    {
        public int index;
        public Message message;
        public string finish_reason;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
        public string refusal;
        public Audio audio;
    }

    [System.Serializable]
    public class Audio
    {
        public string id;
        public string data;
        public long expires_at;
        public string transcript;
    }

    [System.Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
        public PromptTokensDetails prompt_tokens_details;
        public CompletionTokensDetails completion_tokens_details;
    }

    [System.Serializable]
    public class PromptTokensDetails
    {
        public int cached_tokens;
        public int text_tokens;
        public int image_tokens;
        public int audio_tokens;
    }

    [System.Serializable]
    public class CompletionTokensDetails
    {
        public int reasoning_tokens;
        public int text_tokens;
        public int audio_tokens;
    }
}
public class OpenAISpeechAPI : MonoBehaviour
{
    
    private string apiKey = "";  // Replace with your OpenAI API key
    
    [SerializeField]
    private string preprompt = "";

    private void Start()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    }
    public IEnumerator TestAudioReceiving()
    {
        StartCoroutine(GenerateAudioResponse("Welche Farbe hat der Himmel?"));  // Send text to generate speech response
        yield return null;
    }

    public IEnumerator SendAudioToAPI(string filePath)
    {
        string apiUrl = "https://api.openai.com/v1/audio/transcriptions"; // Whisper endpoint for transcription
        byte[] audioData = File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "audioInput.wav", "audio/wav");
        form.AddField("model", "whisper-1"); // Whisper model for transcription

        UnityWebRequest request = UnityWebRequest.Post(apiUrl, form);
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;  // Transcription response
            Debug.Log("Transcription: " + responseText);
            StartCoroutine(GenerateAudioResponse(preprompt+responseText));  // Send text to generate speech response
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }

    public IEnumerator GenerateAudioResponse(string textPrompt)
    {
        //string apiUrl = "https://api.openai.com/v1/audio/completions"; // Endpoint for audio completion
        string apiUrl = "https://api.openai.com/v1/chat/completions"; // Endpoint for audio completion

        SendingOpenAI.Message userMessage = new SendingOpenAI.Message
        {
            role = "user",
            content = textPrompt
        };

        SendingOpenAI.Request config = new Request
        {
            model = "gpt-4o-audio-preview",
            modalities = new string[] { "text", "audio" },
            audio = new AudioConfig
            {
                voice = "alloy",
                format = "wav"
            },
            messages = new Message[]
            {
                new Message
                {
                    role = "user",
                    content = textPrompt
                }
            }
        };

        // Serialize the config object to JSON
        string jsonString = JsonUtility.ToJson(config, true); // 'true' makes the JSON formatted

        Debug.Log(jsonString);

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Send the request and wait for the response
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error: {request.error}");
            }
            else
            {
                // Parse and handle the response
                string responseText = request.downloadHandler.text;
                Debug.Log("OpenAI API Response: " + responseText);


                ReceivingOpenAI.Response response = JsonUtility.FromJson<ReceivingOpenAI.Response>(responseText);
                string data = response.choices[0].message.audio.data;
                AudioClip audioClip = WavUtility.ToAudioClip(data);  // Convert byte[] to AudioClip using a WAV utility
                AudioSource audioSource = GetComponent<AudioSource>();
                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }
    }
}
