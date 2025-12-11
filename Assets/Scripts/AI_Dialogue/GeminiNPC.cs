using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;

public class GeminiNPC : MonoBehaviour
{
    [Header("=== GEMINI API CONFIGURATION ===")]
    public string apiKey = "AIzaSyAsREvTZobeVptC7slWm96M2H0IB3gFfQA"; // Replace with your API key
    public string model = "gemini-2.5-flash";
    public bool enableGemini = true;
    
    [Header("=== NPC PERSONALITY ===")]
    public string npcName = "Jungle Farmer";
    public string occupation = "Farmer";
    public string personalityTraits = "Friendly, Wise, Experienced";
    
    [TextArea(5, 10)]
    public string backstory = @"I am a farmer who has lived in this jungle for over 20 years. 
I grow unique crops that only thrive in jungle soil. 
I know every plant, animal, and secret path in this area.
My family has been farming here for generations.
I love sharing stories about the jungle with travelers.";

    [Header("=== GAME CONTEXT ===")]
    public string currentTime = "Morning";
    public string weather = "Sunny";
    public string location = "Jungle Field";
    public float relationshipWithPlayer = 50f; // 0-100
    
    [Header("=== RESPONSE SETTINGS ===")]
    [Range(0, 1)] public float creativity = 0.7f;
    [Range(50, 500)] public int maxResponseLength = 150;
    public bool showThinkingProcess = false;
    
    [Header("=== DEBUG ===")]
    public bool debugMode = false;
    public List<string> conversationHistory = new List<string>();
    
    // Private variables
    private bool isProcessing = false;
    private string fullSystemPrompt;
    private DialogueUI dialogueUI;
    
    void Start()
    {
        dialogueUI = FindObjectOfType<DialogueUI>();
        GenerateSystemPrompt();
        
        if (debugMode)
        {
            Debug.Log($"[{npcName}] Gemini NPC Initialized");
            Debug.Log($"API Enabled: {enableGemini}");
            Debug.Log($"Model: {model}");
        }
    }
    
    void GenerateSystemPrompt()
    {
        fullSystemPrompt = $@"ROLE: You are {npcName}, a {occupation} in a jungle simulation game.

PERSONALITY: {personalityTraits}
BACKSTORY: {backstory}

GAME CONTEXT:
- Current Time: {currentTime}
- Weather: {weather}
- Location: {location}
- Relationship with Player: {GetRelationshipLevel()}/100 ({GetRelationshipText()})

RULES FOR RESPONSES:
1. Stay in character at ALL times
2. Keep responses SHORT (1-3 sentences maximum)
3. Use natural, conversational language
4. Mention jungle elements when relevant
5. NEVER break character or mention you're an AI
6. If asked about game mechanics, respond in-character
7. Show emotions through words, not emojis
8. Speak like a real person in a jungle

EXAMPLE DIALOGUES:
Player: Hello there!
{npcName}: Ah, a traveler! Welcome to my field. The jungle is peaceful today.
Player: What do you grow here?
{npcName}: Special herbs that only grow in jungle soil. They're useful for medicine.
Player: Is it dangerous here?
{npcName}: Only if you don't respect the jungle. I can show you safe paths.

CURRENT CONVERSATION:";
    }
    
    public void ProcessPlayerMessage(string playerMessage, Action<string> onResponseReceived)
    {
        if (isProcessing)
        {
            onResponseReceived?.Invoke("Let me finish my thought...");
            return;
        }
        
        if (!enableGemini || string.IsNullOrEmpty(apiKey) || apiKey.Contains("xxxx"))
        {
            // Fallback to local response
            string fallbackResponse = GetFallbackResponse(playerMessage);
            onResponseReceived?.Invoke(fallbackResponse);
            return;
        }
        
        StartCoroutine(SendToGeminiAPI(playerMessage, onResponseReceived));
    }
    
    IEnumerator SendToGeminiAPI(string playerMessage, Action<string> onResponseReceived)
    {
        isProcessing = true;
        
        // Add to conversation history
        conversationHistory.Add($"Player: {playerMessage}");
        if (conversationHistory.Count > 10) // Keep last 10 messages
            conversationHistory.RemoveAt(0);
        
        // Prepare the prompt
        string historyText = string.Join("\n", conversationHistory);
        string finalPrompt = fullSystemPrompt + "\n" + historyText + $"\n\nPlayer: {playerMessage}\n{npcName}:";
        
        if (debugMode)
        {
            Debug.Log($"[{npcName}] Sending to Gemini:\n{finalPrompt}");
        }
        
        // Prepare JSON payload
        GeminiRequest requestData = new GeminiRequest
        {
            contents = new List<Content>
            {
                new Content
                {
                    parts = new List<Part>
                    {
                        new Part { text = finalPrompt }
                    }
                }
            },
            generationConfig = new GenerationConfig
            {
                temperature = creativity,
                topK = 40,
                topP = 0.95f,
                maxOutputTokens = maxResponseLength,
                stopSequences = new List<string> { "Player:", "###", "\n\n" }
            },
            safetySettings = new List<SafetySetting>
            {
                new SafetySetting
                {
                    category = "HARM_CATEGORY_HARASSMENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new SafetySetting
                {
                    category = "HARM_CATEGORY_HATE_SPEECH", 
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new SafetySetting
                {
                    category = "HARM_CATEGORY_SEXUALLY_EXPLICIT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                },
                new SafetySetting
                {
                    category = "HARM_CATEGORY_DANGEROUS_CONTENT",
                    threshold = "BLOCK_MEDIUM_AND_ABOVE"
                }
            }
        };
        
        string jsonPayload = JsonUtility.ToJson(requestData);
        
        // API URL
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        
        // Create web request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 10; // 10 second timeout
        
        // Send request
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseJson = request.downloadHandler.text;
            
            if (debugMode)
            {
                Debug.Log($"[{npcName}] Gemini Response:\n{responseJson}");
            }
            
            // Parse response
            string npcResponse = ParseAPIResponse(responseJson);
            
            // Clean up response
            npcResponse = CleanResponse(npcResponse);
            
            // Update relationship
            UpdateRelationshipBasedOnInteraction(playerMessage, npcResponse);
            
            // Add to history
            conversationHistory.Add($"{npcName}: {npcResponse}");
            
            // Return response
            onResponseReceived?.Invoke(npcResponse);
            
            if (debugMode)
            {
                Debug.Log($"[{npcName}] Final Response: {npcResponse}");
            }
        }
        else
        {
            Debug.LogError($"[{npcName}] Gemini API Error: {request.error}");
            
            // Fallback response
            string fallback = GetFallbackResponse(playerMessage);
            onResponseReceived?.Invoke(fallback);
        }
        
        request.Dispose();
        isProcessing = false;
    }
    
    string ParseAPIResponse(string jsonResponse)
    {
        try
        {
            // Parse the JSON response
            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
            
            if (response != null && response.candidates != null && response.candidates.Count > 0)
            {
                if (response.candidates[0].content != null && 
                    response.candidates[0].content.parts != null && 
                    response.candidates[0].content.parts.Count > 0)
                {
                    return response.candidates[0].content.parts[0].text;
                }
            }
            
            // Fallback parsing if structure is different
            int textStart = jsonResponse.IndexOf("\"text\":\"") + 8;
            if (textStart > 8)
            {
                int textEnd = jsonResponse.IndexOf("\"", textStart);
                if (textEnd > textStart)
                {
                    return jsonResponse.Substring(textStart, textEnd - textStart)
                        .Replace("\\n", "\n")
                        .Replace("\\\"", "\"");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{npcName}] Parse Error: {e.Message}");
        }
        
        return "I'm not sure how to respond to that right now.";
    }
    
    string CleanResponse(string response)
    {
        if (string.IsNullOrEmpty(response)) return "The jungle winds carry no words...";
        
        // Remove any potential prefix
        if (response.StartsWith($"{npcName}:"))
            response = response.Substring(npcName.Length + 1).Trim();
        
        // Remove markdown formatting
        response = response.Replace("**", "").Replace("*", "").Replace("#", "");
        
        // Ensure it's not too long
        if (response.Length > maxResponseLength)
            response = response.Substring(0, maxResponseLength) + "...";
        
        return response.Trim();
    }
    
    string GetFallbackResponse(string playerMessage)
    {
        playerMessage = playerMessage.ToLower();
        
        if (playerMessage.Contains("hello") || playerMessage.Contains("hi") || playerMessage.Contains("hey"))
            return $"Greetings, traveler! I'm {npcName}. The jungle welcomes you.";
        
        if (playerMessage.Contains("how are you") || playerMessage.Contains("how do you do"))
            return $"I'm well, thank you. The {weather.ToLower()} weather is good for the crops.";
        
        if (playerMessage.Contains("what do you do") || playerMessage.Contains("what is your job"))
            return $"I'm a {occupation.ToLower()} here in the jungle. It's hard work but rewarding.";
        
        if (playerMessage.Contains("jungle") || playerMessage.Contains("forest"))
            return "The jungle is full of life and secrets. One must respect it to thrive here.";
        
        if (playerMessage.Contains("danger") || playerMessage.Contains("safe"))
            return "Stay on the paths and avoid the deep thickets after dark.";
        
        if (playerMessage.Contains("name"))
            return $"I'm {npcName}. My family has lived here for generations.";
        
        // Default responses based on relationship
        if (relationshipWithPlayer > 70)
            return "Good to see you again, friend! What's on your mind?";
        else if (relationshipWithPlayer > 30)
            return "Hmm, interesting. Tell me more.";
        else
            return "I'm busy with my work. What do you need?";
    }
    
    void UpdateRelationshipBasedOnInteraction(string playerMessage, string npcResponse)
    {
        playerMessage = playerMessage.ToLower();
        
        // Positive interactions
        if (playerMessage.Contains("thank") || playerMessage.Contains("please") || 
            playerMessage.Contains("help") || playerMessage.Contains("kind"))
        {
            relationshipWithPlayer += 5f;
        }
        // Negative interactions
        else if (playerMessage.Contains("stupid") || playerMessage.Contains("hate") || 
                 playerMessage.Contains("idiot") || playerMessage.Contains("dumb"))
        {
            relationshipWithPlayer -= 15f;
        }
        // Neutral interaction
        else
        {
            relationshipWithPlayer += 1f;
        }
        
        // Clamp between 0-100
        relationshipWithPlayer = Mathf.Clamp(relationshipWithPlayer, 0f, 100f);
        
        // Update system prompt with new relationship
        GenerateSystemPrompt();
    }
    
    string GetRelationshipLevel()
    {
        return relationshipWithPlayer.ToString("F0");
    }
    
    string GetRelationshipText()
    {
        if (relationshipWithPlayer > 80) return "Close Friends";
        if (relationshipWithPlayer > 60) return "Friends";
        if (relationshipWithPlayer > 40) return "Acquaintances";
        if (relationshipWithPlayer > 20) return "Strangers";
        return "Suspicious";
    }
    
    [ContextMenu("Test Gemini Connection")]
    void TestConnection()
    {
        if (!enableGemini)
        {
            Debug.Log("Gemini is disabled. Enable it in the inspector.");
            return;
        }
        
        if (apiKey.Contains("xxxx") || string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("Please set your Gemini API key in the inspector!");
            return;
        }
        
        Debug.Log($"Testing Gemini connection for {npcName}...");
        ProcessPlayerMessage("Hello! How are you today?", (response) => {
            Debug.Log($"Test Response: {response}");
        });
    }
    
    [ContextMenu("Reset Conversation History")]
    void ResetHistory()
    {
        conversationHistory.Clear();
        GenerateSystemPrompt();
        Debug.Log($"[{npcName}] Conversation history reset");
    }
    
    // JSON Classes for Gemini API
    [System.Serializable]
    public class GeminiRequest
    {
        public List<Content> contents;
        public GenerationConfig generationConfig;
        public List<SafetySetting> safetySettings;
    }
    
    [System.Serializable]
    public class Content
    {
        public List<Part> parts;
    }
    
    [System.Serializable]
    public class Part
    {
        public string text;
    }
    
    [System.Serializable]
    public class GenerationConfig
    {
        public float temperature;
        public int topK;
        public float topP;
        public int maxOutputTokens;
        public List<string> stopSequences;
    }
    
    [System.Serializable]
    public class SafetySetting
    {
        public string category;
        public string threshold;
    }
    
    [System.Serializable]
    public class GeminiResponse
    {
        public List<Candidate> candidates;
    }
    
    [System.Serializable]
    public class Candidate
    {
        public Content content;
        public string finishReason;
    }
}