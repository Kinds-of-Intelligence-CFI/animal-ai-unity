using System.Collections;
using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Freeze an agent for a set period of time
    /// </summary>
    public class FreezeAgent : Operation
    {
        public float freezeDuration;

        public override void execute()
        {
            Debug.Log("Freezing agent for " + freezeDuration + " seconds");
            TrainingAgent agent = FindFirstObjectByType<TrainingAgent>();
            agent.StartCoroutine(FreezeCoroutine(agent));
        }

        private IEnumerator FreezeCoroutine(TrainingAgent agent)
        {
            agent.FreezeAgent(true, updateHealthFreeze: false);
            yield return new WaitForSeconds(freezeDuration);
            agent.FreezeAgent(false);
        }
    }
}