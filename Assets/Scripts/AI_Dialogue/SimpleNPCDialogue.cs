using UnityEngine;
using System.Collections.Generic;

public class SimpleNPCDialogue : MonoBehaviour
{
    [Header("NPC Identity")]
    public string npcName = "Jungle Farmer";
    public string occupation = "Farmer";
    
    [Header("Dialogue Settings")]
    public float interactionRadius = 5f;
    public List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();
    
    [System.Serializable]
    public class DialogueEntry
    {
        public string[] playerKeywords; // What player might say
        public string npcResponse;      // NPC's response
        public int relationshipChange;  // + or - points
    }
    
    [Header("NPC State")]
    public int relationshipWithPlayer = 50; // 0-100
    public string currentMood = "Neutral";
    
    private GameObject player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Initialize default dialogues if empty
        if (dialogueEntries.Count == 0)
        {
            CreateDefaultDialogues();
        }
        
        Debug.Log($"[{npcName}] Simple NPC Ready. Radius: {interactionRadius}m");
    }
    
    void CreateDefaultDialogues()
    {
        // Greetings
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "hello", "hi", "hey", "greetings" },
            npcResponse = "Greetings, traveler. The jungle welcomes you.",
            relationshipChange = +5
        });
        
        // Who are you
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "who are you", "your name", "what is your name" },
            npcResponse = $"I'm {npcName}, the {occupation.ToLower()} of these lands.",
            relationshipChange = +3
        });
        
        // What do you do
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "what do you do", "your job", "what is your work" },
            npcResponse = $"I'm a {occupation.ToLower()}. I work with the soil and seasons.",
            relationshipChange = +2
        });
        
        // Jungle questions
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "jungle", "forest", "this place", "where are we" },
            npcResponse = "The jungle is both home and challenge. Respect it.",
            relationshipChange = +2
        });
        
        // Help
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "help", "need help", "assistance" },
            npcResponse = "What do you need? I know these parts well.",
            relationshipChange = +5
        });
        
        // Danger/Safety
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "danger", "safe", "risk", "dangerous" },
            npcResponse = "Stay on marked paths. The jungle has its rules.",
            relationshipChange = +3
        });
        
        // Weather
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "weather", "rain", "sun", "climate" },
            npcResponse = "Weather changes fast here. Always be prepared.",
            relationshipChange = +1
        });
        
        // Goodbye
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "bye", "goodbye", "farewell", "see you" },
            npcResponse = "Safe travels. May the jungle guide you.",
            relationshipChange = +2
        });
        
        // Thank you
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "thank", "thanks", "appreciate" },
            npcResponse = "No need for thanks. We help each other here.",
            relationshipChange = +10
        });
        
        // Rude/Insult
        dialogueEntries.Add(new DialogueEntry {
            playerKeywords = new[] { "stupid", "idiot", "dumb", "hate" },
            npcResponse = "The jungle teaches respect. You should learn.",
            relationshipChange = -20
        });
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Check if player is in radius
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= interactionRadius && Input.GetKeyDown(KeyCode.T))
        {
            StartDialogue();
        }
    }
    
    void StartDialogue()
    {
        SimpleDialogueUI dialogueUI = FindObjectOfType<SimpleDialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.StartDialogueWithNPC(this);
        }
    }
    
    public string GetResponse(string playerMessage)
    {
        playerMessage = playerMessage.ToLower().Trim();
        
        // Update mood based on relationship
        UpdateMood();
        
        // Find matching dialogue entry
        foreach (var entry in dialogueEntries)
        {
            foreach (var keyword in entry.playerKeywords)
            {
                if (playerMessage.Contains(keyword))
                {
                    // Apply relationship change
                    relationshipWithPlayer += entry.relationshipChange;
                    relationshipWithPlayer = Mathf.Clamp(relationshipWithPlayer, 0, 100);
                    
                    // Return response (with mood flavor)
                    return AddMoodFlavor(entry.npcResponse);
                }
            }
        }
        
        // Default response based on relationship
        return GetDefaultResponse();
    }
    
    string AddMoodFlavor(string baseResponse)
    {
        if (relationshipWithPlayer > 75)
            return baseResponse + " Friend.";
        else if (relationshipWithPlayer > 50)
            return baseResponse;
        else if (relationshipWithPlayer > 25)
            return baseResponse + " But be careful.";
        else
            return "Hmm. " + baseResponse;
    }
    
    string GetDefaultResponse()
    {
        string[] neutralResponses = {
            "Interesting. Tell me more.",
            "The jungle holds many secrets.",
            "What brings you here?",
            "Life here is simple but good.",
            "Every day brings new challenges."
        };
        
        string[] friendlyResponses = {
            "Good to talk with you!",
            "Always happy to chat.",
            "You're becoming part of this place.",
            "The jungle feels your presence.",
            "We understand each other better now."
        };
        
        string[] hostileResponses = {
            "I'm busy.",
            "Make it quick.",
            "What now?",
            "Speak plainly.",
            "The jungle watches."
        };
        
        if (relationshipWithPlayer > 70)
            return friendlyResponses[Random.Range(0, friendlyResponses.Length)];
        else if (relationshipWithPlayer > 30)
            return neutralResponses[Random.Range(0, neutralResponses.Length)];
        else
            return hostileResponses[Random.Range(0, hostileResponses.Length)];
    }
    
    void UpdateMood()
    {
        if (relationshipWithPlayer > 80) currentMood = "Friendly";
        else if (relationshipWithPlayer > 60) currentMood = "Warm";
        else if (relationshipWithPlayer > 40) currentMood = "Neutral";
        else if (relationshipWithPlayer > 20) currentMood = "Cautious";
        else currentMood = "Hostile";
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    
    [ContextMenu("Test Dialogue")]
    void TestDialogue()
    {
        Debug.Log($"Test 'hello': {GetResponse("hello")}");
        Debug.Log($"Test 'who are you': {GetResponse("who are you")}");
        Debug.Log($"Relationship: {relationshipWithPlayer}, Mood: {currentMood}");
    }
}