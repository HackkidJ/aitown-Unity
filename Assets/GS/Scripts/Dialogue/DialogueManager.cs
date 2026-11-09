using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager _instance;
    public static DialogueManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DialogueManager>();
            }
            return _instance;
        }
    }

    [Header("UI 组件")]
    public GameObject dialoguePanel;
    public Text dialogueText;
    public Button nextButton;

    [Header("设置")]
    public float textSpeed = 0.05f;
    private Queue<string> dialogueQueue = new Queue<string>();
    private bool isTyping = false;
    private string currentLine = "";

    // Start is called before the first frame update
    void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    public void ShowDialogue(string speaker, string listener,
                             List<string> speakerLines, List<string> listenerLines)
    {
        // 获取说话者和听者的 AgentIdentifier
        AgentIdentifier speakerAgent = GetAgentByName(speaker);
        AgentIdentifier listenerAgent = GetAgentByName(listener);

        // 协程显示对话
        StartCoroutine(DisplayDialogueWithBubbles(speakerAgent, listenerAgent, speakerLines, listenerLines));
    }

    IEnumerator DisplayDialogueWithBubbles(AgentIdentifier speaker, AgentIdentifier listener,
                                            List<string> speakerLines, List<string> listenerLines)
    {
        int maxLength = Mathf.Max(speakerLines.Count, listenerLines.Count);

        for (int i = 0; i < maxLength; i++)
        {
            if (i < speakerLines.Count && speaker != null)
            {
                speaker.Say(speakerLines[i], 3f);
                Debug.Log($"??? {speaker.agentName}: {speakerLines[i]}");
                yield return new WaitForSeconds(2.5f);
            }

            if (i < listenerLines.Count && listener != null)
            {
                listener.Say(listenerLines[i], 3f);
                Debug.Log($"??? {listener.agentName}: {listenerLines[i]}");
                yield return new WaitForSeconds(2.5f);
            }
        }
    }

    private AgentIdentifier GetAgentByName(string name)
    {
        AgentIdentifier[] allAgents = FindObjectsOfType<AgentIdentifier>();
        foreach (var agent in allAgents)
        {
            if (agent.agentName == name)
                return agent;
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
