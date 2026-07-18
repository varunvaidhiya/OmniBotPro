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

using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// PointerInteractable provides a base template for any kind of interaction which can be characterized as, "pointing at something."
    /// Interactables of this kind, examples of which include <see cref="PokeInteractable"/> and <see cref="RayInteractable"/>,
    /// receive <see cref="PointerEvent"/>s from interacting <typeparamref name="TInteractor"/>s and re-emit those events to describe
    /// their behavior during interaction, in addition to the signals and data provided by
    /// <see cref="Interactable{TInteractor, TInteractable}"/>s.
    /// </summary>
    /// <remarks>
    /// Like <see cref="Interactable{TInteractor, TInteractable}"/>, this type has a
    /// [curiously recurring](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern) generic argument
    /// <typeparamref name="TInteractable"/>, which should be the concrete interactable type which derives from this type and is
    /// uniquely associated with <typeparamref name="TInteractor"/>.
    /// </remarks>
    public abstract class PointerInteractable<TInteractor, TInteractable> : Interactable<TInteractor, TInteractable>,
        IPointable
        where TInteractor : Interactor<TInteractor, TInteractable>
        where TInteractable : PointerInteractable<TInteractor, TInteractable>
    {
        [SerializeField, Interface(typeof(IPointableElement))]
        [Optional(OptionalAttribute.Flag.DontHide)]
        private UnityEngine.Object _pointableElement;

        /// <summary>
        /// The "think which can be pointed at" associated with this pointer interactable. This property is typically set through
        /// the Unity Editor and populated from there during MonoBehaviour start-up, but it can also be set programmatically using
        /// <see cref="InjectOptionalPointableElement(IPointableElement)"/>. This property is optional can can return null.
        /// </summary>
        /// <remarks>
        /// For example, a <see cref="RayInteractable"/> might have a <see cref="PointableCanvas"/> with which one can interact by
        /// pointing a <see cref="RayInteractor"/>'s ray at it, in which case the canvas would be the PointableElement.
        /// </remarks>
        public IPointableElement PointableElement { get; protected set; }

        /// <summary>
        /// Implementation of <see cref="IPointable.WhenPointerEventRaised"/>; for details, please refer to the related
        /// documentation provided for that interface.
        /// </summary>
        public event Action<PointerEvent> WhenPointerEventRaised = delegate { };

        /// <summary>
        /// Internal API which publishes the provided <see cref="PointerEvent"/>, making sure that <see cref="PointableElement"/>
        /// has the first opportunity to handle the event before it is emitted more broadly via <see cref="WhenPointerEventRaised"/>.
        /// This method is automatically invoked by <see cref="PointerInteractor{TInteractor, TInteractable}"/> as part of the
        /// <see cref="PointerEvent"/> propagation process, and it should not be called directly from outside that process.
        /// </summary>
        /// <param name="evt"></param>
        public void PublishPointerEvent(PointerEvent evt)
        {
            if (PointableElement != null)
            {
                PointableElement.ProcessPointerEvent(evt);
            }
            WhenPointerEventRaised(evt);
        }

        protected override void Awake()
        {
            base.Awake();
            PointableElement = _pointableElement as IPointableElement;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            if (_pointableElement != null)
            {
                this.AssertField(PointableElement, nameof(PointableElement));
            }
            this.EndStart(ref _started);
        }

        #region Inject

        /// <summary>
        /// Sets the <see cref="PointableElement"/> on a dynamically instantiated GameObject. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectOptionalPointableElement(IPointableElement pointableElement)
        {
            PointableElement = pointableElement;
            _pointableElement = pointableElement as UnityEngine.Object;
        }

        #endregion
    }
}
