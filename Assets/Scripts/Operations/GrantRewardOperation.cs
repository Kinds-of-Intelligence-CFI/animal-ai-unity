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


        public override void execute()
        {
            TrainingAgent agent = FindAnyObjectByType<TrainingAgent>();
            if (agent != null)
            {
                agent.RecordRewardType(rewardType);
                agent.UpdateHealth(reward);
            }
        }
    }
}
