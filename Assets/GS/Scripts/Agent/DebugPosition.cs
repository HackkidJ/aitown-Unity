using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPosition : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"{name}  µº Œª÷√: ({transform.position.x}, {transform.position.y}, {transform.position.z})");
        }
    }
}