using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class FireFlyBaker : EditorWindow
{
    private int fireflyCount = 10000000;  // 10 million fireflies
    private float radius = 100f;
    private AnimationCurve distributionCurve = AnimationCurve.Linear(0, 0, 1, 1);
    private string fileName = "firefly_positions.json";
    private float NoisePositionScale;
    private float NoiseSampleScale;

    [MenuItem("Tools/Firefly Position Saver")]
    public static void ShowWindow()
    {
        GetWindow<FireFlyBaker>("Firefly Position Saver");
    }

    private void OnGUI()
    {
        GUILayout.Label("Firefly Position Generator", EditorStyles.boldLabel);

        fireflyCount = EditorGUILayout.IntField("Firefly Count", fireflyCount);
        radius = EditorGUILayout.FloatField("Radius", radius);
        distributionCurve = EditorGUILayout.CurveField("Distribution Curve", distributionCurve);
        fileName = EditorGUILayout.TextField("Output File Name", fileName);
        NoisePositionScale = EditorGUILayout.FloatField("Noise Position Scale", NoisePositionScale);
        NoiseSampleScale = EditorGUILayout.FloatField("Noise Sample Scale", NoiseSampleScale);

        if (GUILayout.Button("Generate and Save Positions"))
        {
            StartGeneratingFireflies();
        }
    }

    private void StartGeneratingFireflies()
    {
        // Start generating fireflies
        List<Vector3> fireflyPositions = GenerateFireflyPositions(fireflyCount, radius, distributionCurve);

        // Save positions to a JSON file
        SavePositionsToFile(fireflyPositions, fileName);
    }

    private List<Vector3> GenerateFireflyPositions(int count, float radius, AnimationCurve distributionCurve)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            Vector3 perlinOffset = CalculatePerlinOffset(count);
            Vector3 position = FireFlyUtils.RandomPointInSphereWithCurve(count, radius, distributionCurve) + perlinOffset;
            positions.Add(position);
        }

        Debug.Log($"Generated {positions.Count} firefly positions.");
        return positions;
    }


    public Vector3 CalculatePerlinOffset(int count)
    {
        float xIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
        float perlinX = FireFlyUtils.Remap(Mathf.PerlinNoise1D(xIndex));

        float yIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
        float perlinY = FireFlyUtils.Remap(Mathf.PerlinNoise1D(yIndex));

        float zIndex = (count + UnityEngine.Random.Range(0, count)) * NoiseSampleScale;
        float perlinZ = FireFlyUtils.Remap(Mathf.PerlinNoise1D(zIndex));



        Vector3 perlinoffset = new Vector3(perlinX, perlinY, perlinZ) * NoisePositionScale;

        return perlinoffset;
    }
    

    private void SavePositionsToFile(List<Vector3> positions, string fileName)
    {
        string filePath = Path.Combine(Application.dataPath, fileName);

        // Use a StreamWriter for efficient writing of large data
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var pos in positions)
            {
                // Write each position as "x,y,z" on a new line
                writer.WriteLine($"{pos.x},{pos.y},{pos.z}");
            }
        }

        Debug.Log($"Positions saved to {filePath}");
        
        // Estimate file size
        long estimatedSize = positions.Count * 20; // Approximate bytes per line
        Debug.Log($"Estimated file size: {estimatedSize / (1024 * 1024)} MB");
    }
}
