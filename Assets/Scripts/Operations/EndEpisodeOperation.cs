using UnityEngine;

namespace Operations
{
    /// <summary>
    /// End the episode with a reward
    /// </summary>
    public class EndEpisode : Operation
    {
        public float reward = 0;
        private string rewardType = "End Episode Operation";


        public override void execute(TrainingAgent triggeringAgent)
        {
            if (triggeringAgent == null)
            {
                Debug.LogError("EndEpisode was executed without a triggering agent");
                return;
            }
            triggeringAgent.RecordRewardType(rewardType);
            triggeringAgent.UpdateHealth(reward, true);
        }
    }
}
