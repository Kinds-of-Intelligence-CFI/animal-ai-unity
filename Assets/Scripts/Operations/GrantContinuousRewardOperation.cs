using System.Collections;
using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Grant a reward every fixed update
    /// </summary>
    public class GrantContinuousReward : Operation
    {
        public float rewardPerStep;
        private Coroutine grantRewardCoroutine;

        private IEnumerator GrantRewardEveryStep()
        {
            while (true)
            {
                TrainingAgent agent = FindFirstObjectByType<TrainingAgent>();
                agent.UpdateHealth(rewardPerStep);               
                yield return new WaitForFixedUpdate();
            }
        }

        public override void execute() {
            Debug.Log(rewardPerStep);
            if (attachedObjectDetails.obj is not DataZone)
            {
                Debug.LogError("GrantContinuousReward can only be used with a Datazone");
                return;
            }

            if (grantRewardCoroutine != null)
            {
                Debug.LogError("Attempted to start GrantContinuousReward while it is already in progress");
                return;
            }

            TrainingAgent.OnEpisodeEnd.AddListener(StopReward);
            grantRewardCoroutine = attachedObjectDetails.obj.StartCoroutine(GrantRewardEveryStep());
        }

        private void StopReward()
        {
            if (grantRewardCoroutine != null)
            {
                attachedObjectDetails.obj.StopCoroutine(grantRewardCoroutine);
                grantRewardCoroutine = null;
            }
            TrainingAgent.OnEpisodeEnd.RemoveListener(StopReward);
        }

        public override void completeExecution() {
            if (grantRewardCoroutine == null)
            {
                Debug.LogError("Attempted to stop GrantContinuousReward while it is not in progress");
                return;
            }

            attachedObjectDetails.obj.StopCoroutine(grantRewardCoroutine);
            grantRewardCoroutine = null;
            TrainingAgent.OnEpisodeEnd.RemoveListener(StopReward);
        }
    }
}