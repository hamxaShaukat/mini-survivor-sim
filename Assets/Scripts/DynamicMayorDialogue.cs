// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using System.Collections.Generic;

// public class DynamicMayorDialogue : MonoBehaviour
// {
//     [Header("UI Prefabs")]
//     public GameObject dialoguePanelPrefab;
//     public GameObject taskButtonPrefab;
//     public GameObject infoTextPrefab;
//     public GameObject headerPrefab;
    
//     [Header("Settings")]
//     public Color availableColor = Color.green;
//     public Color busyColor = Color.yellow;
//     public Color farmerColor = new Color(0.2f, 0.8f, 0.2f); // Green
//     public Color blacksmithColor = new Color(0.8f, 0.5f, 0.2f); // Orange
//     public Color merchantColor = new Color(0.2f, 0.5f, 0.8f); // Blue
    
//     // Runtime references
//     private GameObject currentDialoguePanel;
//     private MayorNPCDialogue currentNPC;
//     private List<GameObject> uiElements = new List<GameObject>();
    
//     void Update()
//     {
//         // Check for T key near NPCs
//         if (Input.GetKeyDown(KeyCode.T) && currentDialoguePanel == null)
//         {
//             CheckForNPCInteraction();
//         }
        
//         // Close with Escape
//         if (Input.GetKeyDown(KeyCode.Escape) && currentDialoguePanel != null)
//         {
//             CloseDialogue();
//         }
//     }
    
//     void CheckForNPCInteraction()
//     {
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player == null) return;
        
//         MayorNPCDialogue[] npcs = FindObjectsOfType<MayorNPCDialogue>();
//         float closestDistance = float.MaxValue;
//         MayorNPCDialogue closestNPC = null;
        
//         foreach (var npc in npcs)
//         {
//             float distance = Vector3.Distance(player.transform.position, npc.transform.position);
//             if (distance <= npc.interactionRadius && distance < closestDistance)
//             {
//                 closestDistance = distance;
//                 closestNPC = npc;
//             }
//         }
        
//         if (closestNPC != null)
//         {
//             OpenDialogue(closestNPC);
//         }
//     }
    
//     void OpenDialogue(MayorNPCDialogue npc)
//     {
//         currentNPC = npc;
        
//         // Create dialogue panel
//         CreateDialoguePanel();
        
//         // Freeze player
//         FreezePlayer(true);
//     }
    
//     void CreateDialoguePanel()
//     {
//         // Destroy any existing UI
//         CloseDialogue();
        
//         // Create main panel
//         currentDialoguePanel = Instantiate(dialoguePanelPrefab);
//         currentDialoguePanel.transform.SetParent(transform, false);
//         uiElements.Add(currentDialoguePanel);
        
//         // Get panel rect
//         RectTransform panelRect = currentDialoguePanel.GetComponent<RectTransform>();
//         panelRect.anchoredPosition = Vector2.zero;
        
//         // Create UI elements dynamically
//         float currentY = 150f; // Start from top
//         float spacing = 40f;
        
//         // 1. NPC Header
//         CreateHeader(currentY, currentNPC);
//         currentY -= 60f;
        
//         // 2. Status Display
//         CreateStatusDisplay(currentY);
//         currentY -= 60f;
        
//         // 3. Task Report
//         CreateTaskReport(currentY);
//         currentY -= 100f;
        
//         // 4. Task Buttons
//         CreateTaskButtons(currentY);
        
//         // 5. Close Button (bottom)
//         CreateCloseButton(-180f);
//     }
    
//     void CreateHeader(float yPos, MayorNPCDialogue npc)
//     {
//         GameObject header = Instantiate(headerPrefab, currentDialoguePanel.transform);
//         RectTransform rt = header.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(0, yPos);
        
//         TextMeshProUGUI text = header.GetComponent<TextMeshProUGUI>();
//         text.text = $"{npc.npcName} - {npc.npcType}";
        
//         // Color based on NPC type
//         switch (npc.npcType)
//         {
//             case MayorNPCDialogue.NPCType.Farmer:
//                 text.color = farmerColor;
//                 break;
//             case MayorNPCDialogue.NPCType.Blacksmith:
//                 text.color = blacksmithColor;
//                 break;
//             case MayorNPCDialogue.NPCType.Merchant:
//                 text.color = merchantColor;
//                 break;
//         }
        
//         text.fontSize = 28;
//         text.alignment = TextAlignmentOptions.Center;
        
//         uiElements.Add(header);
//     }
    
//     void CreateStatusDisplay(float yPos)
//     {
//         GameObject statusObj = Instantiate(infoTextPrefab, currentDialoguePanel.transform);
//         RectTransform rt = statusObj.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(0, yPos);
//         rt.sizeDelta = new Vector2(400, 50);
        
//         TextMeshProUGUI text = statusObj.GetComponent<TextMeshProUGUI>();
//         UpdateStatusText(text);
        
//         text.fontSize = 22;
//         text.alignment = TextAlignmentOptions.Center;
        
//         uiElements.Add(statusObj);
//     }
    
//     void UpdateStatusText(TextMeshProUGUI text)
//     {
//         if (currentNPC == null) return;
        
//         string status = currentNPC.isBusy ? 
//             $"🟡 <color=yellow>BUSY: {currentNPC.currentTask}</color>" : 
//             $"🟢 <color=green>AVAILABLE</color>";
        
//         text.text = status;
//     }
    
//     void CreateTaskReport(float yPos)
//     {
//         GameObject reportObj = Instantiate(infoTextPrefab, currentDialoguePanel.transform);
//         RectTransform rt = reportObj.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(0, yPos);
//         rt.sizeDelta = new Vector2(450, 90);
        
//         TextMeshProUGUI text = reportObj.GetComponent<TextMeshProUGUI>();
//         text.text = currentNPC.GetTaskReport();
        
//         text.fontSize = 18;
//         text.alignment = TextAlignmentOptions.Left;
        
//         // Add background
//         Image bg = reportObj.AddComponent<Image>();
//         bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
//         uiElements.Add(reportObj);
//     }
    
//     void CreateTaskButtons(float startY)
//     {
//         if (currentNPC == null) return;
        
//         List<string> tasks = currentNPC.GetAvailableTasks();
        
//         float buttonWidth = 200f;
//         float buttonHeight = 40f;
//         float spacing = 10f;
        
//         // Calculate grid layout
//         int buttonsPerRow = 2;
//         float totalWidth = (buttonsPerRow * buttonWidth) + ((buttonsPerRow - 1) * spacing);
//         float startX = -totalWidth / 2 + buttonWidth / 2;
        
//         for (int i = 0; i < tasks.Count; i++)
//         {
//             int row = i / buttonsPerRow;
//             int col = i % buttonsPerRow;
            
//             float xPos = startX + (col * (buttonWidth + spacing));
//             float yPos = startY - (row * (buttonHeight + spacing));
            
//             CreateTaskButton(tasks[i], xPos, yPos, buttonWidth, buttonHeight);
//         }
//     }
    
//     void CreateTaskButton(string taskName, float x, float y, float width, float height)
//     {
//         GameObject buttonObj = Instantiate(taskButtonPrefab, currentDialoguePanel.transform);
//         RectTransform rt = buttonObj.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(x, y);
//         rt.sizeDelta = new Vector2(width, height);
        
//         // Button text
//         TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
//         buttonText.text = taskName;
        
//         // Button functionality
//         Button button = buttonObj.GetComponent<Button>();
//         button.onClick.AddListener(() => OnTaskButtonClicked(taskName));
        
//         // Style button based on task type
//         StyleTaskButton(button, taskName);
        
//         uiElements.Add(buttonObj);
//     }
    
//     void StyleTaskButton(Button button, string taskName)
//     {
//         Image buttonImage = button.GetComponent<Image>();
//         ColorBlock colors = button.colors;
        
//         if (taskName.Contains("Follow"))
//         {
//             buttonImage.color = new Color(0.3f, 0.7f, 1f); // Blue
//             colors.normalColor = new Color(0.3f, 0.7f, 1f);
//         }
//         else if (taskName.Contains("Report") || taskName.Contains("Submit"))
//         {
//             buttonImage.color = new Color(1f, 0.8f, 0.3f); // Gold
//             colors.normalColor = new Color(1f, 0.8f, 0.3f);
//         }
//         else
//         {
//             buttonImage.color = new Color(0.4f, 0.8f, 0.4f); // Green
//             colors.normalColor = new Color(0.4f, 0.8f, 0.4f);
//         }
        
//         // Hover color
//         colors.highlightedColor = colors.normalColor * 1.2f;
//         colors.pressedColor = colors.normalColor * 0.8f;
//         button.colors = colors;
//     }
    
//     void CreateCloseButton(float yPos)
//     {
//         GameObject closeButton = Instantiate(taskButtonPrefab, currentDialoguePanel.transform);
//         RectTransform rt = closeButton.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(0, yPos);
//         rt.sizeDelta = new Vector2(150, 40);
        
//         // Text
//         TextMeshProUGUI buttonText = closeButton.GetComponentInChildren<TextMeshProUGUI>();
//         buttonText.text = "Close (ESC)";
//         buttonText.color = Color.white;
        
//         // Button
//         Button button = closeButton.GetComponent<Button>();
//         button.onClick.AddListener(CloseDialogue);
        
//         // Style
//         Image buttonImage = closeButton.GetComponent<Image>();
//         buttonImage.color = new Color(0.8f, 0.2f, 0.2f); // Red
        
//         uiElements.Add(closeButton);
//     }
    
//     void OnTaskButtonClicked(string taskName)
//     {
//         if (currentNPC == null) return;
        
//         Debug.Log($"Task clicked: {taskName}");
        
//         // Handle special tasks
//         if (taskName == "Submit to Treasury")
//         {
//             currentNPC.SubmitToTreasury();
//             ShowFeedback("💰 Submitted to Treasury!", Color.yellow);
//             UpdateAllUI();
//             return;
//         }
        
//         if (taskName.Contains("Report"))
//         {
//             // Just refresh display
//             UpdateAllUI();
//             ShowFeedback("📊 Report Updated", Color.cyan);
//             return;
//         }
        
//         // Assign regular task
//         currentNPC.AssignTask(taskName);
//         ShowFeedback($"✓ {taskName} Assigned", Color.green);
//         UpdateAllUI();
//     }
    
//     void ShowFeedback(string message, Color color)
//     {
//         // Create floating feedback text
//         GameObject feedback = Instantiate(infoTextPrefab, currentDialoguePanel.transform);
//         RectTransform rt = feedback.GetComponent<RectTransform>();
//         rt.anchoredPosition = new Vector2(0, -120);
//         rt.sizeDelta = new Vector2(300, 40);
        
//         TextMeshProUGUI text = feedback.GetComponent<TextMeshProUGUI>();
//         text.text = message;
//         text.color = color;
//         text.fontSize = 20;
//         text.alignment = TextAlignmentOptions.Center;
        
//         // Fade out and destroy
//         Destroy(feedback, 1.5f);
//     }
    
//     void UpdateAllUI()
//     {
//         // Update all dynamic text elements
//         foreach (var element in uiElements)
//         {
//             if (element == null) continue;
            
//             TextMeshProUGUI text = element.GetComponent<TextMeshProUGUI>();
//             if (text != null)
//             {
//                 if (text.text.Contains("BUSY") || text.text.Contains("AVAILABLE"))
//                 {
//                     UpdateStatusText(text);
//                 }
//                 else if (text.text.Contains("Current Task"))
//                 {
//                     text.text = currentNPC.GetTaskReport();
//                 }
//             }
//         }
//     }
    
//     void FreezePlayer(bool freeze)
//     {
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player == null) return;
        
//         MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
//         foreach (var script in scripts)
//         {
//             if (script != this)
//                 script.enabled = !freeze;
//         }
        
//         Cursor.lockState = freeze ? CursorLockMode.None : CursorLockMode.Locked;
//         Cursor.visible = freeze;
//     }
    
//     void CloseDialogue()
//     {
//         // Destroy all UI elements
//         foreach (var element in uiElements)
//         {
//             if (element != null)
//                 Destroy(element);
//         }
        
//         uiElements.Clear();
//         currentDialoguePanel = null;
        
//         // Unfreeze player
//         FreezePlayer(false);
        
//         currentNPC = null;
//     }
    
//     void OnGUI()
//     {
//         // Show interaction prompt
//         if (currentDialoguePanel != null) return;
        
//         GameObject player = GameObject.FindGameObjectWithTag("Player");
//         if (player == null) return;
        
//         MayorNPCDialogue[] npcs = FindObjectsOfType<MayorNPCDialogue>();
        
//         foreach (var npc in npcs)
//         {
//             float distance = Vector3.Distance(player.transform.position, npc.transform.position);
//             if (distance <= npc.interactionRadius)
//             {
//                 // Dynamic prompt with NPC info
//                 string prompt = $"[T] Command {npc.npcName} ({npc.npcType})";
                
//                 GUIStyle style = new GUIStyle(GUI.skin.label);
//                 style.alignment = TextAnchor.MiddleCenter;
//                 style.fontSize = 16;
//                 style.fontStyle = FontStyle.Bold;
                
//                 // Color based on NPC type
//                 switch (npc.npcType)
//                 {
//                     case MayorNPCDialogue.NPCType.Farmer:
//                         style.normal.textColor = farmerColor;
//                         break;
//                     case MayorNPCDialogue.NPCType.Blacksmith:
//                         style.normal.textColor = blacksmithColor;
//                         break;
//                     case MayorNPCDialogue.NPCType.Merchant:
//                         style.normal.textColor = merchantColor;
//                         break;
//                 }
                
//                 // Background
//                 GUI.color = new Color(0, 0, 0, 0.7f);
//                 GUI.Box(new Rect(Screen.width/2 - 110, Screen.height - 80, 220, 35), "");
                
//                 // Text
//                 GUI.color = Color.white;
//                 GUI.Label(new Rect(Screen.width/2 - 100, Screen.height - 75, 200, 25), 
//                          prompt, style);
                
//                 break;
//             }
//         }
//     }
// }