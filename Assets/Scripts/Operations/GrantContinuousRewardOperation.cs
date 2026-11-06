using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Grant a reward every fixed update to each agent in the zone (a DataZone or an agent's zone), from
    /// when it enters until it leaves (or its episode ends)
    /// </summary>
    public class GrantContinuousReward : Operation
    {
        public float rewardPerStep;
        /* One reward per agent in the zone: several agents can be inside at once */
        private readonly Dictionary<TrainingAgent, Coroutine> rewardCoroutines =
            new Dictionary<TrainingAgent, Coroutine>();

        private IEnumerator GrantRewardEveryStep(TrainingAgent agent)
        {
            while (true)
            {
                agent.UpdateHealth(rewardPerStep);
                yield return new WaitForFixedUpdate();
            }
        }

        public override void execute(TrainingAgent triggeringAgent) {
            if (attachedObjectDetails.obj is not TriggerVolume)
            {
                Debug.LogError("GrantContinuousReward can only be used with a zone (a DataZone or an agent's zone)");
                return;
            }

            if (triggeringAgent == null)
            {
                Debug.LogError("GrantContinuousReward was executed without a triggering agent");
                return;
            }

            if (rewardCoroutines.ContainsKey(triggeringAgent))
            {
                Debug.LogError($"Attempted to start GrantContinuousReward for agent {triggeringAgent.agentIndex} while it is already in progress");
                return;
            }

            triggeringAgent.EpisodeEnded += StopReward;
            rewardCoroutines[triggeringAgent] = attachedObjectDetails.obj.StartCoroutine(GrantRewardEveryStep(triggeringAgent));
        }

        public override void completeExecution(TrainingAgent triggeringAgent) {
            if (triggeringAgent == null || !rewardCoroutines.ContainsKey(triggeringAgent))
            {
                Debug.LogError("Attempted to stop GrantContinuousReward while it is not in progress");
                return;
            }

            StopReward(triggeringAgent);
        }

        private void StopReward(TrainingAgent agent)
        {
            if (rewardCoroutines.TryGetValue(agent, out Coroutine coroutine))
            {
                attachedObjectDetails.obj.StopCoroutine(coroutine);
                rewardCoroutines.Remove(agent);
            }
            agent.EpisodeEnded -= StopReward;
        }
    }
}
