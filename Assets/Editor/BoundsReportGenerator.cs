using UnityEngine;
using UnityEditor;
using System.IO;

public class BoundsReportGenerator
{
    [MenuItem("Tools/Generate Bounds Report")]
    public static void Generate()
    {
        string report = "--- BOUNDS REPORT ---\n";
        
        string[] prefabs = new string[] 
        {
            "Assets/Prefabs/Obstacle_Block.prefab",
            "Assets/Prefabs/Obstacle_Block 1.prefab",
            "Assets/Prefabs/Obstacle_Block 2.prefab"
        };
        
        foreach (string path in prefabs)
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj != null)
            {
                // Instantiate to get world bounds properly
                GameObject inst = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                inst.transform.position = Vector3.zero;
                inst.transform.rotation = Quaternion.identity;
                
                Collider[] colliders = inst.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    Bounds bounds = col.bounds;
                    report += $"Prefab: {obj.name}\n";
                    report += $"Collider Type: {col.GetType().Name}\n";
                    report += $"Bounds Height: {bounds.size.y}\n";
                    report += $"Bottom Point: {bounds.min.y}\n";
                    report += $"Top Point: {bounds.max.y}\n";
                    report += $"Real World Height: {bounds.max.y - bounds.min.y}\n\n";
                }
                
                GameObject.DestroyImmediate(inst);
            }
        }
        
        File.WriteAllText("BoundsReport.txt", report);
        Debug.Log("Bounds report generated.");
    }
}
