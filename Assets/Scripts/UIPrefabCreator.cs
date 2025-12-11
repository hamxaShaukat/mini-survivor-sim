#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create Mayor UI Prefabs")]
    static void Init()
    {
        GetWindow<UIPrefabCreator>("UI Prefab Creator").Show();
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("Create All UI Prefabs"))
        {
            CreatePrefabs();
        }
    }
    
    void CreatePrefabs()
    {
        // Create prefabs folder if not exists
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }
        
        // 1. Dialogue Panel Prefab
        CreateDialoguePanelPrefab();
        
        // 2. Task Button Prefab
        CreateTaskButtonPrefab();
        
        // 3. Info Text Prefab
        CreateInfoTextPrefab();
        
        // 4. Header Prefab
        CreateHeaderPrefab();
        
        Debug.Log("✅ All UI prefabs created in Assets/Prefabs/UI/");
    }
    
    void CreateDialoguePanelPrefab()
    {
        GameObject panel = new GameObject("DialoguePanel");
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(500, 500);
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(panel, "Assets/Prefabs/UI/DialoguePanel.prefab");
        DestroyImmediate(panel);
    }
    
    void CreateTaskButtonPrefab()
    {
        GameObject button = new GameObject("TaskButton");
        RectTransform rt = button.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 40);
        
        Image img = button.AddComponent<Image>();
        img.color = new Color(0.3f, 0.6f, 0.3f);
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        
        Button btn = button.AddComponent<Button>();
        
        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(5, 5);
        textRt.offsetMax = new Vector2(-5, -5);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Task Button";
        text.color = Color.white;
        text.fontSize = 16;
        text.alignment = TextAlignmentOptions.Center;
        
        PrefabUtility.SaveAsPrefabAsset(button, "Assets/Prefabs/UI/TaskButton.prefab");
        DestroyImmediate(button);
    }
    
    void CreateInfoTextPrefab()
    {
        GameObject textObj = new GameObject("InfoText");
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 30);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Info Text";
        text.color = Color.white;
        text.fontSize = 18;
        
        PrefabUtility.SaveAsPrefabAsset(textObj, "Assets/Prefabs/UI/InfoText.prefab");
        DestroyImmediate(textObj);
    }
    
    void CreateHeaderPrefab()
    {
        GameObject header = new GameObject("Header");
        RectTransform rt = header.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 50);
        
        TextMeshProUGUI text = header.AddComponent<TextMeshProUGUI>();
        text.text = "NPC Header";
        text.color = Color.white;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        
        PrefabUtility.SaveAsPrefabAsset(header, "Assets/Prefabs/UI/Header.prefab");
        DestroyImmediate(header);
    }
}
#endif