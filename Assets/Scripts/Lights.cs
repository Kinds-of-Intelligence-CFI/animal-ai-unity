using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Facilitates the management and retrieval of Fade components associated with a collection of GameObjects, black screens.
/// </summary>
namespace Lights
{
    public class InfiniteEnumerator : IEnumerator<int>
    {
        private int _initialValue;
        private int _currentValue;

        public InfiniteEnumerator(int initialValue = 0)
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

        public int Current => _currentValue;

        object IEnumerator.Current => Current;

        public void Dispose() { }
    }

    public class LightsSwitch
    {
        private int _episodeLength;
        private bool _lightStatus = true;
        private List<int> _blackouts;
        private IEnumerator<int> _blackoutsEnum;
        private int _nextFrameSwitch = -1;

        public LightsSwitch(int episodeLength = 0, List<int> blackouts = null)
        {
            if (episodeLength < 0)
                throw new ArgumentException("Episode length cannot be negative.");

            _episodeLength = episodeLength;
            _blackouts = blackouts ?? new List<int>();

            if (_blackouts.Count > 0)
            {
                foreach (var blackout in _blackouts)
                {
                    if (blackout < 0 || blackout >= _episodeLength)
                        throw new ArgumentException("Blackout interval is invalid.");
                }

                _blackouts.Sort();

                _blackoutsEnum =
                    _blackouts[0] < 0
                        ? new InfiniteEnumerator(-_blackouts[0])
                        : _blackouts.GetEnumerator();
            }
            else
            {
                _blackoutsEnum = _blackouts.GetEnumerator();
            }

            Reset();
        }

        public void Reset()
        {
            _lightStatus = true;
            _blackoutsEnum.Reset();
            _nextFrameSwitch = _blackoutsEnum.MoveNext() ? _blackoutsEnum.Current : -1;
        }

        public bool LightStatus(int step, int agentDecisionInterval)
        {
            if (step < 0 || agentDecisionInterval <= 0)
                throw new ArgumentException("Step and agent decision interval must be positive.");

            if (step == _nextFrameSwitch * agentDecisionInterval)
            {
                _lightStatus = !_lightStatus;
                _nextFrameSwitch = _blackoutsEnum.MoveNext() ? _blackoutsEnum.Current : -1;
            }
            return _lightStatus;
        }
    }
}
