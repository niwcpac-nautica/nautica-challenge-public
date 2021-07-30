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
		if (!agent) agent = FindObjectOfType<AbstractNauticaAgent>();
		// since agent is spawned by TrainingManger, it gets set there
		// but just in case it's not set by the time we start running, try setting here
    }

    void Update()
    {
		if (!agent) return;
		obsText.text = agent.GetCurrentObservationsText();
		actionText.text = agent.GetCurrentActionsText();
		rewardText.text = agent.GetCurrentRewardsText();
    }
}
