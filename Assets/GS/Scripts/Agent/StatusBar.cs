using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusBar : MonoBehaviour
{
    public Slider energyBar;
    public Slider happinessBar;
    public Text energyText;
    public Text happinessText;

    private AgentIdentifier agent;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponentInParent<AgentIdentifier>();
        if (agent != null)
        {
            agent.OnStatusChanged += UpdateBars;
            UpdateBars(agent.energy, agent.happiness);
        }
    }

    void UpdateBars(int energy, int happiness)
    {
        if (energyBar != null)
            energyBar.value = energy / 100f;
        if (happinessBar != null)
            happinessBar.value = happiness / 100f;
        if (energyText != null)
            energyText.text = $"{energy}%";
        if (happinessText != null)
            happinessText.text = $"{happiness}%";
    }

    private void OnDestroy()
    {
        if (agent != null)
        {
            agent.OnStatusChanged -= UpdateBars;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
