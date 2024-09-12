using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Script for handling the lights in the environment (blackouts).
/// </summary>
namespace Lights
{
    public class InfiniteEnumerator : IEnumerator<int>
    {
        private readonly int _initialValue = 0;
        private int _currentValue = 0;

        public InfiniteEnumerator() { }

        public InfiniteEnumerator(int initialValue)
        {
            _initialValue = initialValue;
            _currentValue = 0;
        }

        public bool MoveNext()
        {
            _currentValue += _initialValue;
            return true;
        }

        public void Reset()
        {
            _currentValue = 0;
        }

        public int Current
        {
            get { return _currentValue; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        void IDisposable.Dispose() { }
    }
    public class LightsSwitch
    {
        private readonly int _episodeLength = 0;
        private bool _lightStatus = true;
        private readonly List<int> _blackouts = new List<int>();
        private readonly IEnumerator<int> _blackoutsEnum;
        private int _nextFrameSwitch = -1;

        public LightsSwitch()
        {
            _blackoutsEnum = _blackouts.GetEnumerator();
        }

        public LightsSwitch(int episodeLength, List<int> blackouts)
        {
            try
            {
                if (episodeLength < 0)
                {
                    throw new ArgumentException("Episode length (timeLimit) cannot be negative.");
                }

                _episodeLength = episodeLength;
                _blackouts = blackouts ?? throw new ArgumentNullException(nameof(blackouts));

                if (_blackouts.Count > 1)
                {
                    for (int i = 1; i < _blackouts.Count; i++)
                    {
                        if (_blackouts[i] <= _blackouts[i - 1])
                        {
                            throw new ArgumentException("Invalid blackout sequence: values must be in strictly increasing order.");
                        }
                    }
                }

                if (_blackouts.Count > 0 && _blackouts[0] < 0)
                {
                    _blackoutsEnum = new InfiniteEnumerator(-_blackouts[0]);
                }
                else
                {
                    _blackoutsEnum = _blackouts.GetEnumerator();
                }
                Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initialising lightSwitch: {ex.Message}");
                throw;
            }
        }

        public void Reset()
        {
            try
            {
                _lightStatus = true;
                _blackoutsEnum.Reset();
                if (_blackoutsEnum.MoveNext())
                {
                    _nextFrameSwitch = _blackoutsEnum.Current;
                }
                else
                {
                    _nextFrameSwitch = -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting LightsSwitch (blackouts): {ex.Message}");
                throw;
            }
        }

        public bool LightStatus(int step, int agentDecisionInterval)
        {
            try
            {
                if (step == _nextFrameSwitch * agentDecisionInterval)
                {
                    _lightStatus = !_lightStatus;
                    if (_blackoutsEnum.MoveNext())
                    {
                        _nextFrameSwitch = _blackoutsEnum.Current;
                    }
                }
                return _lightStatus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LightStatus (blackouts): {ex.Message}");
                return _lightStatus;
            }
        }
    }
}
