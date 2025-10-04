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

        public override void execute()
        {
            Debug.Log("Freezing agent for " + freezeSteps + " FixedUpdate steps");
            TrainingAgent agent = FindFirstObjectByType<TrainingAgent>();
            agent.StartCoroutine(FreezeCoroutine(agent));
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