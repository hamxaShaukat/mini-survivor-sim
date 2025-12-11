using UnityEngine;
using System.Collections.Generic;

public class NPCManager : MonoBehaviour
{
    [Header("NPC Management")]
    public List<SmartDialogueSystem> allNPCs = new List<SmartDialogueSystem>();
    public Transform playerTransform;
    
    [Header("Global NPC Settings")]
    public bool enableNPCInteractions = true;
    public float minInteractionDistance = 10f;
    public TimeOfDay currentTime = TimeOfDay.Morning;
    
    public enum TimeOfDay { Morning, Afternoon, Evening, Night }
    
    void Start()
    {
        // Find all NPCs automatically if not assigned
        if (allNPCs.Count == 0)
        {
            SmartDialogueSystem[] foundNPCs = FindObjectsOfType<SmartDialogueSystem>();
            allNPCs.AddRange(foundNPCs);
        }
        
        // Find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) playerTransform = player.transform;
        }
        
        Debug.Log($"NPC Manager initialized with {allNPCs.Count} NPCs");
    }
    
    void Update()
    {
        if (!enableNPCInteractions || playerTransform == null)
            return;
        
        // Update each NPC's awareness of player
        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;
            
            float distanceToPlayer = Vector3.Distance(npc.transform.position, playerTransform.position);
            bool playerIsNear = distanceToPlayer < minInteractionDistance;
            
            // NPCs could react to player proximity
            if (playerIsNear && distanceToPlayer < 3f)
            {
                // NPC might look at player
                LookAtPlayer(npc);
            }
        }
        
        // Update time of day (simplified)
        UpdateTimeOfDay();
    }
    
    void LookAtPlayer(SmartDialogueSystem npc)
    {
        if (playerTransform == null || npc.isBusy) return;
        
        Vector3 direction = playerTransform.position - npc.transform.position;
        direction.y = 0; // Keep upright
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, 
                targetRotation, Time.deltaTime * 2f);
        }
    }
    
    void UpdateTimeOfDay()
    {
        // Simple time simulation (every 2 minutes cycles through day)
        float time = Time.time % 480f; // 8 minute day cycle
        if (time < 120f) currentTime = TimeOfDay.Morning;
        else if (time < 240f) currentTime = TimeOfDay.Afternoon;
        else if (time < 360f) currentTime = TimeOfDay.Evening;
        else currentTime = TimeOfDay.Night;
    }
    
    // Get NPC by name
    public SmartDialogueSystem GetNPC(string npcName)
    {
        return allNPCs.Find(npc => npc.knowledge.npcName.Equals(npcName, System.StringComparison.OrdinalIgnoreCase));
    }
    
    // Get all NPCs of a certain occupation
    public List<SmartDialogueSystem> GetNPCsByOccupation(string occupation)
    {
        return allNPCs.FindAll(npc => npc.knowledge.occupation.Equals(occupation, System.StringComparison.OrdinalIgnoreCase));
    }
    
    // Make NPCs react to global event
    public void TriggerGlobalEvent(string eventName)
    {
        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;
            
            switch (eventName)
            {
                case "RainStart":
                    // NPCs might comment on rain
                    Debug.Log($"{npc.knowledge.npcName}: Rain! Better take cover.");
                    npc.mood -= 10; // Rain makes some NPCs less happy
                    break;
                    
                case "DangerNearby":
                    Debug.Log($"{npc.knowledge.npcName}: Did you hear that? Something's out there.");
                    npc.mood -= 20;
                    break;
                    
                case "Festival":
                    Debug.Log($"{npc.knowledge.npcName}: Festival time! Everyone's happy!");
                    npc.mood += 30;
                    break;
            }
            
            npc.mood = Mathf.Clamp(npc.mood, 0, 100);
        }
    }
    
    // Quick debug commands
    [ContextMenu("Print All NPC Status")]
    void PrintAllNPCStatus()
    {
        Debug.Log("=== NPC STATUS REPORT ===");
        foreach (var npc in allNPCs)
        {
            if (npc != null)
            {
                Debug.Log($"{npc.knowledge.npcName} ({npc.knowledge.occupation}): " +
                         $"Mood: {npc.mood:F0}, Energy: {npc.energy:F0}, " +
                         $"Conversations: {npc.memory.conversationCount}");
            }
        }
    }
    
    [ContextMenu("Reset All NPC Moods")]
    void ResetAllMoods()
    {
        foreach (var npc in allNPCs)
        {
            if (npc != null)
            {
                npc.mood = 75f;
                npc.energy = 80f;
            }
        }
        Debug.Log("All NPC moods reset to normal");
    }
}