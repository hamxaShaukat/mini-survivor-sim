using UnityEngine;
using System.Collections.Generic;

public class PathNetworkManager : MonoBehaviour
{
    public static PathNetworkManager Instance;
    
    [System.Serializable]
    public class PathRoute
    {
        public string routeName;
        public Transform[] mainPath;      // Main route points
        public Transform[] branchPoints;  // Where branches split off
        public Dictionary<string, Transform[]> branches = new Dictionary<string, Transform[]>();
    }
    
    [Header("=== DIRECT PATHS: HOME → CROPS ===")]
    public Transform[] home1ToWheatPath;
    public Transform[] home2ToWheatPath;
    public Transform[] home3ToWheatPath;
    public Transform[] home4ToWheatPath;
    public Transform[] home5ToWheatPath;
    public Transform[] home6ToWheatPath;
    
    public Transform[] home1ToCornPath;
    public Transform[] home2ToCornPath;
    public Transform[] home3ToCornPath;
    public Transform[] home4ToCornPath;
    public Transform[] home5ToCornPath;
    public Transform[] home6ToCornPath;
    
    [Header("=== CROP → MARKET ROUTES ===")]
    public PathRoute wheatToMarketRoute;
    public PathRoute cornToMarketRoute;
    
    [Header("=== MARKET → HOME ROUTE ===")]
    public Transform[] marketToHomeMain;  // From X junction to homes area
    
    [Header("=== STALL LOCATIONS ===")]
    public Transform stall1Location;
    public Transform stall2Location;
    public Transform stall3Location;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupBranches();
            Debug.Log("PathNetworkManager initialized with direct home→crop paths");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void SetupBranches()
    {
        // Initialize branch dictionaries
        wheatToMarketRoute.branches = new Dictionary<string, Transform[]>();
        cornToMarketRoute.branches = new Dictionary<string, Transform[]>();
    }
    
    // ========== PUBLIC METHODS ==========
    
    // Get path from home to specific crop field
    public Transform[] GetHomeToCropPath(int homeNumber, string cropType)
    {
        return cropType == "Wheat" ? 
               GetWheatPathFromHome(homeNumber) : 
               GetCornPathFromHome(homeNumber);
    }
    
    // Get path from crop back to home (reverse of home→crop)
    public Transform[] GetCropToHomePath(int homeNumber, string cropType)
    {
        Transform[] forwardPath = GetHomeToCropPath(homeNumber, cropType);
        return ReversePath(forwardPath);
    }
    
    // Get path from crop to market (with optional stall branch)
    public Transform[] GetCropToMarketPath(string cropType, string stallName = "")
    {
        PathRoute route = cropType == "Wheat" ? wheatToMarketRoute : cornToMarketRoute;
        
        if (string.IsNullOrEmpty(stallName))
            return route.mainPath;
        
        if (route.branches.ContainsKey(stallName) && route.branches[stallName] != null)
        {
            return CombinePaths(route.mainPath, route.branches[stallName]);
        }
        
        Debug.LogWarning($"No branch to {stallName}, using main path only");
        return route.mainPath;
    }
    
    // Get path from market back to crop (reverse)
    public Transform[] GetMarketToCropPath(string cropType, string stallName = "")
    {
        Transform[] forwardPath = GetCropToMarketPath(cropType, stallName);
        return ReversePath(forwardPath);
    }
    
    // Get path from market to home (via X junction)
    public Transform[] GetMarketToHomePath(int homeNumber)
    {
        Transform[] homePath = GetHomePathFromMarketJunction(homeNumber);
        return CombinePaths(marketToHomeMain, homePath);
    }
    
    // Get path from home to market (reverse of market→home)
    public Transform[] GetHomeToMarketPath(int homeNumber)
    {
        Transform[] forwardPath = GetMarketToHomePath(homeNumber);
        return ReversePath(forwardPath);
    }
    
    // ========== PRIVATE HELPER METHODS ==========
    
    private Transform[] GetWheatPathFromHome(int homeNumber)
    {
        switch (homeNumber)
        {
            case 1: return home1ToWheatPath;
            case 2: return home2ToWheatPath;
            case 3: return home3ToWheatPath;
            case 4: return home4ToWheatPath;
            case 5: return home5ToWheatPath;
            case 6: return home6ToWheatPath;
            default: 
                Debug.LogError($"No wheat path for home {homeNumber}");
                return new Transform[0];
        }
    }
    
    private Transform[] GetCornPathFromHome(int homeNumber)
    {
        switch (homeNumber)
        {
            case 1: return home1ToCornPath;
            case 2: return home2ToCornPath;
            case 3: return home3ToCornPath;
            case 4: return home4ToCornPath;
            case 5: return home5ToCornPath;
            case 6: return home6ToCornPath;
            default: 
                Debug.LogError($"No corn path for home {homeNumber}");
                return new Transform[0];
        }
    }
    
    private Transform[] GetHomePathFromMarketJunction(int homeNumber)
    {
        // These are the paths from the X junction to each home
        // You need to create these in your scene
        // For now, returning empty - you'll create these paths
        
        Debug.Log($"Create path from market junction to home {homeNumber}");
        return new Transform[0]; // You'll fill this
    }
    
    private Transform[] ReversePath(Transform[] path)
    {
        if (path == null || path.Length == 0) return new Transform[0];
        
        Transform[] reversed = new Transform[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            reversed[i] = path[path.Length - 1 - i];
        }
        return reversed;
    }
    
    private Transform[] CombinePaths(Transform[] path1, Transform[] path2)
    {
        if (path1 == null || path1.Length == 0) return path2;
        if (path2 == null || path2.Length == 0) return path1;
        
        List<Transform> combined = new List<Transform>();
        combined.AddRange(path1);
        combined.AddRange(path2);
        return combined.ToArray();
    }
    
    // ========== EDITOR VISUALIZATION ==========
    
    void OnDrawGizmos()
    {
        DrawAllPaths();
    }
    
    void DrawAllPaths()
    {
        // Draw wheat paths in green
        DrawPathArray(home1ToWheatPath, Color.green, "Home1→Wheat");
        DrawPathArray(home2ToWheatPath, Color.green, "Home2→Wheat");
        DrawPathArray(home3ToWheatPath, Color.green, "Home3→Wheat");
        DrawPathArray(home4ToWheatPath, Color.green, "Home4→Wheat");
        DrawPathArray(home5ToWheatPath, Color.green, "Home5→Wheat");
        DrawPathArray(home6ToWheatPath, Color.green, "Home6→Wheat");
        
        // Draw corn paths in yellow
        DrawPathArray(home1ToCornPath, Color.yellow, "Home1→Corn");
        DrawPathArray(home2ToCornPath, Color.yellow, "Home2→Corn");
        DrawPathArray(home3ToCornPath, Color.yellow, "Home3→Corn");
        DrawPathArray(home4ToCornPath, Color.yellow, "Home4→Corn");
        DrawPathArray(home5ToCornPath, Color.yellow, "Home5→Corn");
        DrawPathArray(home6ToCornPath, Color.yellow, "Home6→Corn");
        
        // Draw crop→market routes
        if (wheatToMarketRoute.mainPath != null)
            DrawPathArray(wheatToMarketRoute.mainPath, Color.cyan, "Wheat→Market");
        
        if (cornToMarketRoute.mainPath != null)
            DrawPathArray(cornToMarketRoute.mainPath, Color.magenta, "Corn→Market");
        
        // Draw market→home main route
        if (marketToHomeMain != null)
            DrawPathArray(marketToHomeMain, Color.blue, "Market→Homes");
        
        // Draw stall locations
        Gizmos.color = Color.red;
        if (stall1Location != null) 
        {
            Gizmos.DrawSphere(stall1Location.position, 0.8f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(stall1Location.position + Vector3.up, "Stall1");
            #endif
        }
        if (stall2Location != null) 
        {
            Gizmos.DrawSphere(stall2Location.position, 0.8f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(stall2Location.position + Vector3.up, "Stall2");
            #endif
        }
        if (stall3Location != null) 
        {
            Gizmos.DrawSphere(stall3Location.position, 0.8f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(stall3Location.position + Vector3.up, "Stall3");
            #endif
        }
    }
    
    void DrawPathArray(Transform[] path, Color color, string label)
    {
        if (path == null || path.Length == 0) return;
        
        Gizmos.color = color;
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (path[i] != null && path[i + 1] != null)
            {
                Gizmos.DrawLine(path[i].position, path[i + 1].position);
                Gizmos.DrawSphere(path[i].position, 0.2f);
            }
        }
        
        // Draw label at start
        if (path[0] != null)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(path[0].position + Vector3.up * 0.5f, label);
            #endif
        }
    }
}