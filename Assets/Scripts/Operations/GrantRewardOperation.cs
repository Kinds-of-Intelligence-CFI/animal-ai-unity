using UnityEngine;

namespace Operations
{
    /// <summary>
    /// Give the agent a reward
    /// </summary>
    public class GrantReward : Operation
    {
        public float reward = 1;
        private string rewardType = "End Episode Operation";


        public override void execute(TrainingAgent triggeringAgent)
        {
            if (triggeringAgent == null)
            {
                Debug.LogError("GrantReward was executed without a triggering agent");
                return;
            }
            triggeringAgent.RecordRewardType(rewardType);
            triggeringAgent.UpdateHealth(reward);
        }
    }
}
