# Animal-AI
![steampunkFOURcrop](https://github.com/Kinds-of-Intelligence-CFI/animal-ai/assets/65875290/df798f4a-cb2c-416f-a150-093b9382a621)
Animal-AI Unity Project

This repo is for the Unity project for Animal-AI in case you would like to make improvements to the environment. The main Animal-AI repo is at [https://github.com/Kinds-of-Intelligence-CFI/animal-ai](https://github.com/Kinds-of-Intelligence-CFI/animal-ai).

Developed originally using `Unity 2020.3.9f1` and `ML-Agents 2.1.0-exp.1`.

Currently using `Unity 6000.0.23f1` and `ML-Agents 3.0.0`.

## WebGL Build
WEBGL build has limitations and is INCOMPLETE:
- WebGL build is not fully functional.
- WebGL build is not fully tested.
- WebGL build is not fully integrated with the main branch.
- WebGL build is not fully optimized.
- WebGL build is not fully documented.
- Some features may not work as intended or even work at all due to the limitations of WebGL (such as serialisation).
- Some code was written specifically for WebGL build and may be breaking or not working as intended. This was done to quickly create a WebGL build for demonstration purposes and is not recommended for production (i.e. AAI3EnvironmentManager.cs and ArenaParameters.cs were modified for WebGL build but hastely and probably not the best approach).

## TODO:
- [x] Create WebGL build.
- [ ] Implement logic to output console logs for WEBGL UNITY builds for the experiments.
- [x] Add WebGL build ref to animal-ai repo.
- [ ] Implement testing for WebGL build.
- [ ] Merge WebGL build with main branch or create a new repo for WebGL build.
- [ ] Optimize WebGL build (if possible).
- [ ] Document WebGL build (mainly limitations and known issues).
- [ ] Cleanup code in feature-webgl branch written specifically for WebGL build.
- [ ] Check if all or most features are working as intended in WebGL build.
- [ ] Go over all code TODOs in feature-webgl branch and address them before merging with main branch.

## Version History
- WebGL v1.0.0
  - Initial release of WebGL version.
- v5.0.0
  - Upgraded Unity Game Engine to `Unity 6000.0.23f1`.
  - Updated ML-Agents to `3.0.0` (transitioned to non-experimental version).
  - Refreshed project assets and reimported all assets for quality control.
- v4.0.0
  - Upgraded Unity Game Engine to `Unity 2021.3.21f1`.
  - Updated ML-Agents to `2.3.0-exp.1`.
  - Various minor and major bug fixes.
