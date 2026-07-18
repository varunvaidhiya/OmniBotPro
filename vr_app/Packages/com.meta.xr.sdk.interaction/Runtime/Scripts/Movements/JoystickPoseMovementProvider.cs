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
using Oculus.Interaction.Input;
using System.Linq;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// Provides a movement provider that uses joystick input to control the movement and rotation of an <see cref="Interactable{TInteractor,TInteractable}"/> object. This can be useful for examining an object up close and from different angles.
    /// This class is designed to work with an <see cref="IInteractableView"/> view which is used to access the last selecting controller via the respective <see cref="IInteractorView"/>.
    /// This movement provider allows developers to customize the movement speed, rotation speed, and distance constraints of the <see cref="Interactable{TInteractor,TInteractable}"/> object.
    /// </summary>
    public class JoystickPoseMovementProvider : MonoBehaviour, IMovementProvider
    {
        /// <summary>
        /// The interactable view that this movement provider is associated with.
        /// This property must be set before using the movement provider.
        /// </summary>
        [SerializeField, Interface(typeof(IInteractableView))]
        private MonoBehaviour _interactable;

        private IInteractableView _interactableView;

        [FormerlySerializedAs("moveSpeed")]
        [SerializeField, Optional]
        [Tooltip("The speed at which movement occurs.")]
        private float _moveSpeed = 0.04f;

        /// <summary>
        /// Gets or sets the speed at which movement occurs.
        /// A higher value will result in faster movement.
        /// </summary>
        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value;
        }

        [FormerlySerializedAs("rotationSpeed")]
        [SerializeField, Optional]
        [Tooltip("The speed at which rotation occurs.")]
        private float _rotationSpeed = 1.0f;

        /// <summary>
        /// Gets or sets the speed at which rotation occurs.
        /// A higher value will result in faster rotation.
        /// </summary>
        public float RotationSpeed
        {
            get => _rotationSpeed;
            set => _rotationSpeed = value;
        }

        [SerializeField, Optional, Range(0f, 10f)]
        [Tooltip("The minimum distance along the Z-axis for the grabbed object.")]
        private float _minDistance = 0.1f;

        /// <summary>
        /// Gets or sets the minimum distance along the Z-axis for the grabbed object.
        /// This value must be greater than or equal to 0.
        /// </summary>
        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }

        [SerializeField, Optional, Range(1f, 10f)]
        [Tooltip("The maximum distance along the Z-axis for the grabbed object.")]
        private float _maxDistance = 3.0f;

        /// <summary>
        /// Gets or sets the maximum distance along the Z-axis for the grabbed object.
        /// This value must be greater than or equal to the minimum distance.
        /// </summary>
        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        private IInteractorView _latestSelectingInteractor;

        private void Awake()
        {
            _interactableView = _interactable as IInteractableView;
        }

        private void OnEnable()
        {
            if (_interactableView != null)
            {
                _interactableView.WhenSelectingInteractorViewAdded += OnSelectingInteractorViewAdded;
                _interactableView.WhenSelectingInteractorViewRemoved += OnSelectingInteractorViewRemoved;
            }
        }

        private void OnDisable()
        {
            if (_interactableView != null)
            {
                _interactableView.WhenSelectingInteractorViewAdded -= OnSelectingInteractorViewAdded;
                _interactableView.WhenSelectingInteractorViewRemoved -= OnSelectingInteractorViewRemoved;
            }
        }

        private void OnSelectingInteractorViewAdded(IInteractorView interactor)
        {
            _latestSelectingInteractor = interactor;
        }

        private void OnSelectingInteractorViewRemoved(IInteractorView interactor)
        {
            if (_latestSelectingInteractor == interactor)
            {
                // If the latest interactor is removed, reset or find the next latest
                _latestSelectingInteractor = _interactableView.SelectingInteractorViews.LastOrDefault();
            }
        }

        /// <summary>
        /// Creates a new movement instance that uses joystick input to control the position and rotation of an object.
        /// The movement instance is created with the specified movement speed, rotation speed, minimum distance, and maximum distance.
        /// </summary>
        /// <returns>A new movement instance that can be used to control the position and rotation of an object.</returns>
        public IMovement CreateMovement()
        {
            IController controller = null;

            if (_latestSelectingInteractor != null)
            {
                InteractorControllerDecorator.TryGetControllerForInteractor(_latestSelectingInteractor, out controller);
            }

            return new JoystickPoseMovement(controller, _moveSpeed, _rotationSpeed, _minDistance, _maxDistance);
        }
    }

    /// <summary>
    /// Represents an <see cref="IMovement"/> that uses joystick input to control the position and rotation of an object.
    /// This class provides methods for moving the object to a target pose, updating the target pose, stopping the movement, and adjusting the pose based on joystick input.
    /// </summary>
    public class JoystickPoseMovement : IMovement
    {
        public Pose Pose => _currentPose;
        public bool Stopped => false;

        private Pose _currentPose;
        private Pose _targetPose;
        private Vector3 _localDirection;
        private IController _controller;
        private float _moveSpeed;
        private float _rotationSpeed;
        private float _minDistance;
        private float _maxDistance;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoystickPoseMovement"/> class.
        /// This constructor sets the movement properties, such as the controller, movement speed, rotation speed, minimum distance, and maximum distance.
        /// </summary>
        /// <param name="controller">The controller to use for the movement.</param>
        /// <param name="moveSpeed">The speed at which to move the object.</param>
        /// <param name="rotationSpeed">The speed at which to rotate the object.</param>
        /// <param name="minDistance">The minimum distance along the Z-axis for the grabbed object.</param>
        /// <param name="maxDistance">The maximum distance along the Z-axis for the grabbed object.</param>
        public JoystickPoseMovement(IController controller, float moveSpeed, float rotationSpeed, float minDistance, float maxDistance)
        {
            _controller = controller;
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
        }

        /// <summary>
        /// Moves the object to the specified target pose using the movement speed and rotation speed.
        /// The target pose is used as a reference point to calculate the movement delta.
        /// </summary>
        /// <param name="target">The target pose to move to.</param>
        public void MoveTo(Pose target)
        {
            _targetPose = target;
            _localDirection = Quaternion.Inverse(_targetPose.rotation)
                * (_currentPose.position - _targetPose.position).normalized;
        }

        /// <summary>
        /// Updates the target pose of the movement to the specified pose.
        /// This method does not affect the current pose of the object.
        /// </summary>
        /// <param name="target">The new target pose.</param>
        public void UpdateTarget(Pose target)
        {
            _targetPose = target;
        }

        /// <summary>
        /// Stops the movement and sets the current pose to the specified pose.
        /// This method resets the movement state and sets the current pose to the specified value.
        /// </summary>
        /// <param name="pose">The pose to set as the current pose.</param>
        public void StopAndSetPose(Pose pose)
        {
            _currentPose = pose;
        }

        /// <summary>
        /// Updates the movement based on the current joystick input.
        /// This method adjusts the pose of the object based on the joystick input and movement speed.
        /// </summary>
        public void Tick()
        {
            AdjustPoseWithJoystickInput();
        }

        /// <summary>
        /// Adjusts the pose of the movement based on the current joystick input.
        /// This method calculates the movement delta based on the joystick input and adjusts the pose accordingly.
        /// It also takes into account the movement speed, rotation speed, minimum distance, and maximum distance.
        /// </summary>
        public void AdjustPoseWithJoystickInput()
        {
            if (_controller == null)
            {
                return;
            }

            Vector2 joystickInput = _controller.ControllerInput.Primary2DAxis;

            float moveDelta = joystickInput.y * _moveSpeed;
            float rotationDelta = -joystickInput.x * _rotationSpeed;

            Vector3 direction = _targetPose.rotation * _localDirection;
            Vector3 controllerPosition = _targetPose.position;
            Vector3 controllerToObject = _currentPose.position - controllerPosition;

            float currentDistanceAlongForward = Vector3.Project(controllerToObject, direction).magnitude;
            float newDistanceAlongForward = Mathf.Clamp(currentDistanceAlongForward + moveDelta, _minDistance, _maxDistance);

            Vector3 newPosition = controllerPosition + direction * newDistanceAlongForward;
            Quaternion newRotation = Quaternion.AngleAxis(rotationDelta, Vector3.up) * _currentPose.rotation;

            _currentPose = new Pose(newPosition, newRotation);
            UpdateTarget(_currentPose);
        }

        public void InjectController(IController controller)
        {
            _controller = controller;
        }
    }
}
