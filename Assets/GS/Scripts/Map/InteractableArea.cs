using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableArea : MonoBehaviour
{
    public string areaName;
    public string description;
    public bool canEnter = true;
    public string[] allowedActions;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            Debug.Log($"智能体进入区域：{areaName}");

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            Debug.Log($"智能体离开区域:{areaName}");
        }
    }
    // Start is called before the first frame update
}
