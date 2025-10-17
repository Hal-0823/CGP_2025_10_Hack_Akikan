using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CanGenerator : MonoBehaviour
{
    private const float laneWidth = 0.1f;

    [SerializeField]
    private Vector3 generateOffset;

    public List<GameObject> Lanes;

    public GameObject canPrefab;

    public void GenerateCan(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= Lanes.Count)
        {
            Debug.LogError("Invalid lane index: " + laneIndex);
            return;
        }

        Transform lane = Lanes[laneIndex].transform;
        Vector3 generatePosition = lane.position + generateOffset + new Vector3(Random.Range(-laneWidth / 2, laneWidth / 2), 0, 0);
        Instantiate(canPrefab, generatePosition, Quaternion.Euler(-90, 0, 0), lane);
    }
}

[CustomEditor(typeof(CanGenerator))]
public class CanGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        CanGenerator canGenerator = (CanGenerator)target;

        if (GUILayout.Button("Generate Can in Lane 0"))
        {
            canGenerator.GenerateCan(0);
        }

        if (GUILayout.Button("Generate Can in Lane 1"))
        {
            canGenerator.GenerateCan(1);
        }

        if (GUILayout.Button("Generate Can in Lane 2"))
        {
            canGenerator.GenerateCan(2);
        }
    }
}