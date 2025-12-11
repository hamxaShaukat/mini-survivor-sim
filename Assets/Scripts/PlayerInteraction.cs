using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.T;
    
    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }
    
    void TryInteract()
    {
    
        if (TryMerchantInteraction())
            return;
            
        TryNPCInteraction();
    }
    
    bool TryMerchantInteraction()
    {
        MerchantInteraction[] merchants = FindObjectsOfType<MerchantInteraction>();
        float closestDistance = float.MaxValue;
        MerchantInteraction closestMerchant = null;
        
        foreach (var merchant in merchants)
        {
            float distance = Vector3.Distance(transform.position, merchant.transform.position);
            if (distance <= merchant.interactionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestMerchant = merchant;
            }
        }
        
        if (closestMerchant != null)
        {
            MerchantUI merchantUI = FindObjectOfType<MerchantUI>();
            if (merchantUI != null)
            {
                merchantUI.OpenMerchantUI(closestMerchant);
                return true;
            }
        }
        
        return false;
    }
    
    bool TryNPCInteraction()
    {
        UnifiedNPCController[] npcs = FindObjectsOfType<UnifiedNPCController>();
        float closestDistance = float.MaxValue;
        UnifiedNPCController closestNPC = null;
        
        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestNPC = npc;
            }
        }
        
        if (closestNPC != null)
        {
            MayorDialogueUI dialogueUI = FindObjectOfType<MayorDialogueUI>();
            if (dialogueUI != null)
            {
                dialogueUI.OpenDialogue(closestNPC);
                return true;
            }
        }
        
        return false;
    }
    
    // Visual feedback for interactable objects
    void OnGUI()
    {
        // Check for nearby interactables
        bool showNPCPrompt = false;
        bool showMerchantPrompt = false;
        string npcName = "";
        string merchantName = "";
        
        // Check NPCs
        UnifiedNPCController[] npcs = FindObjectsOfType<UnifiedNPCController>();
        foreach (var npc in npcs)
        {
            float distance = Vector3.Distance(transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius)
            {
                showNPCPrompt = true;
                npcName = npc.npcName;
                break;
            }
        }
        
        // Check Merchants
        MerchantInteraction[] merchants = FindObjectsOfType<MerchantInteraction>();
        foreach (var merchant in merchants)
        {
            float distance = Vector3.Distance(transform.position, merchant.transform.position);
            if (distance <= merchant.interactionRadius)
            {
                showMerchantPrompt = true;
                merchantName = merchant.merchantName;
                break;
            }
        }
        
        // Show appropriate prompt
        if (showMerchantPrompt)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.cyan;
            
            GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 70, 200, 30),
                     $"Press T to trade with {merchantName}", style);
        }
        else if (showNPCPrompt)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.yellow;
            
            GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 70, 200, 30),
                     $"Press T to command {npcName}", style);
        }
    }
}