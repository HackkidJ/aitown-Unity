using UnityEngine;
using UnityEngine.UI;

public class DialogueTestButton : MonoBehaviour
{
    public Button testButton;
    public string agent1Name = "小千";
    public string agent2Name = "山姆";

    void Start()
    {
        if (testButton == null)
            testButton = GetComponent<Button>();

        if (testButton != null)
        {
            testButton.onClick.AddListener(OnTestButtonClick);
        }
    }

    void OnTestButtonClick()
    {
        // 获取两个智能体的 ID
        var agents = FindObjectsOfType<AgentIdentifier>();
        string agent1Id = null;
        string agent2Id = null;

        foreach (var agent in agents)
        {
            if (agent.agentName == agent1Name)
                agent1Id = agent.agentId;
            if (agent.agentName == agent2Name)
                agent2Id = agent.agentId;
        }

        if (agent1Id != null && agent2Id != null)
        {
            var wsClient = FindObjectOfType<AgentWebSocketClient>();
            if (wsClient != null)
            {
                wsClient.TestDialogue(agent1Id, agent2Id);
            }
        }
        else
        {
            Debug.LogError($"找不到智能体: {agent1Name} 或 {agent2Name}");
        }
    }
}