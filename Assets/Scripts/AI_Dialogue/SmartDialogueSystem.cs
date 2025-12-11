using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SmartDialogueSystem : MonoBehaviour
{
    [System.Serializable]
    public class NPCKnowledge
    {
        [Header("NPC Identity")]
        public string npcName = "Farmer";
        public string occupation = "Farmer";
        public string personality = "Friendly";
        
        [Header("Knowledge Database")]
        public string[] aboutJungle = {
            "The jungle gets dangerous at night. Predators come out.",
            "I saw a tiger near the river yesterday evening.",
            "Monsoon season starts next month. Need to harvest before that.",
            "There are medicinal herbs near the waterfall.",
            "The old temple ruins are west of here. But be careful."
        };
        
        public string[] aboutWork = {
            "Harvest is good this season. Soil is fertile.",
            "Need to sell these crops before they spoil.",
            "Blacksmith makes the best tools in this region.",
            "Working from sunrise to sunset. Hard but honest work.",
            "My father taught me how to farm. Family tradition."
        };
        
        public string[] personalThoughts = {
            "I enjoy the simple life here.",
            "Sometimes I miss the city, but jungle is home.",
            "Hard work always pays off in the end.",
            "Family is the most important thing.",
            "I believe in living in harmony with nature."
        };
        
        public string[] greetings = {
            "Hello there, traveler!",
            "Good to see you!",
            "Hello! Need something?",
            "Ah, a visitor! Welcome.",
            "Greetings from the jungle!"
        };
        
        public string[] farewells = {
            "Stay safe out there!",
            "Come back anytime!",
            "May the jungle protect you.",
            "Till we meet again!",
            "Take care on your journey."
        };
    }
    
    [System.Serializable]
    public class ConversationMemory
    {
        public List<string> recentConversations = new List<string>();
        public string lastTopic = "";
        public int conversationCount = 0;
        
        public void AddConversation(string playerSaid, string npcReplied)
        {
            string entry = $"Player: {playerSaid}\nNPC: {npcReplied}";
            recentConversations.Add(entry);
            
            if (recentConversations.Count > 5)
                recentConversations.RemoveAt(0);
            
            conversationCount++;
        }
        
        public bool HasDiscussed(string topic)
        {
            return recentConversations.Any(c => c.ToLower().Contains(topic.ToLower()));
        }
    }
    
    [Header("NPC Configuration")]
    public NPCKnowledge knowledge = new NPCKnowledge();
    public ConversationMemory memory = new ConversationMemory();
    
    [Header("Current State")]
    public float mood = 75f; // 0-100
    public float energy = 80f;
    public bool isBusy = false;
    
    // References (will be set automatically)
    private FarmerFieldWork farmerWork;
    private Animator animator;
    
    void Start()
    {
        farmerWork = GetComponent<FarmerFieldWork>();
        animator = GetComponent<Animator>();
        
        // If this is Blacksmith, change occupation
        if (gameObject.name.Contains("Blacksmith", System.StringComparison.OrdinalIgnoreCase))
        {
            knowledge.occupation = "Blacksmith";
            knowledge.npcName = "Blacksmith";
            knowledge.aboutWork = new string[] {
                "The forge is hot today! Perfect for shaping metal.",
                "I make the best tools and weapons in the jungle.",
                "Need something forged? I work with iron and steel.",
                "My grandfather taught me this craft. Three generations now.",
                "Good tools make all the difference in jungle life."
            };
        }
    }
    
    void Update()
    {
        // Update energy based on activity
        if (animator != null)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName("Gathering") || state.IsName("Work"))
            {
                energy = Mathf.Max(0, energy - Time.deltaTime * 0.5f);
            }
        }
        
        // Update mood
        if (energy < 30) mood -= Time.deltaTime * 0.2f;
        else mood += Time.deltaTime * 0.1f;
        mood = Mathf.Clamp(mood, 0, 100);
    }
    
    // MAIN METHOD: Call this when player wants to talk
    public string ProcessPlayerInput(string playerInput)
    {
        // Check if NPC is busy
        if (farmerWork != null && (farmerWork.isSelling || farmerWork.isGoingToMerchant))
        {
            isBusy = true;
            return "I'm a bit busy right now. Can we talk later?";
        }
        
        isBusy = false;
        playerInput = playerInput.ToLower().Trim();
        
        // Analyze and generate response
        string response = GenerateResponse(playerInput);
        
        // Add to memory
        memory.AddConversation(playerInput, response);
        memory.lastTopic = ExtractTopic(playerInput);
        
        // Adjust mood based on conversation
        if (playerInput.Contains("thank") || playerInput.Contains("appreciate"))
            mood = Mathf.Min(100, mood + 10);
        
        if (playerInput.Contains("stupid") || playerInput.Contains("hate"))
            mood = Mathf.Max(0, mood - 15);
        
        return response;
    }
    
    string GenerateResponse(string playerInput)
    {
        // GREETINGS
        if (IsGreeting(playerInput))
            return GetGreeting() + " " + GetMoodComment();
        
        // FAREWELLS
        if (IsFarewell(playerInput))
            return knowledge.farewells[Random.Range(0, knowledge.farewells.Length)];
        
        // QUESTION DETECTION
        if (playerInput.StartsWith("what") || playerInput.StartsWith("how") || 
            playerInput.StartsWith("where") || playerInput.StartsWith("why"))
        {
            return AnswerQuestion(playerInput);
        }
        
        // STATEMENTS/COMMENTS
        return RespondToStatement(playerInput);
    }
    
    string GetGreeting()
    {
        if (memory.conversationCount == 0)
            return $"Hello! I'm {knowledge.npcName}, the {knowledge.occupation.ToLower()} here.";
        
        if (memory.conversationCount > 3)
            return $"Welcome back! Good to see you again.";
        
        return knowledge.greetings[Random.Range(0, knowledge.greetings.Length)];
    }
    
    string GetMoodComment()
    {
        if (energy < 30) return "I'm quite tired from working all day.";
        if (mood > 80) return "Feeling great today!";
        if (mood < 40) return "Not having the best day, but okay.";
        
        if (farmerWork != null && farmerWork.goodsProduced > 0)
            return $"Harvested {farmerWork.goodsProduced} crops already!";
        
        return "The jungle is peaceful today.";
    }
    
    string AnswerQuestion(string question)
    {
        // WHAT questions
        if (question.Contains("what do you do") || question.Contains("what is your job"))
            return $"I'm a {knowledge.occupation.ToLower()}. " + knowledge.aboutWork[Random.Range(0, knowledge.aboutWork.Length)];
        
        if (question.Contains("what is") && question.Contains("jungle"))
            return knowledge.aboutJungle[Random.Range(0, knowledge.aboutJungle.Length)];
        
        if (question.Contains("what do you think") || question.Contains("what is your opinion"))
            return knowledge.personalThoughts[Random.Range(0, knowledge.personalThoughts.Length)];
        
        if (question.Contains("what is your name"))
            return $"I'm {knowledge.npcName}. People here know me as the {knowledge.occupation.ToLower()}.";
        
        // HOW questions
        if (question.Contains("how are you"))
            return GetHowAreYouResponse();
        
        if (question.Contains("how is work") || question.Contains("how is business"))
            return GetWorkStatusResponse();
        
        // WHERE questions
        if (question.Contains("where is") && question.Contains("merchant"))
            return "The merchant is east of here, near the big banyan tree.";
        
        if (question.Contains("where is") && (question.Contains("river") || question.Contains("water")))
            return "Follow the animal path north. You'll find the river.";
        
        // WHY questions
        if (question.Contains("why are you"))
            return "That's an interesting question. " + knowledge.personalThoughts[Random.Range(0, knowledge.personalThoughts.Length)];
        
        // DEFAULT ANSWER for unknown questions
        return "Hmm, let me think... " + GetPhilosophicalResponse();
    }
    
    string RespondToStatement(string statement)
    {
        // Player shares something about themselves
        if (statement.Contains("i am") || statement.Contains("my name is"))
            return "Nice to meet you! " + knowledge.personalThoughts[Random.Range(0, knowledge.personalThoughts.Length)];
        
        // Player comments on jungle
        if (statement.Contains("jungle") || statement.Contains("forest"))
            return "Yes, the jungle is special. " + knowledge.aboutJungle[Random.Range(0, knowledge.aboutJungle.Length)];
        
        // Player mentions danger or problems
        if (statement.Contains("danger") || statement.Contains("problem") || statement.Contains("help"))
            return "The jungle can be dangerous. Be careful out there. Maybe I can help?";
        
        // Player gives compliment
        if (statement.Contains("good") || statement.Contains("nice") || statement.Contains("great"))
            return "Thank you! " + (mood > 60 ? "I appreciate that." : "That means a lot.");
        
        // DEFAULT: Engage thoughtfully
        return GetThoughtfulResponse(statement);
    }
    
    // Helper Methods
    string GetHowAreYouResponse()
    {
        if (energy < 30) return "Tired, but still working. The jungle doesn't wait.";
        if (mood > 80) return "Wonderful! The harvest is good and weather is perfect.";
        if (mood < 40) return "Could be better. Had some troubles with wild animals.";
        return "I'm doing well, thank you for asking.";
    }
    
    string GetWorkStatusResponse()
    {
        if (farmerWork != null)
        {
            if (farmerWork.goodsProduced > 5) 
                return "Excellent! Harvested plenty today.";
            if (farmerWork.completedCycles > farmerWork.workCycles/2)
                return "Halfway through the day's work. Making progress.";
        }
        return "Work is steady. As a " + knowledge.occupation.ToLower() + ", every day brings new challenges.";
    }
    
    string GetPhilosophicalResponse()
    {
        string[] philosophical = {
            "You know, in the jungle, everything is connected.",
            "Life here teaches you patience and respect for nature.",
            "Sometimes the answer isn't in words, but in observing.",
            "Every question leads to more questions. That's how we learn.",
            "The jungle has its own wisdom. We just need to listen."
        };
        return philosophical[Random.Range(0, philosophical.Length)];
    }
    
    string GetThoughtfulResponse(string about)
    {
        string[] connectors = {
            "About that... ",
            "You know what I think? ",
            "That reminds me... ",
            "Interesting you mention that. ",
            "I've been thinking about something similar. "
        };
        
        return connectors[Random.Range(0, connectors.Length)] + 
               knowledge.personalThoughts[Random.Range(0, knowledge.personalThoughts.Length)];
    }
    
    bool IsGreeting(string text)
    {
        string[] greetings = {"hello", "hi", "hey", "greetings", "good morning", "good afternoon"};
        return greetings.Any(g => text.Contains(g));
    }
    
    bool IsFarewell(string text)
    {
        string[] farewells = {"bye", "goodbye", "see you", "farewell", "take care"};
        return farewells.Any(f => text.Contains(f));
    }
    
    string ExtractTopic(string text)
    {
        if (text.Contains("jungle") || text.Contains("forest")) return "jungle";
        if (text.Contains("work") || text.Contains("job")) return "work";
        if (text.Contains("family") || text.Contains("home")) return "personal";
        if (text.Contains("danger") || text.Contains("safe")) return "safety";
        return "general";
    }
    
    // Quick test method
    [ContextMenu("Test Dialogue")]
    void TestDialogue()
    {
        string[] testInputs = {
            "Hello!",
            "How are you today?",
            "What do you do here?",
            "The jungle is beautiful",
            "Where is the merchant?",
            "Goodbye!"
        };
        
        foreach (string input in testInputs)
        {
            Debug.Log($"Player: {input}");
            Debug.Log($"{knowledge.npcName}: {ProcessPlayerInput(input)}");
        }
    }
}