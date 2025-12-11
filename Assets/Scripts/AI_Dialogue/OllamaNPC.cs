using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;

public class OllamaNPC : MonoBehaviour
{
    [Header("=== OLLAMA CONNECTION ===")]
    public string serverURL = "http://localhost:11434";
    public string modelName = "tinyllama:latest";
    public bool autoDetectServer = true;
    public float requestTimeout = 55f;
    
    [Header("=== NPC PERSONALITY ===")]
    public string npcName = "Jungle Farmer";
    public string npcTitle = "Farmer";
    
    [TextArea(3, 6)]
    public string characterContext = @"You are {npcName}, a {npcTitle} living deep in the jungle. 
You've spent your whole life here. You speak simply and practically.
You know the jungle's secrets but share them cautiously.
Keep responses short (1-2 sentences). Always stay in character.";
    
    [Header("=== RESPONSE SETTINGS ===")]
    [Range(0.1f, 1f)] public float creativity = 0.7f;
    [Range(20, 100)] public int maxResponseLength = 60;
    public bool showThinkingProcess = false;
    
    [Header("=== DEBUG ===")]
    public bool debugMode = true;
    public bool enableOllama = true;
    public bool forceOllama = true; // NEW: Force Ollama usage
    
    // Private variables
    private bool isProcessing = false;
    private bool serverAvailable = false;
    private List<string> conversationHistory = new List<string>();
    private float lastRequestTime = 0f;
    private float minRequestInterval = 2f;
    
    // Cache for quick responses
    private Dictionary<string, string> responseCache = new Dictionary<string, string>();
    
    void Start()
    {
        // Replace placeholders in context
        characterContext = characterContext
            .Replace("{npcName}", npcName)
            .Replace("{npcTitle}", npcTitle.ToLower());
        
        if (autoDetectServer)
        {
            StartCoroutine(DetectAndTestServer());
        }
        else
        {
            StartCoroutine(TestServerConnection(serverURL));
        }
        
        // Initialize cache with common responses
        InitializeResponseCache();
        
        Debug.Log($"[{npcName}] Ollama NPC Initialized");
        
        // Test after 3 seconds
        Invoke("RunConnectionTest", 3f);
    }
    
    IEnumerator DetectAndTestServer()
    {
        Debug.Log($"[{npcName}] Auto-detecting Ollama server...");
        
        string[] possibleURLs = {
            "http://localhost:11434",
            "http://127.0.0.1:11434"
        };
        
        foreach (string url in possibleURLs)
        {
            if (debugMode) Debug.Log($"[{npcName}] Testing: {url}");
            
            UnityWebRequest testRequest = UnityWebRequest.Get($"{url}/api/tags");
            testRequest.timeout = 5;
            yield return testRequest.SendWebRequest();
            
            if (testRequest.result == UnityWebRequest.Result.Success)
            {
                serverURL = url;
                serverAvailable = true;
                Debug.Log($"✅ [{npcName}] Ollama server FOUND at: {url}");
                
                // Check if model is available
                string responseText = testRequest.downloadHandler.text;
                if (responseText.Contains(modelName))
                {
                    Debug.Log($"✅ [{npcName}] Model '{modelName}' is available");
                }
                else
                {
                    Debug.LogWarning($"[{npcName}] Model '{modelName}' not found in available models");
                }
                break;
            }
            else
            {
                if (debugMode) Debug.Log($"❌ [{npcName}] {url}: {testRequest.error}");
            }
            
            testRequest.Dispose();
        }
        
        if (!serverAvailable)
        {
            Debug.LogWarning($"[{npcName}] Ollama server not found. Using local mode.");
        }
    }
    
    IEnumerator TestServerConnection(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get($"{url}/api/tags");
        request.timeout = 5;
        yield return request.SendWebRequest();
        
        serverAvailable = request.result == UnityWebRequest.Result.Success;
        
        if (serverAvailable)
        {
            Debug.Log($"✅ [{npcName}] Connected to Ollama at {url}");
        }
        else
        {
            Debug.LogWarning($"[{npcName}] Cannot connect to Ollama: {request.error}");
            Debug.Log($"[{npcName}] Make sure Ollama is running with: 'ollama serve'");
        }
        
        request.Dispose();
    }
    
    void RunConnectionTest()
    {
        Debug.Log($"[{npcName}] === CONNECTION STATUS ===");
        Debug.Log($"[{npcName}] enableOllama: {enableOllama}");
        Debug.Log($"[{npcName}] serverAvailable: {serverAvailable}");
        Debug.Log($"[{npcName}] forceOllama: {forceOllama}");
        Debug.Log($"[{npcName}] serverURL: {serverURL}");
        Debug.Log($"[{npcName}] modelName: {modelName}");
        
        if (enableOllama && serverAvailable)
        {
            Debug.Log($"✅ [{npcName}] Ready for Ollama AI conversations!");
            
            // Auto-test with a simple message
            if (debugMode)
            {
                ProcessMessage("test connection", (response) => {
                    Debug.Log($"[{npcName}] Connection test response: {response}");
                });
            }
        }
        else if (enableOllama && !serverAvailable)
        {
            Debug.LogError($"[{npcName}] ERROR: Ollama enabled but server not found!");
            Debug.LogError($"[{npcName}] Solution: Run 'ollama serve' in terminal");
        }
    }
    
    void InitializeResponseCache()
    {
        // Common questions and their responses
        responseCache["who are you"] = $"I'm {npcName}, the {npcTitle.ToLower()} of this jungle. I've tended these lands for years.";
        responseCache["what is your name"] = $"They call me {npcName}. The jungle knows me well.";
        responseCache["what do you do"] = $"I'm a {npcTitle.ToLower()}. The soil here grows unique crops that need careful tending.";
        responseCache["hello"] = "Greetings, traveler. The jungle welcomes those who respect it.";
        responseCache["hi"] = "Ah, a visitor. Watch your step in these parts.";
        responseCache["how are you"] = "Can't complain. The jungle provides, though it tests us daily.";
        responseCache["where are we"] = "Deep in the jungle. My home, and perhaps yours for a while.";
        responseCache["is it safe"] = "Safe enough if you know the paths. I can show you around.";
        responseCache["help"] = "The jungle can be unforgiving. What do you need help with?";
        responseCache["thank you"] = "No need for thanks. We help each other here.";
        responseCache["bye"] = "Take care on your journey. The jungle remembers.";
        responseCache["goodbye"] = "Safe travels. May the jungle guide your path.";
    }
    
    public void ProcessMessage(string playerMessage, Action<string> onResponse)
    {
        if (debugMode) Debug.Log($"[{npcName}] ProcessMessage called: '{playerMessage}'");
        
        if (isProcessing)
        {
            onResponse?.Invoke("...");
            return;
        }
        
        // Rate limiting
        float timeSinceLastRequest = Time.time - lastRequestTime;
        if (timeSinceLastRequest < minRequestInterval)
        {
            onResponse?.Invoke("Let me think...");
            return;
        }
        
        // Clean and check cache first
        playerMessage = playerMessage.ToLower().Trim();
        string cachedResponse = GetCachedResponse(playerMessage);
        if (!string.IsNullOrEmpty(cachedResponse))
        {
            if (debugMode) Debug.Log($"[{npcName}] Using cached response");
            onResponse?.Invoke(cachedResponse);
            return;
        }
        
        // Decide whether to use Ollama or local
        bool useOllama = ShouldUseOllama(playerMessage);
        
        if (debugMode) Debug.Log($"[{npcName}] useOllama decision: {useOllama}");
        
        if (useOllama)
        {
            if (debugMode) Debug.Log($"[{npcName}] Sending to Ollama...");
            StartCoroutine(SendToOllama(playerMessage, onResponse));
        }
        else
        {
            if (debugMode) Debug.Log($"[{npcName}] Using local response");
            string localResponse = GenerateLocalResponse(playerMessage);
            onResponse?.Invoke(localResponse);
        }
    }
    
    bool ShouldUseOllama(string playerMessage)
    {
        // If forceOllama is true, always use Ollama when available
        if (forceOllama && enableOllama && serverAvailable)
            return true;
        
        // Use Ollama for complex questions
        if (playerMessage.Contains("why") || playerMessage.Contains("how") || 
            playerMessage.Contains("explain") || playerMessage.Length > 20)
            return enableOllama && serverAvailable;
        
        // Use local for simple greetings (but still sometimes use Ollama)
        if (playerMessage.Contains("hello") || playerMessage.Contains("hi") || 
            playerMessage.Contains("hey"))
        {
            // 50% chance to use Ollama for greetings
            return enableOllama && serverAvailable && UnityEngine.Random.value > 0.5f;
        }
        
        // Default: Use Ollama if available
        return enableOllama && serverAvailable;
    }
    
    string GetCachedResponse(string playerMessage)
    {
        foreach (var kvp in responseCache)
        {
            if (playerMessage.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }
        return null;
    }
    
    IEnumerator SendToOllama(string playerMessage, Action<string> onResponse)
    {
        isProcessing = true;
        lastRequestTime = Time.time;
        
        // Add to conversation history
        conversationHistory.Add($"Player: {playerMessage}");
        if (conversationHistory.Count > 6)
            conversationHistory.RemoveAt(0);
        
        // Build the prompt
        string prompt = BuildPrompt(playerMessage);
        
        if (debugMode)
        {
            Debug.Log($"[{npcName}] === OLLAMA PROMPT ===");
            Debug.Log(prompt);
            Debug.Log($"[{npcName}] ====================");
        }
        
        // Prepare request data
        OllamaRequest requestData = new OllamaRequest
        {
            model = modelName,
            prompt = prompt,
            stream = false,
            options = new OllamaOptions
            {
                temperature = creativity,
                num_predict = maxResponseLength,
                top_k = 40,
                top_p = 0.9f,
                repeat_penalty = 1.1f,
                seed = DateTime.Now.Second
            }
        };
        
        string json = JsonUtility.ToJson(requestData);
        byte[] body = Encoding.UTF8.GetBytes(json);
        
        // Send request
        UnityWebRequest request = new UnityWebRequest($"{serverURL}/api/generate", "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = (int)requestTimeout;
        
        if (debugMode) Debug.Log($"[{npcName}] Sending request to Ollama...");
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            if (debugMode) Debug.Log($"[{npcName}] Ollama request successful");
            
            string responseJson = request.downloadHandler.text;
            string npcResponse = ParseOllamaResponse(responseJson);
            
            // Clean and validate response
            npcResponse = CleanResponse(npcResponse);
            
            // Add to history
            conversationHistory.Add($"{npcName}: {npcResponse}");
            
            // Cache this response for future
            if (!responseCache.ContainsKey(playerMessage) && playerMessage.Length < 20)
            {
                responseCache[playerMessage] = npcResponse;
            }
            
            if (debugMode)
            {
                Debug.Log($"[{npcName}] === OLLAMA RESPONSE ===");
                Debug.Log(npcResponse);
                Debug.Log($"[{npcName}] =======================");
            }
            
            onResponse?.Invoke(npcResponse);
        }
        else
        {
            Debug.LogError($"[{npcName}] Ollama Error: {request.error}");
            Debug.LogError($"[{npcName}] Response: {request.downloadHandler?.text}");
            
            string fallback = GenerateLocalResponse(playerMessage);
            onResponse?.Invoke(fallback);
        }
        
        request.Dispose();
        isProcessing = false;
    }
    
    string BuildPrompt(string playerMessage)
    {
        // TinyLlama specific prompt format
        string history = "";
        if (conversationHistory.Count > 0)
        {
            int start = Math.Max(0, conversationHistory.Count - 4);
            for (int i = start; i < conversationHistory.Count; i++)
            {
                history += conversationHistory[i] + "\n";
            }
        }
        
        // Simple prompt format that works with TinyLlama
        return $@"[INST] <<SYS>>
{characterContext}

You are {npcName}. Never break character. 
Respond in 1-2 short sentences.
Current time: Daytime
Location: Jungle clearing
<</SYS>>

{history}Player: {playerMessage}
[/INST] {npcName}:";
    }
    
    string ParseOllamaResponse(string jsonResponse)
    {
        try
        {
            if (debugMode) Debug.Log($"[{npcName}] Parsing response JSON...");
            
            // Simple JSON parsing for Ollama response
            int responseStart = jsonResponse.IndexOf("\"response\":\"") + 12;
            if (responseStart < 12)
            {
                // Try alternative format
                responseStart = jsonResponse.IndexOf("response\":\"") + 11;
            }
            
            if (responseStart >= 11)
            {
                int responseEnd = jsonResponse.IndexOf("\"", responseStart);
                if (responseEnd > responseStart)
                {
                    string rawResponse = jsonResponse.Substring(responseStart, responseEnd - responseStart);
                    
                    // Decode escape sequences
                    rawResponse = rawResponse.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\t", "\t");
                    
                    // Remove the NPC name prefix if present
                    if (rawResponse.StartsWith($"{npcName}:"))
                        rawResponse = rawResponse.Substring(npcName.Length + 1).Trim();
                    
                    // Remove [/INST] tag if present
                    if (rawResponse.Contains("[/INST]"))
                        rawResponse = rawResponse.Substring(rawResponse.IndexOf("[/INST]") + 7).Trim();
                    
                    return rawResponse.Trim();
                }
            }
            
            // Fallback to JsonUtility
            OllamaResponse response = JsonUtility.FromJson<OllamaResponse>(jsonResponse);
            if (response != null && !string.IsNullOrEmpty(response.response))
            {
                string rawResponse = response.response;
                
                // Clean up
                if (rawResponse.StartsWith($"{npcName}:"))
                    rawResponse = rawResponse.Substring(npcName.Length + 1);
                
                return rawResponse.Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{npcName}] Parse error: {e.Message}");
            Debug.LogError($"[{npcName}] JSON was: {jsonResponse}");
        }
        
        return "Let me think about that...";
    }
    
    string CleanResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) 
            return "The jungle winds carry no words...";
        
        response = response.Trim();
        
        // Remove multiple spaces
        while (response.Contains("  "))
            response = response.Replace("  ", " ");
        
        // Ensure it's not too long
        if (response.Length > maxResponseLength)
        {
            response = response.Substring(0, maxResponseLength);
            if (!response.EndsWith(".") && !response.EndsWith("!") && !response.EndsWith("?"))
                response += "...";
        }
        
        return response;
    }
    
    string GenerateLocalResponse(string playerMessage)
    {
        if (debugMode) Debug.Log($"[{npcName}] Generating local response");
        
        // Smart local response generation
        if (playerMessage.Contains("why"))
            return "The jungle works in mysterious ways. Some things are not for us to understand.";
        
        if (playerMessage.Contains("how"))
            return "With patience and respect. The jungle teaches those who listen.";
        
        if (playerMessage.Contains("when"))
            return "Time flows differently here. The jungle decides when.";
        
        if (playerMessage.Contains("where"))
            return "Paths shift in the jungle. I can show you safe routes.";
        
        // Contextual responses based on NPC type
        if (npcTitle.Contains("Farmer"))
        {
            string[] farmResponses = {
                "The soil whispers of seasons past and those to come.",
                "Crops grow with care, not just water and sun.",
                "Every plant has its story in this jungle.",
                "Harvest comes to those who understand the land."
            };
            return farmResponses[UnityEngine.Random.Range(0, farmResponses.Length)];
        }
        
        // Fallback philosophical responses
        string[] fallbacks = {
            "The jungle holds many answers for those who ask the right questions.",
            "Sometimes the journey matters more than the destination.",
            "Nature speaks in whispers; we must learn to listen.",
            "Every path in the jungle leads somewhere, eventually.",
            "Patience reveals what haste overlooks."
        };
        
        return fallbacks[UnityEngine.Random.Range(0, fallbacks.Length)];
    }
    
    [ContextMenu("Test Ollama Connection")]
    public void TestConnection()
    {
        Debug.Log($"[{npcName}] === OLLAMA CONNECTION TEST ===");
        
        if (!enableOllama)
        {
            Debug.Log("Ollama is disabled. Enable it in inspector.");
            return;
        }
        
        ProcessMessage("Hello, who are you?", (response) => {
            Debug.Log($"✅ [{npcName}] Test Response: {response}");
            
            // Test a second message to check conversation memory
            if (serverAvailable)
            {
                StartCoroutine(TestFollowUp());
            }
        });
    }
    
    IEnumerator TestFollowUp()
    {
        yield return new WaitForSeconds(1f);
        
        ProcessMessage("What do you do here?", (response) => {
            Debug.Log($"✅ [{npcName}] Follow-up Response: {response}");
        });
    }
    
    [ContextMenu("Reset Conversation")]
    public void ResetConversation()
    {
        conversationHistory.Clear();
        Debug.Log($"[{npcName}] Conversation reset");
    }
    
    // JSON Classes
    [System.Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
        public OllamaOptions options;
    }
    
    [System.Serializable]
    private class OllamaOptions
    {
        public float temperature;
        public int num_predict;
        public int top_k;
        public float top_p;
        public float repeat_penalty;
        public int seed;
    }
    
    [System.Serializable]
    private class OllamaResponse
    {
        public string model;
        public string created_at;
        public string response;
        public bool done;
        public long total_duration;
    }
}