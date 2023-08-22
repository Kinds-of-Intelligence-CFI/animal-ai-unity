using Unity.MLAgents;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{
	private Camera _cameraAbove;
	private Camera _cameraAgent;
	private Camera _cameraFollow;
	private ScreenshotCamera _screenshotCam; // Don't include in _cameras
	private TrainingAgent _agent;
	public Text score; // Assigned to 'Score Text' in editor inspector.
	private int _numActive = 0;
	private Dictionary<int, Camera> _cameras;
	public float prevScore = 0;
	public Canvas effectCanvas;
	public int cameraID
	{
		get { return _numActive; }
	}

	void Start()
	{
		_agent = GameObject.FindGameObjectWithTag("agent").GetComponent<TrainingAgent>();
		GameObject ssCamGO = GameObject.FindGameObjectWithTag("ScreenshotCam");
		_screenshotCam = GameObject
			.FindGameObjectWithTag("ScreenshotCam")
			.GetComponent<ScreenshotCamera>();

		_cameraAbove = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
		_cameraAgent = _agent.transform.Find("AgentCamMid").GetComponent<Camera>();
		_cameraFollow = GameObject.FindGameObjectWithTag("camBase").GetComponent<Camera>();

		_cameraAbove.enabled = true;
		_cameraAgent.enabled = false;
		_cameraFollow.enabled = false;

		_cameras = new Dictionary<int, Camera>();
		_cameras.Add(0, _cameraAbove);
		_cameras.Add(1, _cameraAgent);
		_cameras.Add(2, _cameraFollow);
		_numActive = 0;
		effectCanvas.renderMode = RenderMode.ScreenSpaceCamera;
		effectCanvas.worldCamera = getActiveCam();
		effectCanvas.planeDistance = choosePlaneDistance(); // Has to be within clip volume of all cameras (3rd person one is set to 0.3)
	}

	void Update()
	{
		bool cDown = Input.GetKeyDown(KeyCode.C);
		if (cDown)
		{
			UpdateCam();
		}
		if (Input.GetKeyDown(KeyCode.R))
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

		score.text =
			"Prev reward: "
			+ _agent.GetPreviousScore().ToString("0.000")
			+ "\n"
			+ "Reward: "
			+ _agent.GetCumulativeReward().ToString("0.000");
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
