using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Nautica;


public class DebugDisplayer : MonoBehaviour
{
	public AbstractNauticaAgent agent;
	public Text obsText;
	public Text actionText;
	public Text rewardText;
	private const string LOGTAG = nameof(DebugDisplayer);


    void Start()
    {
		if (!agent)
		{
			agent = FindObjectOfType<AbstractNauticaAgent>();
			Debug.unityLogger.Log(LOGTAG, "Warning: agent not assigned, grabbing any available agent in scene...");
		}
		Debug.AssertFormat(agent, "Could not find any agent in scene!");
    }

    void Update()
    {
		if (!agent) return;
		obsText.text = agent.GetCurrentObservationsText();
		actionText.text = agent.GetCurrentActionsText();
		rewardText.text = agent.GetCurrentRewardsText();
    }
}
