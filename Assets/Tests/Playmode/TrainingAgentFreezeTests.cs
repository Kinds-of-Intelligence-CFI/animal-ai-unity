using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

/// <summary>
/// Playmode tests for TrainingAgent's freeze/unfreeze countdown.
/// </summary>
public class TrainingAgentFreezeTests
{
    [SetUp]
    public void Setup()
    {
        SceneManager.LoadScene("AAI3EnvironmentManager", LoadSceneMode.Single);
    }

    [UnityTest]
    public IEnumerator OnEpisodeBegin_StillUnfreezesAfterAnEarlierCountdownWasInterrupted()
    {
        yield return null;

        TrainingAgent agent = GameObject.FindAnyObjectByType<TrainingAgent>();
        Assert.IsNotNull(agent, "TrainingAgent should be found in the scene");

        float freezeDelay = 0.2f;

        // Episode 1: a freeze countdown starts, but the episode ends early (e.g. via an
        // EndEpisode operation) before the countdown completes. ML-Agents then calls
        // OnEpisodeBegin() for the next episode, which always calls StopAllCoroutines() -
        // killing the countdown mid-flight. Calling OnEpisodeBegin() directly (rather than just
        // StopAllCoroutines()) is what actually exercises the fix.
        agent.SetFreezeDelay(freezeDelay);
        agent.OnEpisodeBegin();

        // Episode 2: spawning applies a fresh freeze delay, exactly as ArenaBuilders.SpawnAgent
        // does on every arena reset.
        agent.SetFreezeDelay(freezeDelay);

        yield return new WaitForSeconds(freezeDelay + 0.3f);

        float healthBefore = agent.health;
        agent.UpdateHealth(-0.01f);

        float expectedHealth = healthBefore - 1f;
        Assert.AreEqual(expectedHealth, agent.health, 0.001f,
            "Agent should have unfrozen once episode 2's freeze delay elapsed. If it's still " +
            "frozen, the countdown interrupted in episode 1 has permanently blocked future " +
            "countdowns from starting.");
    }
}
