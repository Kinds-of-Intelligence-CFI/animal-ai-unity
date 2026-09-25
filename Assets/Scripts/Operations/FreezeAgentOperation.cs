using System.Collections;
using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Freeze an agent for a set period of time
    /// </summary>
    public class FreezeAgent : Operation
    {
        public int freezeSteps;

        public override void execute(TrainingAgent triggeringAgent)
        {
            Debug.Log("Freezing agent for " + freezeSteps + " FixedUpdate steps");
            if (triggeringAgent == null)
            {
                Debug.LogError("FreezeAgent was executed without a triggering agent");
                return;
            }
            triggeringAgent.StartCoroutine(FreezeCoroutine(triggeringAgent));
        }

        private IEnumerator FreezeCoroutine(TrainingAgent agent)
        {
            agent.FreezeAgent(true, updateHealthFreeze: false);

            for (int i = 0; i < freezeSteps; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            agent.FreezeAgent(false);
        }
    }
}
