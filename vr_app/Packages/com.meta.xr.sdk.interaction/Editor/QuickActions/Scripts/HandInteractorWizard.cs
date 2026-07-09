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

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Oculus.Interaction.Input;

namespace Oculus.Interaction.Editor.QuickActions
{
    using static InteractorUtils;

    internal class HandInteractorWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Interactor(s) To Hand";

        [MenuItem(MENU_NAME, priority = MenuOrder.HAND_INTERACTORS)]
        private static void OpenWizard()
        {
            ShowWindow<HandInteractorWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
        static bool Validate()
        {
            return Selection.gameObjects.Length == 1 &&
                GetBaseComponent<Hand>(Selection.gameObjects[0].transform, false, true) != null;
        }

        #region Fields

        [SerializeField]
        [WizardSetting]
        [Tooltip("The selected interactor types will be added to the hand.")]
        private InteractorTypes _interactorTypes = InteractorTypes.All;

        [SerializeField]
        [WizardDependency(ReadOnly = true)]
        [Tooltip("New interactor(s) will be added to this hand.")]
        private Hand _hand;

        [SerializeField]
        [WizardDependency(FindMethod = nameof(FindHmd))]
        [Tooltip("The player HMD instance within the scene.")]
        private Hmd _hmd;

        [SerializeField]
        [WizardDependency, ChangeCheck(nameof(OnTransformChanged))]
        [InspectorName("Parent Transform")]
        [Tooltip("New interactor(s) will be instantiated under this transform.")]
        private Transform _transform;

        [SerializeField]
        [WizardDependency(Category = Category.Optional)]
        [Tooltip("If provided, interactor(s) will be added to this InteractorGroup.")]
        private InteractorGroup _interactorGroup;

        #endregion Fields

        private void OnTransformChanged()
        {
            _interactorGroup = _transform == null ? null :
                _transform.GetComponent<InteractorGroup>();
        }

        private static void RemoveDuplicateInteractors(Transform root,
            ref InteractorTypes interactorType)
        {
            interactorType &= ~GetExistingHandInteractors(root);
        }

        private void FindHmd()
        {
            _hmd = GetHmd();
        }

        protected override void Create()
        {
            AddInteractorsToHand(_interactorTypes,
                _hand, _hmd, _transform, _interactorGroup);
        }

        protected override void InitializeFieldsExtra()
        {
            base.InitializeFieldsExtra();

            // If any selected objects contains a child Hand
            _hand = GetBaseComponent<Hand>(Target.transform, false, false);
            if (_hand == null)
            {
                _hand = GetBaseComponent<Hand>(Target.transform, false, true);
            }

            if (_interactorGroup == null && _hand != null)
            {
                if (TryFindInteractorsGroup(_hand, out _interactorGroup, out Transform holder)
                    && _transform == null)
                {
                    _transform = holder;
                }
            }

            if (_interactorGroup != null && _transform == null)
            {
                _transform = _interactorGroup.transform;
            }

            if (_transform != null)
            {
                _interactorTypes &= ~GetExistingHandInteractors(_transform);
            }
        }

        protected override IEnumerable<MessageData> GetMessages()
        {
            IEnumerable<MessageData> messages = Enumerable.Empty<MessageData>();
            if (_transform != null)
            {
                foreach (InteractorTypes value in Enum.GetValues(typeof(InteractorTypes)))
                {
                    InteractorTypes existingTypes = GetExistingHandInteractors(_transform);
                    if (_interactorTypes.HasFlag(value) &&
                        existingTypes.HasFlag(value) &&
                        TryGetTypeForHandInteractor(value, out Type type))
                    {
                        messages = messages.Append(new MessageData(MessageType.Error,
                        $"There is already a {type.Name} added to this Hand.",
                        new ButtonData("Do Not Add", () => _interactorTypes &= ~value)));
                    }
                }
            }
            if ((_interactorTypes & InteractorTypes.All) == 0)
            {
                messages = messages.Append(new MessageData(MessageType.Error,
                    "No interactor types selected."));
            }
            if (_hand != null && _transform != null &&
                !_transform.IsChildOf(_hand.transform))
            {
                messages = messages.Append(new MessageData(MessageType.Warning,
                $"It recommended to add interactors under their associated Hand."));
            }

            foreach (InteractorTypes value in Enum.GetValues(typeof(InteractorTypes)))
            {
                if (value != InteractorTypes.All &&
                    value != InteractorTypes.None &&
                    _interactorTypes.HasFlag(value) &&
                    !Templates.TryGetHandInteractorTemplate(value, out _))
                {
                    messages = messages.Append(new MessageData(MessageType.Error,
                    $"No existing {value} template for this device.",
                    new ButtonData("Do Not Add", () => _interactorTypes &= ~value)));
                }
            }
            return messages;
        }

        #region Inject

        public void InjectAllHandInteractorWizard(InteractorTypes interactorTypes,
            Hand hand, Hmd hmd, Transform transform)
        {
            InjectInteractorTypes(interactorTypes);
            InjectHand(hand);
            InjectHmd(hmd);
            InjectTransform(transform);
        }

        public void InjectInteractorTypes(InteractorTypes interactorTypes)
        {
            _interactorTypes = interactorTypes;
        }

        public void InjectHand(Hand hand)
        {
            _hand = hand;
        }

        public void InjectHmd(Hmd hmd)
        {
            _hmd = hmd;
        }

        public void InjectTransform(Transform transform)
        {
            _transform = transform;
        }

        public void InjectOptionalInteractorGroup(InteractorGroup group)
        {
            _interactorGroup = group;
        }


        #endregion
    }
}
