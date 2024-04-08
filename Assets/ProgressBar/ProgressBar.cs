using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// A progress bar that is used to display the health of an agent. The progress bar has a title, a bar, and a pass marker.
/// </summary>
[ExecuteInEditMode]
public class ProgressBar : MonoBehaviour
{
    [Header("Title Text Setting")]
    public string Title;
    public Color TitleColor;
    public TMP_FontAsset TitleFont;
    public int TitleFontSize = 10;

    [Header("Bar Setting")]
    public Color BarFullColor;
    public Color BarNormalColor;
    public Color BarEmptyColor;
    public Color BarBackGroundColor;
    public Sprite BarBackGroundSprite;
    public int MaxHealth = 100;
    public int MinHealth = 0;
    public int StartHealth = 0;
    public int PassMark = 0;

    [Header("Pass Mark Settings")]
    private float _pass_mark_proportion;
    private float _min_pass_color_pivot;
    private float _pass_max_color_pivot;

    private Image _bar,
        _barBackground;
    private RectTransform _passMarker;
    private TMP_Text _txtTitle;
    private TrainingAgent _agent;

    private int _barSize;
    private float _barValue;
    public float BarValue
    {
        get { return _barValue; }
        set
        {
            value = Mathf.Clamp(value, MinHealth, MaxHealth);
            _barValue = value;
            UpdateValue(_barValue);
        }
    }

    private void Awake()
    {
        _bar = transform.Find("Bar").GetComponent<Image>();
        _txtTitle = transform.Find("Text").GetComponent<TMP_Text>();
        _barBackground = transform.Find("BarBackground").GetComponent<Image>();
        _passMarker = transform.Find("PassMarker").GetComponent<RectTransform>();

        InitializeProgressBar();
    }

    private void InitializeProgressBar()
    {
        _txtTitle.text = Title;
        _txtTitle.color = TitleColor;
        _txtTitle.font = TitleFont;
        _txtTitle.fontSize = TitleFontSize;
        _barBackground.color = BarBackGroundColor;
        _barBackground.sprite = BarBackGroundSprite;

        _barSize = MaxHealth - MinHealth;
        StartHealth = Clamp(StartHealth);
        PassMark = Clamp(PassMark);
        _pass_mark_proportion = HealthProportion(PassMark);
        _min_pass_color_pivot = _pass_mark_proportion / 2;
        _pass_max_color_pivot = (_pass_mark_proportion + 1) / 2;

        _passMarker.anchoredPosition = new Vector2(77.5f * 2 * (_pass_mark_proportion - 0.5f), 0);
        _bar.fillAmount = HealthProportion(StartHealth);

        UpdateColor(_bar.fillAmount);
        UpdateValue(StartHealth);
    }

    private void FixedUpdate()
    {
        if (!Application.isPlaying)
        {
            UpdateValue(50);
            _txtTitle.color = TitleColor;
            _txtTitle.font = TitleFont;
            _txtTitle.fontSize = TitleFontSize;

            _bar.color = BarNormalColor;
            _barBackground.color = BarBackGroundColor;
            _barBackground.sprite = BarBackGroundSprite;
        }

        if (_agent != null)
        {
            BarValue = _agent.health;
        }
        else
        {
            Debug.Log("Agent properties null or not assigned! Cannot update progress bar!");
        }
    }

    private float HealthProportion(float x)
    {
        return ((x - MinHealth) / _barSize);
    }

    private int Clamp(int h)
    {
        return Mathf.Clamp(h, MinHealth, MaxHealth);
    }

    private float Clamp(float h)
    {
        return Mathf.Clamp(h, MinHealth, MaxHealth);
    }

    void UpdateValue(float val)
    {
        if (val != Mathf.Clamp(val, MinHealth, MaxHealth))
        {
            Debug.Log("Progress bar value out of bounds! Clamping...");
            val = Mathf.Clamp(val, MinHealth, MaxHealth);
        }
        float health_proportion = HealthProportion(val);
        _bar.fillAmount = health_proportion;
        _txtTitle.text = Title + ": " + val.ToString("F1");

        UpdateColor(_bar.fillAmount);
    }

    private void UpdateColor(float fill)
    {
        if (fill != Mathf.Clamp(fill, 0, 1))
        {
            Debug.Log("UpdateColor() in ProgressBar.cs passed a bad fill proportion! Clamping...");
            fill = Mathf.Clamp(fill, 0, 1);
        }
        if (fill < _pass_mark_proportion)
        {
            fill = (fill / _pass_mark_proportion);
            _bar.color =
                fill * BarNormalColor
                + (1 - fill) * BarEmptyColor
                + (0.5f - Mathf.Abs(fill - 0.5f)) * Color.white;
        }
        else
        {
            fill = ((fill - _pass_mark_proportion) / (1 - _pass_mark_proportion));
            _bar.color =
                fill * BarFullColor
                + (1 - fill) * BarNormalColor
                + (0.5f - Mathf.Abs(fill - 0.5f)) * Color.white * 0.5f;
        }
    }

    public void AssignAgent(TrainingAgent training_agent)
    {
        if (training_agent == null)
        {
            Debug.Log("Agent is null or no agent found for assignment!");
            return;
        }
        else
        {
            _agent = training_agent;
        }
    }
}
