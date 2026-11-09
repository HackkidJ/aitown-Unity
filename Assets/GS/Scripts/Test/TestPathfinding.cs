using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPathfinding : MonoBehaviour
{
    public Vector3 targetPosition = new Vector3 (-28, -20, 0);
    public bool testOnStart = true;
    public float testDelay = 2f;
    private AgentController agentController;
    // Start is called before the first frame update
    void Start()
    {
        agentController = GetComponent<AgentController>();

        if (testOnStart)
        {
            Invoke("StartTest", testDelay);
        }
    }

    void StartTest()
    {
        agentController.MoveToWithPathfinding(targetPosition, "walk");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(targetPosition, Vector3.one);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
