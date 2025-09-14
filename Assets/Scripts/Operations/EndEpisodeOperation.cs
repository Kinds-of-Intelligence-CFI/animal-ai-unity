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


        public override void execute()
        {
            TrainingAgent agent = FindAnyObjectByType<TrainingAgent>();
            if (agent != null)
            {
                agent.RecordRewardType(rewardType);
                agent.UpdateHealth(reward, true);
            }
        }
    }
}
