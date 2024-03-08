using Unity.MLAgents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ArenasParameters;

public class PlayerControls : MonoBehaviour
{
	private Camera _cameraAbove;
	private Camera _cameraAgent;
	private Camera _cameraFollow;
	private ScreenshotCamera _screenshotCam;
	private TrainingAgent _agent;
	public Text score;
	private int _numActive = 0;
	private Dictionary<int, Camera> _cameras;
	public float prevScore = 0;
	public Canvas effectCanvas;
	private bool camerasFetched = false;

	public int cameraID
	{
		get { return _numActive; }
	}

	private bool _canResetEpisode = true;
	private bool _canChangePerspective = true;
	private ArenasConfigurations arenasConfigurations;
	public UIManager uiManager;


	void Start()
	{
		effectCanvas.renderMode = RenderMode.ScreenSpaceCamera;
		arenasConfigurations = ArenasConfigurations.Instance;

		_screenshotCam = GameObject.FindGameObjectWithTag("ScreenshotCam").GetComponent<ScreenshotCamera>();
		_cameraFollow = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();
		_cameraAbove = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

		ArenaConfiguration currentConfig = arenasConfigurations.CurrentArenaConfiguration;
		if (currentConfig == null)
		{
			Debug.LogError("Current Arena Configuration is null.");
			return;
		}

		_canResetEpisode = arenasConfigurations.canResetEpisode;
		_canChangePerspective = arenasConfigurations.canChangePerspective;


		// Camera Component Checks. Important check in general.
		if (_screenshotCam == null)
		{
			Debug.LogError("Screenshot camera not found!");
		}
		if (_cameraFollow == null)
		{
			Debug.LogError("Camera with 'camBase' tag not found!");
		}
		if (_cameraAbove == null)
		{
			Debug.LogError("Main camera not found!");
		}
	}

	void Update()
	{
		if (!camerasFetched)
		{
			_agent = GameObject.FindGameObjectWithTag("agent").GetComponent<TrainingAgent>();

			// Agent Component Check. Also important.
			if (_agent == null)
			{
				Debug.LogError("Agent with 'agent' tag not found!");
				return;
			}

			_cameraAgent = _agent.transform.Find("AgentCamMid").GetComponent<Camera>();

			// Camera Agent Check
			if (_cameraAgent == null)
			{
				Debug.LogError("Camera 'AgentCamMid' not found in agent!");
				return;
			}

			camerasFetched = true;

			_cameraAbove.enabled = true;
			_cameraAgent.enabled = false;
			_cameraFollow.enabled = false;

			_cameras = new Dictionary<int, Camera>();
			_cameras.Add(0, _cameraAbove);
			_cameras.Add(1, _cameraAgent);
			_cameras.Add(2, _cameraFollow);

			effectCanvas.worldCamera = getActiveCam();
			effectCanvas.planeDistance = choosePlaneDistance();

			if (!_canChangePerspective)
			{
				_cameraAbove.enabled = false;
				_cameraAgent.enabled = true;
				_cameraFollow.enabled = false;
				_numActive = 1;

				effectCanvas.worldCamera = getActiveCam();
				effectCanvas.planeDistance = choosePlaneDistance();
			}

			if (_cameras == null || _cameras.Count != 3)
			{
				Debug.LogError("Cameras dictionary not initialized properly!");
				return;
			}
		}

		if (_canChangePerspective && Input.GetKeyDown(KeyCode.C))
		{
			UpdateCam();
		}
		if (_canResetEpisode && Input.GetKeyDown(KeyCode.R))
		{
			_agent.EndEpisode();
		}
		if (Input.GetKeyDown(KeyCode.Q))
		{
			Application.Quit();
		}
		if (Input.GetKeyDown(KeyCode.F9))
		{
			_screenshotCam.Activate();
		}
		if (Input.GetKeyDown(KeyCode.B))
		{
			if (uiManager != null)
			{
				uiManager.ToggleDropdown();
			}
		}

		score.text = "Prev reward: " + _agent.GetPreviousScore().ToString("0.000")
					 + "\n" + "Reward: " + _agent.GetCumulativeReward().ToString("0.000");
	}

	public Camera getActiveCam()
	{
		return _cameras[_numActive];
	}

	private float choosePlaneDistance()
	{
		return _numActive == 1 ? 0.02f : 0.31f;
	}

	void UpdateCam()
	{
		_cameras[_numActive].enabled = false;
		_numActive = (_numActive + 1) % 3;
		_cameras[_numActive].enabled = true;

		effectCanvas.worldCamera = getActiveCam();
		effectCanvas.planeDistance = choosePlaneDistance();
	}
}
