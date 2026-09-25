# Animal-AI
![steampunkFOURcrop](https://github.com/Kinds-of-Intelligence-CFI/animal-ai/assets/65875290/df798f4a-cb2c-416f-a150-093b9382a621)
Animal-AI Unity Project

This repo is for the Unity project for Animal-AI in case you would like to make improvements to the environment. The main Animal-AI repo is at [https://github.com/Kinds-of-Intelligence-CFI/animal-ai](https://github.com/Kinds-of-Intelligence-CFI/animal-ai).

Developed originally using `Unity 2020.3.9f1` and `ML-Agents 2.1.0-exp.1`.

Currently using `Unity 6000.0.23f1` and `ML-Agents 3.0.0`.

## Multiple agents

An arena can contain several agents: add one `Agent` item per agent (the k-th `Agent` item is agent k).
Each item works exactly like the single `Agent` item always has: several `positions` are candidate spawn
points (one is chosen at random), and `rotations`, `skins` and `frozenAgentDelays` use their first entry.
The new `teams` field sets the agent's ML-Agents team (default 0).

```yaml
!ArenaConfig
arenas:
  0: !Arena
    timeLimit: 250
    episodeEnd: any        # optional: "any" (default) or "all"
    items:
    - !Item
      name: Agent
      positions:
      - !Vector3 {x: 10, y: 0, z: 20}
      rotations: [90]
    - !Item
      name: Agent
      positions:
      - !Vector3 {x: 30, y: 0, z: 20}
      rotations: [270]
      teams: [1]           # optional: this agent's ML-Agents team
```

- **Each agent has the same observations and actions as a single agent**, except that rays now label more
  objects, including other agents: there are 18 detectable tags (`agent` is the last), so each ray is 20
  floats instead of 14. This applies to single-agent configs too, so Python code that reads rays needs the
  matching `animalai` version, and policies trained on ray casts with older builds need retraining.
- From Python, all agents on the same team are agents of one behavior, `AnimalAI?team=N`, and appear
  together in that behavior's `DecisionSteps` (each has its own agent id). Without `teams` every agent is on
  team 0, so there is a single behavior `AnimalAI?team=0`, exactly as with one agent.
- **Use `teams` when agents should learn separately** — competing sides, or self-play, where each team trains
  as its own behavior. Agents that cooperate, or that share a policy, should stay on one team. An agent's team
  is fixed for the whole run: every arena that gives agent k a team must give it the same one.
- **The arena owns the episode.** With `episodeEnd: any`, the episode ends for every agent as soon as one
  agent finishes (runs out of health or completes the arena). With `episodeEnd: all`, an agent that
  finishes is frozen until every agent has finished. Either way the arena is rebuilt once and all agents
  start their next episode together. All agents are in one ML-Agents `SimpleMultiAgentGroup`.
- **Goals, DataZones and spawner buttons react to whichever agent touches them.** A DataZone runs its
  operations once for each agent that enters (and again when that agent leaves), so with two agents inside,
  they have run twice. A spawner button ignores presses while it's moving, whichever agent presses it.
- **Operations** either act on the agent that triggered them or on the arena, which every agent shares:

  | Operation | With several agents |
  |---|---|
  | `grantReward`, `freezeAgent` | Only the triggering agent |
  | `grantContinuousReward` | Each agent in the zone, from when it enters until it leaves |
  | `endEpisode` | The triggering agent finishes (with the reward); `episodeEnd` then decides whether the episode ends for everyone (`any`) or that agent waits frozen (`all`) |
  | `toggleObject` | The arena: one object, shared by every agent. On a DataZone, each agent that enters toggles it again |
  | `limitedInvocationsOperation` | One count per button or zone, shared by every agent: `maxInvocations: 3` means 3 in total, not 3 each |
  | `operationFromList` | Picks at random each time; the chosen operation behaves as above |
- **Agent 0 is the primary agent**: the screen cameras and health bar follow it.
- **Player mode supports one agent only.** An arena with several `Agent` items is rejected with an error in
  player mode, which includes the Unity editor; run with `playerMode` off and control the agents from Python.
- **Logs:** each agent writes its own `ObservationLogs/Observations_<time>_agent<k>.csv`.
- Arenas in one configuration may have different numbers of agents; unused agents are deactivated. Arenas
  joined by `mergeNextArena` must have the same number, since the episode carries on across them.

## Version History
- v5.0.0
  - Upgraded Unity Game Engine to `Unity 6000.0.23f1`.
  - Updated ML-Agents to `3.0.0` (transitioned to non-experimental version).
  - Refreshed project assets and reimported all assets for quality control.
- v4.0.0
  - Upgraded Unity Game Engine to `Unity 2021.3.21f1`.
  - Updated ML-Agents to `2.3.0-exp.1`.
  - Various minor and major bug fixes.
