using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObjectProperties : MonoBehaviour
{
    public string objectType;
    public bool isDestructible;
    public bool isInteractive;
    [TextArea]
    public string description;

    public Dictionary<string, object> GetProperties() {
        return new Dictionary<string, object>
        {
            {"type", objectType},
            {"isDestructible", isDestructible},
            {"isInteractive", isInteractive},
            {"description", description}
        };
    }
}
