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

	public int cameraID
	{
		get { return _numActive; }
	}

	private bool _canResetEpisode = true;
	private bool _canChangePerspective = true;
	private ArenasConfigurations arenasConfigurations;

	void Start()
	{
		_agent = GameObject.FindGameObjectWithTag("agent").GetComponent<TrainingAgent>();
		_screenshotCam = GameObject.FindGameObjectWithTag("ScreenshotCam").GetComponent<ScreenshotCamera>();
		_cameraFollow = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();
		_cameraAbove = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		_cameraAgent = _agent.transform.Find("AgentCamMid").GetComponent<Camera>();

		if (_cameraAgent != null)
		{
			Debug.Log("Camera Agent fetched successfully.");
		}
		else
		{
			Debug.Log("Failed to fetch Camera Agent.");
			return;
		}

		_cameraAbove.enabled = true;
		_cameraAgent.enabled = false;
		_cameraFollow.enabled = false;

		_cameras = new Dictionary<int, Camera>();
		_cameras.Add(0, _cameraAbove);
		_cameras.Add(1, _cameraAgent);
		_cameras.Add(2, _cameraFollow);

		effectCanvas.renderMode = RenderMode.ScreenSpaceCamera;
		effectCanvas.worldCamera = getActiveCam();
		effectCanvas.planeDistance = choosePlaneDistance();

		arenasConfigurations = ArenasConfigurations.Instance;

		ArenaConfiguration currentConfig = arenasConfigurations.CurrentArenaConfiguration;
		if (currentConfig == null)
		{
			Debug.LogError("Current Arena Configuration is null.");
			return;
		}

		_canResetEpisode = currentConfig.canResetEpisode;
		_canChangePerspective = currentConfig.canChangePerspective;

		if (!_canChangePerspective)
		{
			_cameraAbove.enabled = true;
			_cameraAgent.enabled = true;
			_cameraFollow.enabled = true;
			_numActive = 2;

			effectCanvas.worldCamera = getActiveCam();
			effectCanvas.planeDistance = choosePlaneDistance();
		}
	}

	void Update()
	{
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
