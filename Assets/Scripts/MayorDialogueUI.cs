using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class MayorDialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI npcTypeText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI reportText;
    public GameObject TreasureryText;
    
    [Header("Task Buttons")]
    public GameObject buttonContainer;
    public GameObject taskButtonPrefab;
    
    [Header("Close Button")]
    public Button closeButton;
    
    private UnifiedNPCController currentNPC;
    private List<GameObject> currentButtons = new List<GameObject>();
    
    void Start()
    {
        dialoguePanel.SetActive(false);
        closeButton.onClick.AddListener(CloseDialogue);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && dialoguePanel.activeSelf)
        {
            CloseDialogue();
        }
    }
    
    public void OpenDialogue(UnifiedNPCController npc)
    {
        if (npc == null || dialoguePanel.activeSelf) return;
        
        currentNPC = npc;
        dialoguePanel.SetActive(true);
        TreasureryText.SetActive(false);
        
        // Update NPC info
        npcNameText.text = npc.npcName;
        npcTypeText.text = npc.npcType.ToString();
        
        // Update status
        UpdateStatusDisplay();
        
        // Create task buttons
        CreateTaskButtons();
        
        // Freeze player (optional)
        FreezePlayer(true);
    }
    
    void UpdateStatusDisplay()
    {
        if (currentNPC == null) return;
        
        string status = currentNPC.isTaskActive ? 
            $"🟡 BUSY: {currentNPC.currentTask}" : 
            "🟢 AVAILABLE";
        
        statusText.text = status;
        reportText.text = currentNPC.GetTaskReport();
    }
    
    void CreateTaskButtons()
    {
        // Clear old buttons
        foreach (var button in currentButtons)
        {
            Destroy(button);
        }
        currentButtons.Clear();
        
        if (currentNPC == null) return;
        
        // Get available tasks based on NPC type
        List<string> tasks = new List<string>();
        
        switch (currentNPC.npcType)
        {
            case UnifiedNPCController.NPCType.Farmer:
                tasks.Add("Harvest Crops");
                tasks.Add("Patrol Village");
                tasks.Add("Follow Mayor");
                tasks.Add("Report Production");
                tasks.Add("Submit to Treasury");
                break;
                
            case UnifiedNPCController.NPCType.Blacksmith:
                tasks.Add("Craft Tools");
                tasks.Add("Forge Weapons");
                tasks.Add("Mine Resources");
                tasks.Add("Patrol Village");
                tasks.Add("Follow Mayor");
                tasks.Add("Report Production");
                tasks.Add("Submit to Treasury");
                break;
                
            case UnifiedNPCController.NPCType.Merchant:
                tasks.Add("Sell Goods");
                tasks.Add("Buy Supplies");
                tasks.Add("Travel to Market");
                tasks.Add("Follow Mayor");
                tasks.Add("Submit to Treasury");
                break;
                
            case UnifiedNPCController.NPCType.Guard:
                tasks.Add("Patrol Village");
                tasks.Add("Guard Treasury");
                tasks.Add("Escort Merchant");
                tasks.Add("Train Militia");
                tasks.Add("Follow Mayor");
                break;
        }
        
        // Create buttons
        float buttonHeight = 40f;
        float spacing = 10f;
        float startY = 0f;
        
        for (int i = 0; i < tasks.Count; i++)
        {
            GameObject buttonObj = Instantiate(taskButtonPrefab, buttonContainer.transform);
            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            
            // Position
            rt.anchoredPosition = new Vector2(0, startY - (i * (buttonHeight + spacing)));
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            
            string taskName = tasks[i];
            buttonText.text = taskName;
            
            // Add listener
            button.onClick.AddListener(() => OnTaskButtonClicked(taskName));
            
            currentButtons.Add(buttonObj);
        }
    }
    
    void OnTaskButtonClicked(string taskName)
    {
        if (currentNPC == null) return;
        
        // Special cases
        if (taskName == "Report Production" || taskName == "Report Earnings")
        {
            // Just show report
            UpdateStatusDisplay();
            return;
        }
        
        if (taskName == "Submit to Treasury")
        {
            currentNPC.SubmitToTreasury();
            UpdateStatusDisplay();
            return;
        }
        
        // Assign task
        currentNPC.AssignTask(taskName);
        
        // Update UI
        UpdateStatusDisplay();
        
        // Show feedback
        ShowTaskAssignedFeedback(taskName);
    }
    
    void ShowTaskAssignedFeedback(string taskName)
    {
        // You can add sound, animation, or popup here
        Debug.Log($"Task assigned: {taskName}");
        
        // Temporary text feedback
        GameObject feedback = new GameObject("Feedback");
        TextMeshProUGUI text = feedback.AddComponent<TextMeshProUGUI>();
        text.text = $"✓ {taskName} Assigned";
        text.color = Color.green;
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        
        // Position
        feedback.transform.SetParent(dialoguePanel.transform);
        RectTransform rt = feedback.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -150);
        
        // Destroy after 2 seconds
        Destroy(feedback, 2f);
    }
    
    void FreezePlayer(bool freeze)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Disable movement scripts
        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
                script.enabled = !freeze;
        }
        
        // Cursor
        Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = freeze;
    }
    
    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        TreasureryText.SetActive(true);
        
        // Clear buttons
        foreach (var button in currentButtons)
        {
            Destroy(button);
        }
        currentButtons.Clear();
        
        // Unfreeze player
        FreezePlayer(false);
        
        currentNPC = null;
    }
    
    void OnGUI()
    {
        // Show "Press T" when near NPC
        if (dialoguePanel.activeSelf) return;
        
        // CHANGE: Find UnifiedNPCController instead of MayorNPCDialogue
        UnifiedNPCController[] npcs = FindObjectsOfType<UnifiedNPCController>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null || npcs.Length == 0) return;
        
        foreach (var npc in npcs)
        {
            // CHANGE: Use npc.transform directly
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            
            // CHANGE: Use a constant interaction radius or add it to UnifiedNPCController
            float interactionRadius = 5f; // You can make this a public variable or get from npc
            
            if (distance <= interactionRadius)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 12;
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = Color.yellow;
                
                GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 70, 200, 30),
                         $"Press T to command {npc.npcName}", style);
                break;
            }
        }
    }
}