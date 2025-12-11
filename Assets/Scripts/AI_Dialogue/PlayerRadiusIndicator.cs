using UnityEngine;

public class PlayerRadiusIndicator : MonoBehaviour
{
    public float checkInterval = 0.2f;
    
    private GameObject player;
    private SimpleOllamaNPC nearestNPC;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        
        InvokeRepeating("CheckForNPCs", 0f, checkInterval);
    }
    
    void CheckForNPCs()
    {
        if (player == null) return;
        
        SimpleOllamaNPC[] allNPCs = FindObjectsOfType<SimpleOllamaNPC>();
        nearestNPC = null;
        float closestDistance = float.MaxValue;
        
        foreach (var npc in allNPCs)
        {
            float distance = Vector3.Distance(player.transform.position, npc.transform.position);
            if (distance <= npc.interactionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                nearestNPC = npc;
            }
        }
    }
    
    void OnGUI()
    {
        if (nearestNPC != null)
        {
            // Visual indicator
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 16;
            style.normal.textColor = Color.yellow;
            
            string npcInfo = $"{nearestNPC.npcName} (Press T)";
            GUI.Label(new Rect(Screen.width/2 - 150, Screen.height - 120, 300, 25), 
                     npcInfo, style);
        }
    }
}