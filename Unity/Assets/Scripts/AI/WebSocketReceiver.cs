using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public class WebSocketReceiver : MonoBehaviour
{
    public ClientWebSocket webSocket;
    
    private WebSocketClient webSocketClient;

    [SerializeField]
    private AudioClip audioClip;

    [SerializeField]
    private ResponseChecker responseChecker;

    private AudioClip streamingClip; //streaming audio clip
    private int sampleRate = 24000; // or the specific sample rate for your data
    private int channels = 1; // Mono or Stereo based on your data

    private AudioSource audioSource;
    Queue<float> audioQueue = new Queue<float>();
    //private MemoryStream audioBuffer;
    private string responseText = string.Empty;

    private string eventID = string.Empty;
    private void Start()
    {
        Debug.Log("int max size "+ int.MaxValue);
        //audioBuffer = new MemoryStream();
        audioSource = GetComponent<AudioSource>();
        streamingClip = AudioClip.Create("AudioStream", sampleRate * 1000, channels, sampleRate, true, OnAudioRecieve);
        audioSource.clip = streamingClip;
        audioSource.loop = true;  // Ensure continuous playback
    }
    public async void SetWebSocket(ClientWebSocket socket)
    {
        webSocket = socket;
        Debug.Log("_clientWebSocket.State " + webSocket.State);
        await ReceiveMessages();
    }

    public void SetWebSocketClient(WebSocketClient client)
    {
        webSocketClient = client;
    }

    private async Task ReceiveMessages()
    {

        var buffer = new byte[1024 * 128];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                Debug.Log("WebSocket closed.");
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log("Received text message in Receiver: " + message);
                
                Debug.Log("Json is complete "+IsCompleteJson(message));
                AudioDeltaResponse response = JsonUtility.FromJson<AudioDeltaResponse>(message);

                // Debug.Log($"Type: {response.type}");
                // Debug.Log($"Event ID: {response.event_id}");
                // Debug.Log($"Response ID: {response.response_id}");
                // Debug.Log($"Item ID: {response.item_id}");
                // Debug.Log($"Output Index: {response.output_index}");
                // Debug.Log($"Content Index: {response.content_index}");
                // Debug.Log($"Delta: {response.delta}");

                eventID = response.event_id;



                if (response.type == "response.audio_transcript.delta")
                {
                    Debug.Log($"Text: {response.delta}");
                    responseText += response.delta;
                }
                else if (response.type == "response.audio.done")
                {

                    Debug.Log("Audio Done!");
                    //audioSource.Stop();
                }
                else if (response.type == "response.audio_transcript.done")
                {
                    Debug.Log("Audio Transription Done!" + responseText);
                    // int answer = await responseChecker.CheckResponseAsync(responseText);
                    // if(answer == 0)
                    // {
                    //     _ =  webSocketClient.SendTextSystem( "Zusatzinformation: Bitte fokusiere dich bei deinen Antworten mehr auf die Instruktionen die ich dir gegeben habe und schweife bitte vom Thema ab."
                    //     + "Bitte antworte immer in deutscher Sprache.");   
                    // }
                 
                    
                    responseText = string.Empty;
                }
                else if (response.type == "response.audio.delta")
                {
                    try
                    {
                        // Convert Base64 audio string to byte array
                        byte[] audioData = Convert.FromBase64String(response.delta);

                        // Write audio data to buffer
                        //lock (audioBuffer)
                        //{
                        //    audioBuffer.Write(audioData, 0, audioData.Length);
                        //}
                        float[] pcmData = ConvertBytesToFloats(audioData, 1); // Assuming mono
                        lock (audioQueue)
                        {
                            foreach (var sample in pcmData)
                                audioQueue.Enqueue(sample);
                        }

                        if (!audioSource.isPlaying)
                            audioSource.Play();
                        Debug.Log("Added audio data to buffer");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Error processing received audio data: " + ex.Message);
                    }
                }
                else if (response.type == "response.done")
                {

                }
                else if (response.type == "conversation.item.truncated")
                {
                    Debug.LogWarning("Conversation item truncated");
                }
                else if (response.type == "response.cancel")
                {
                    Debug.LogWarning("Response canceled");
                }
                else if (response.type == "error")
                {
                    Debug.Log("Error " + response);
                }
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                Debug.Log("Received audio data as binary.");

                // Process binary data (e.g., convert to audio clip or save to file)

            }


        }
    }

    public void InterruptAudioStream()
    {
        lock(audioQueue)
            audioQueue.Clear();
    }

    private bool IsCompleteJson(string json)
    {
    // Ensure the JSON starts with '{' and ends with '}'
        return json.Trim().StartsWith("{") && json.Trim().EndsWith("}");
    }
    private float[] ConvertBytesToFloats(byte[] byteArray, int channels)
    {
        int sampleCount = byteArray.Length / 2;
        float[] floatArray = new float[sampleCount / channels];
        int floatIndex = 0;

        for (int i = 0; i < byteArray.Length; i += 2)
        {
            short sample = (short)(byteArray[i] | (byteArray[i + 1] << 8));
            floatArray[floatIndex++] = sample / 32768f;
        }

        return floatArray;
    }

    public string getResponseEventId()
    {
        return eventID;
    }

    private void OnAudioRecieve(float[] data)
    {
        // try
        // {
        //     if (audioBuffer == null || audioBuffer.Length == 0)
        //     {
        //         // If no data is available, fill output with silence
        //         for (int i = 0; i < data.Length; i++)
        //         {
        //             data[i] = 0f;
        //         }
        //         return;
        //     }

        //     Debug.Log("OnAudioReceive");

        // lock (audioQueue)
        {


            for (int i = 0; i < data.Length; i++)
            {
                // Fill data from the queue or silence if no data
                data[i] = audioQueue.Count > 0 ? audioQueue.Dequeue() : 0f;
            }
            // }
            // // Reset position to start of buffer for reading
            // audioBuffer.Position = 0;

            // using (BinaryReader reader = new BinaryReader(audioBuffer, System.Text.Encoding.Default, leaveOpen: true))
            // {
            //     Debug.Log("Reading audioBuffer with data length: " + data.Length + " " + audioBuffer.Length + " " + audioBuffer.Position);

            //     for (int i = 0; i < data.Length; i++)
            //     {
            //         if (audioBuffer.Position + 2 <= audioBuffer.Length || i <= audioBuffer.Length)
            //         {
            //             // Read a 16-bit sample and convert to -1 to 1 range
            //             short sample = reader.ReadInt16();
            //             data[i] = sample / 32768f;
            //         }
            //         else
            //         {
            //             // Fill the rest with silence if out of data
            //             data[i] = 0f;
            //         }
            //     }
            //     Debug.Log("After audioBuffer with data length: " + audioBuffer.Length + " " + audioBuffer.Position);
            //     // Set the buffer length to the remaining unread data (if any)
            //     var remainingData = audioBuffer.Length - audioBuffer.Position;
            //     if (remainingData > 0)
            //     {
            //         // Copy remaining bytes to start of the buffer
            //         byte[] leftoverBytes = reader.ReadBytes((int)remainingData);
            //         audioBuffer.SetLength(0); // Reset buffer
            //         audioBuffer.Write(leftoverBytes, 0, leftoverBytes.Length);
            //     }
            //     else
            //     {
            //         // No data left, clear buffer
            //         audioBuffer.SetLength(0);
            //     }
            // }

            // // Reset buffer position to start for next write
            // audioBuffer.Position = audioBuffer.Length;
            // }
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError("Error in OnAudioReceive: " + e.Message);
        }
    }
}
