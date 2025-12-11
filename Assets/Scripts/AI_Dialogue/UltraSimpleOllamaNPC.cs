using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class UltraSimpleOllamaNPC : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string serverURL = "http://localhost:11434";
    public string modelName = "tinyllama:latest";
    
    [Header("NPC Settings")]
    public string npcName = "Jungle Farmer";
    public float interactionRadius = 5f;
    
    [TextArea(2, 4)]
    public string personality = "You are a jungle farmer. Speak simply in 1 sentence.";
    
    // Private
    private bool isProcessing = false;
    private GameObject player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        Debug.Log($"[{npcName}] Ready - Radius: {interactionRadius}m");
        
        // Quick test
        StartCoroutine(TestOllama());
    }
    
    IEnumerator TestOllama()
    {
        UnityWebRequest test = UnityWebRequest.Get($"{serverURL}/api/tags");
        yield return test.SendWebRequest();
        
        if (test.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Ollama connected!");
        }
        else
        {
            Debug.LogError($"❌ Ollama error: {test.error}");
        }
    }
    
    void Update()
    {
        if (player == null) return;
        
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= interactionRadius && Input.GetKeyDown(KeyCode.T))
        {
            StartDialogue();
        }
    }
    
    void StartDialogue()
    {
        UltraSimpleDialogueUI dialogueUI = FindObjectOfType<UltraSimpleDialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogue(this);
        }
    }
    
    public void GetAIResponse(string playerMessage, Action<string> callback)
    {
        if (isProcessing)
        {
            callback?.Invoke("Busy...");
            return;
        }
        
        StartCoroutine(CallOllamaAPI(playerMessage, callback));
    }
    
    IEnumerator CallOllamaAPI(string playerMessage, Action<string> callback)
    {
        isProcessing = true;
        
        // SUPER SIMPLE PROMPT
        string prompt = $"{personality}\n\nPlayer: {playerMessage}\n{npcName}:";
        
        // SIMPLE JSON
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""prompt"": ""{EscapeJson(prompt)}"",
            ""stream"": false
        }}";
        
        Debug.Log($"Sending to Ollama: {playerMessage}");
        
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/api/generate", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 20; // 20 seconds
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            Debug.Log($"Raw Ollama response: {responseJson}");
            
            string response = ExtractResponse(responseJson);
            callback?.Invoke(response);
        }
        else
        {
            Debug.LogError($"Ollama failed: {request.error}");
            // NO FALLBACK - Just show error
            callback?.Invoke($"Error: {request.error}");
        }
        
        request.Dispose();
        isProcessing = false;
    }
    
    string ExtractResponse(string json)
    {
        try
        {
            // Direct string extraction
            if (json.Contains("\"response\":\""))
            {
                int start = json.IndexOf("\"response\":\"") + 12;
                int end = json.IndexOf("\"", start);
                
                if (start > 12 && end > start)
                {
                    string response = json.Substring(start, end - start)
                        .Replace("\\n", " ")
                        .Replace("\\\"", "\"")
                        .Trim();
                    
                    // Remove NPC name if at start
                    if (response.StartsWith($"{npcName}:"))
                        response = response.Substring(npcName.Length + 1);
                    
                    return response;
                }
            }
            
            return "Could not parse response";
        }
        catch (Exception e)
        {
            return $"Error: {e.Message}";
        }
    }
    
    string EscapeJson(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}




