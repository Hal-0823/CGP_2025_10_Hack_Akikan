using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LaneController : MonoBehaviour
{
    [SerializeField, Header("UVスクロールのスピードを調整する係数")]
    private float uvScrollMultiplier = 0.1f;

    [SerializeField]
    private float scrollSpeed = 1.0f;
    public float ScrollSpeed => scrollSpeed;

    private MeshRenderer laneRenderer;

    public List<GameObject> liftedObjects = new List<GameObject>();

    private void Awake()
    {
        laneRenderer = GetComponent<MeshRenderer>();
        laneRenderer.material.SetFloat("_ScrollSpeed", scrollSpeed * uvScrollMultiplier);
    }

    private void Update()
    {
        foreach (var obj in liftedObjects)
        {
            if (obj != null)
            {
                obj.transform.position += -Vector3.forward * scrollSpeed * Time.deltaTime;
            }
        }
    }

    public void ChangeScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
        laneRenderer.material.SetFloat("_ScrollSpeed", scrollSpeed * uvScrollMultiplier);
    }

    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("LiftableObject"))
        {
            liftedObjects.Add(collision.gameObject);
        }
    }
    
    public void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("LiftableObject"))
        {
            liftedObjects.Remove(collision.gameObject);
        }
    }
}
