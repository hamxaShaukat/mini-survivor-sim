using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class FastOllamaNPC : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string serverURL = "http://localhost:11434";
    public string modelName = "tinyllama:latest"; // or "tinyllama-fast"
    
    [Header("NPC Settings")]
    public string npcName = "Jungle Farmer";
    public float interactionRadius = 5f;
    
    [TextArea(2, 4)]
    public string personality = @"You are a jungle farmer. Respond in 1 short sentence.";
    
    [Header("Performance")]
    public int maxTokens = 30;
    public float temperature = 0.7f;
    public float timeout = 10f;
    
    private bool isProcessing = false;
    private GameObject player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"[{npcName}] Fast NPC Ready");
    }
    
    void Update()
    {
        if (player == null || isProcessing) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= interactionRadius && Input.GetKeyDown(KeyCode.T))
        {
            StartDialogue();
        }
    }
    
    void StartDialogue()
    {
        FastDialogueUI ui = FindObjectOfType<FastDialogueUI>();
        if (ui != null) ui.StartDialogue(this);
    }
    
    public void GetResponse(string playerMessage, Action<string> callback)
    {
        if (isProcessing)
        {
            callback?.Invoke("...");
            return;
        }
        
        StartCoroutine(SendFastRequest(playerMessage, callback));
    }
    
    IEnumerator SendFastRequest(string playerMessage, Action<string> callback)
    {
        isProcessing = true;
        
        // SHORT prompt
        string prompt = $"{personality}\nPlayer: {playerMessage}\nFarmer:";
        
        // OPTIMIZED JSON with fast parameters
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""prompt"": ""{EscapeJson(prompt)}"",
            ""stream"": false,
            ""options"": {{
                ""num_predict"": {maxTokens},
                ""temperature"": {temperature},
                ""top_k"": 20,
                ""top_p"": 0.9,
                ""repeat_penalty"": 1.1
            }}
        }}";
        
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/api/generate", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)timeout;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            string response = ParseFastResponse(responseJson);
            callback?.Invoke(response);
        }
        else
        {
            Debug.LogError($"Fast request failed in {Time.timeSinceLevelLoad:F1}s: {request.error}");
            callback?.Invoke("...");
        }
        
        request.Dispose();
        isProcessing = false;
    }
    
    string ParseFastResponse(string json)
    {
        try
        {
            // Fast parsing
            int start = json.IndexOf("\"response\":\"") + 12;
            if (start < 12) return "...";
            
            int end = json.IndexOf("\"", start);
            if (end <= start) return "...";
            
            string response = json.Substring(start, end - start)
                .Replace("\\n", " ")
                .Replace("\\\"", "\"")
                .Trim();
            
            // Take only first sentence
            int period = response.IndexOf('.');
            if (period > 0 && period < 50)
                response = response.Substring(0, period + 1);
            
            // Limit length
            if (response.Length > 60)
                response = response.Substring(0, 57) + "...";
            
            return response;
        }
        catch
        {
            return "Hmm...";
        }
    }
    
    string EscapeJson(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0); // Orange
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    
    [ContextMenu("Test Fast Response")]
    void TestFast()
    {
        GetResponse("Hello", (response) => {
            Debug.Log($"Fast test: {response}");
        });
    }
}