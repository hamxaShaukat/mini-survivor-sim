using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class HierarchyTreePrinter
{
    static HierarchyTreePrinter()
    {
        Debug.Log("HierarchyTreePrinter loaded!");
    }

    [MenuItem("Tools/Print Hierarchy Tree")]
    static void PrintHierarchyTree()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            PrintObject(root, "");
        }
    }

    static void PrintObject(GameObject obj, string indent)
    {
        Debug.Log(indent + "└── " + obj.name);

        foreach (Transform child in obj.transform)
        {
            PrintObject(child.gameObject, indent + "    ");
        }
    }
}
