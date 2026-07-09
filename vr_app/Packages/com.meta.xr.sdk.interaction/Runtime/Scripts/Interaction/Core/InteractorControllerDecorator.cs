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

using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Associates one or more <see cref="IInteractorView"/>s with an <see cref="IController"/>
    /// using Context decoration patterns.
    /// </summary>
    /// <remarks>
    /// This is useful in scenarios where knowing the controller associated with an interaction is valuable, even
    /// if the interactor in question does not expose it.
    /// </remarks>
    public class InteractorControllerDecorator : MonoBehaviour
    {
        private class Decorator : ClassToClassDecorator<IInteractorView, IController>
        {
            private Decorator() { }

            public static Decorator GetFromContext(Context context)
            {
                return context.GetOrCreateSingleton<Decorator>(() => new());
            }
        }

        private class ValueDecorator : ValueToClassDecorator<int, IController>
        {
            private ValueDecorator() { }

            public static ValueDecorator GetFromContext(Context context)
            {
                return context.GetOrCreateSingleton<ValueDecorator>(() => new());
            }
        }

        /// <summary>
        /// Retrieves the <see cref="IController"/>, if there is one, with which an <see cref="IInteractorView"/>
        /// has been associated using Context decoration.
        /// </summary>
        /// <param name="interactor">The interactor which may have been associated with a controller</param>
        /// <param name="controller">The controller associated with the interactor</param>
        /// <returns>
        /// True if <paramref name="controller"/> was populated with a valid associated <see cref="IController"/>,
        /// false otherwise.
        /// </returns>
        public static bool TryGetControllerForInteractor(IInteractorView interactor, out IController controller)
        {
            var context = Context.Global.GetInstance();
            return Decorator.GetFromContext(context).TryGetDecoration(interactor, out controller);
        }

        /// <summary>
        /// Retrieves the <see cref="IController"/> associated with a specific interactor identifier
        /// through value-based context decoration
        /// </summary>
        /// <param name="InteractorId">The unique identifier of the interactor to query</param>
        /// <param name="controller">When this method returns, contains the associated <see cref="IController"/> 
        /// if found; otherwise, null</param>
        /// <returns>
        /// True if a controller association exists for the specified ID, false otherwise
        /// </returns>
        public static bool TryGetControllerForInteractorId(int InteractorId,
            out IController controller)
        {
            var context = Context.Global.GetInstance();
            return ValueDecorator.GetFromContext(context)
                .TryGetDecoration(InteractorId, out controller);
        }

        [SerializeField, Interface(typeof(IInteractorView))]
        [Tooltip("Individually-listed interactors to be associated with the specified IController " +
            "via Context decoration")]
        private Component[] _interactors;

        [SerializeField]
        [Tooltip("Individually-listed GameObjects which are the roots of interactor hierarchies; on " +
            "initialization, all IInteractorView instances hierarchically descended from these " +
            "GameObjects will be associated with the specified IController via Context decoration")]
        private GameObject[] _interactorHierarchies;

        [SerializeField, Interface(typeof(IController))]
        [Tooltip("The IController to be associated with the specified IInteractorViews via Context " +
            "decoration.")]
        private Component _controller;

        private void Awake()
        {
            var context = Context.Global.GetInstance();
            var decorator = Decorator.GetFromContext(context);
            var controller = _controller as IController;

            foreach (var component in _interactors)
            {
                var interactor = component as IInteractorView;
                decorator.AddDecoration(interactor, controller);
            }

            foreach (var hierarchy in _interactorHierarchies)
            {
                var interactors = hierarchy.GetComponentsInChildren<IInteractorView>();
                foreach (var interactor in interactors)
                {
                    decorator.AddDecoration(interactor, controller);
                }
            }
        }

        private void Start()
        {
            var context = Context.Global.GetInstance();
            var valueDecorator = ValueDecorator.GetFromContext(context);
            var controller = _controller as IController;

            foreach (var component in _interactors)
            {
                if (component is IInteractorView interactor)
                {
                    valueDecorator.AddDecoration(interactor.Identifier, controller);
                }
            }

            foreach (var hierarchy in _interactorHierarchies)
            {
                var interactors = hierarchy.GetComponentsInChildren<IInteractorView>();
                foreach (var interactor in interactors)
                {
                    if (interactor != null)
                    {
                        valueDecorator.AddDecoration(interactor.Identifier, controller);
                    }
                }
            }
        }
    }
}
