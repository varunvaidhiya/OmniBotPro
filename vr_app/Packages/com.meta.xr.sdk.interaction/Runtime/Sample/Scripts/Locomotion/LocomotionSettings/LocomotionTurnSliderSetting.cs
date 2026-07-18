/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Interaction.Locomotion
{
    [System.Obsolete("Use " + nameof(TurningSetting) + " instead")]
    public class LocomotionTurnSliderSetting : MonoBehaviour
    {
        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private Toggle _snapTurnToggle;

        [SerializeField]
        private Toggle _smoothTurnToggle;

        [SerializeField]
        private float[] _snapTurnSteps = new[] { 30f, 45f, 90f };

        [SerializeField]
        private AnimationCurve[] _smoothTurnSteps;

        [SerializeField]
        private TurnerEventBroadcaster[] _controllerTurners;

        [SerializeField]
        private TurnerEventBroadcaster[] _handTurners;

        [SerializeField]
        private TurnLocomotionBroadcaster[] _locomotionTurners;

        protected bool _started = false;

        protected void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_slider, nameof(_slider));
            this.AssertField(_snapTurnToggle, nameof(_snapTurnToggle));
            this.AssertField(_smoothTurnToggle, nameof(_smoothTurnToggle));
            this.AssertCollectionItems(_snapTurnSteps, nameof(_snapTurnSteps));
            this.AssertCollectionItems(_smoothTurnSteps, nameof(_smoothTurnSteps));
            this.AssertCollectionItems(_locomotionTurners, nameof(_locomotionTurners));
            this.AssertIsTrue(_slider.minValue == 0 && _slider.maxValue == _snapTurnSteps.Length - 1,
                whyItFailed: "The slider does not contains the same number of entries as the Turn Options.");
            this.AssertIsTrue(_smoothTurnSteps.Length == _snapTurnSteps.Length,
                whyItFailed: "You must provide the same amount of options for Smooth and Snap");
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _slider.onValueChanged.AddListener(HandleValueChanged);
                _snapTurnToggle.onValueChanged.AddListener(HandleSnapTurnChanged);
                _smoothTurnToggle.onValueChanged.AddListener(HandleSmoothTurnChanged);
                HandleValueChanged(_slider.value);
                HandleSnapTurnChanged(_snapTurnToggle.isOn);
                HandleSmoothTurnChanged(_smoothTurnToggle.isOn);
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _slider.onValueChanged.RemoveListener(HandleValueChanged);
                _snapTurnToggle.onValueChanged.RemoveListener(HandleSnapTurnChanged);
                _smoothTurnToggle.onValueChanged.RemoveListener(HandleSmoothTurnChanged);
            }
        }

        private void HandleValueChanged(float arg0)
        {
            int index = Mathf.RoundToInt(arg0);
            float snapTurn = _snapTurnSteps[index];
            AnimationCurve smoothTurn = _smoothTurnSteps[index];
            foreach (var turn in _controllerTurners)
            {
                turn.SnapTurnDegrees = snapTurn;
                turn.SmoothTurnCurve = smoothTurn;
            }
            foreach (var turn in _handTurners)
            {
                turn.SnapTurnDegrees = snapTurn;
                turn.SmoothTurnCurve = smoothTurn;
            }
            foreach (var turn in _locomotionTurners)
            {
                turn.SnapTurnDegrees = snapTurn;
                turn.SmoothTurnCurve = smoothTurn;
            }
        }

        private void HandleSnapTurnChanged(bool snapTurn)
        {
            if (!snapTurn)
            {
                return;
            }
            foreach (var turn in _controllerTurners)
            {
                turn.TurnMethod = TurnerEventBroadcaster.TurnMode.Snap;
            }
        }

        private void HandleSmoothTurnChanged(bool smoothTurn)
        {
            if (!smoothTurn)
            {
                return;
            }
            foreach (var turn in _controllerTurners)
            {
                turn.TurnMethod = TurnerEventBroadcaster.TurnMode.Smooth;
            }
        }

    }
}
