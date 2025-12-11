using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class SimpleOllamaNPC : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string serverURL = "http://localhost:11434";
    public string modelName = "tinyllama:latest";
    
    [Header("NPC Settings")]
    public string npcName = "Jungle Farmer";
    public float interactionRadius = 5f;
    
    [TextArea(2, 4)]
    public string systemPrompt = "You are a jungle farmer. Speak simply. Keep responses short.";
    
    // Private
    private bool isProcessing = false;
    private GameObject player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        Debug.Log($"[{npcName}] Ready. Interaction radius: {interactionRadius}m");
    }
    
    void Update()
    {
        // Auto-check if player is in radius
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool playerInRange = distance <= interactionRadius;
            
            // You can add visual indicator here if needed
            if (playerInRange && Input.GetKeyDown(KeyCode.T))
            {
                StartDialogue();
            }
        }
    }
    
    void StartDialogue()
    {
        // Find DialogueUI in scene
        SimpleRadiusDialogueUI dialogueUI = FindObjectOfType<SimpleRadiusDialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogueWithNPC(this);
        }
        else
        {
            Debug.LogError("SimpleRadiusDialogueUI not found in scene!");
        }
    }
    
    public void GetResponse(string playerMessage, Action<string> callback)
    {
        if (isProcessing)
        {
            callback?.Invoke("Thinking...");
            return;
        }
        
        StartCoroutine(SendToOllama(playerMessage, callback));
    }
    
    IEnumerator SendToOllama(string playerMessage, Action<string> callback)
    {
        isProcessing = true;
        
        // Simple prompt
        string prompt = $@"[INST] <<SYS>>
{systemPrompt}
You are {npcName}. Respond in 1-2 sentences.
<</SYS>>

Player: {playerMessage}
[/INST] {npcName}:";
        
        // Prepare JSON
        string json = $@"{{
            ""model"": ""{modelName}"",
            ""prompt"": ""{EscapeJson(prompt)}"",
            ""stream"": false
        }}";
        
        // Send request
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/api/generate", "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            string response = ParseResponse(responseJson);
            callback?.Invoke(response);
        }
        else
        {
            Debug.LogError($"Ollama Error: {request.error}");
            callback?.Invoke("I cannot respond right now.");
        }
        
        request.Dispose();
        isProcessing = false;
    }
    
    string ParseResponse(string json)
    {
        try
        {
            int start = json.IndexOf("\"response\":\"") + 12;
            if (start < 12) return "...";
            
            int end = json.IndexOf("\"", start);
            if (end <= start) return "...";
            
            string response = json.Substring(start, end - start)
                .Replace("\\n", " ")
                .Replace("\\\"", "\"")
                .Trim();
            
            // Remove NPC name if present
            if (response.StartsWith($"{npcName}:"))
                response = response.Substring(npcName.Length + 1);
            
            return response;
        }
        catch
        {
            return "...";
        }
    }
    
    string EscapeJson(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\n", "\\n");
    }
    
    // Draw radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    
    [ContextMenu("Test Connection")]
    void TestConnection()
    {
        GetResponse("Hello, who are you?", (response) => {
            Debug.Log($"Test Response: {response}");
        });
    }
}