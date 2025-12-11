using UnityEngine;
using UnityEditor;

public class HierarchyPrinter
{
    [MenuItem("Tools/Print Hierarchy")]
    static void PrintHierarchy()
    {
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager
                 .GetActiveScene().GetRootGameObjects())
        {
            PrintObject(root.transform, "");
        }
    }

    static void PrintObject(Transform obj, string indent)
    {
        Debug.Log(indent + "- " + obj.name);

        foreach (Transform child in obj)
        {
            PrintObject(child, indent + "  ");
        }
    }
}
