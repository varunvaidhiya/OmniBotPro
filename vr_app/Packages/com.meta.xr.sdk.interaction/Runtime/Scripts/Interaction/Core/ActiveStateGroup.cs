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

using System.Collections.Generic;
using System.Threading.Tasks;
using Oculus.Interaction.PoseDetection.Debug;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Manages a collection of <see cref="IActiveState"/> instances and evaluates their combined state using a specified logical operator.
    /// This class is used to aggregate multiple active states, enabling complex interaction logic by combining various conditions.
    /// </summary>
    public class ActiveStateGroup : MonoBehaviour, IActiveState
    {
        /// <summary>
        /// Defines the logical operators that can be applied to the active states within the group. Each operator changes how the group's active state is determined.
        /// This can define whether all states must be active (AND), any state must be active (OR), or if exactly one state must be active (XOR)
        /// </summary>
        public enum ActiveStateGroupLogicOperator
        {
            AND = 0,
            OR = 1,
            XOR = 2
        }

        /// <summary>
        /// The logic operator will be applied to these IActiveStates.
        /// </summary>
        [Tooltip("The logic operator will be applied to these IActiveStates.")]
        [SerializeField, Interface(typeof(IActiveState))]
        private List<UnityEngine.Object> _activeStates;
        private List<IActiveState> ActiveStates;

        /// <summary>
        /// IActiveStates will have this boolean logic operator applied.
        /// </summary>
        [Tooltip("IActiveStates will have this boolean logic operator applied.")]
        [SerializeField]
        private ActiveStateGroupLogicOperator _logicOperator = ActiveStateGroupLogicOperator.AND;

        protected virtual void Awake()
        {
            ActiveStates = _activeStates.ConvertAll(mono => mono as IActiveState);
        }

        protected virtual void Start()
        {
            this.AssertCollectionItems(ActiveStates, nameof(ActiveStates));
        }
        /// <summary>
        /// Evaluates the combined state of all active states in the group based on the specified logical operator to determine if the group should be considered active.
        /// </summary>
        public bool Active
        {
            get
            {
                if (ActiveStates == null)
                {
                    return false;
                }

                switch (_logicOperator)
                {
                    case ActiveStateGroupLogicOperator.AND:
                        foreach (IActiveState activeState in ActiveStates)
                        {
                            if (!activeState.Active) return false;
                        }
                        return true;

                    case ActiveStateGroupLogicOperator.OR:
                        foreach (IActiveState activeState in ActiveStates)
                        {
                            if (activeState.Active) return true;
                        }
                        return false;

                    case ActiveStateGroupLogicOperator.XOR:
                        bool foundActive = false;
                        foreach (IActiveState activeState in ActiveStates)
                        {
                            if (activeState.Active)
                            {
                                if (foundActive) return false;
                                foundActive = true;
                            }
                        }
                        return foundActive;

                    default:
                        return false;
                }
            }
        }

        static ActiveStateGroup()
        {
            ActiveStateDebugTree.RegisterModel<ActiveStateGroup>(new DebugModel());
        }

        private class DebugModel : ActiveStateModel<ActiveStateGroup>
        {
            protected override Task<IEnumerable<IActiveState>> GetChildrenAsync(ActiveStateGroup instance)
            {
                return Task.FromResult<IEnumerable<IActiveState>>(instance.ActiveStates);
            }
        }

        #region Inject
        /// <summary>
        /// Injects a list of <see cref="IActiveState"/> states into the group.
        /// </summary>
        /// <param name="activeStates">The list of active states to inject.</param>
        public void InjectAllActiveStateGroup(List<IActiveState> activeStates)
        {
            InjectActiveStates(activeStates);
        }
        /// <summary>
        /// Injects a list of <see cref="IActiveState"/> states into the group and converts them for internal use.
        /// </summary>
        /// <param name="activeStates">The list of active states to set.</param>
        public void InjectActiveStates(List<IActiveState> activeStates)
        {
            ActiveStates = activeStates;
            _activeStates = activeStates.ConvertAll(activeState => activeState as UnityEngine.Object);
        }
        /// <summary>
        /// Injects an optional logic operator into the <see cref="ActiveStateGroup"/>. This operator is used to determine how the individual active states in the
        /// group should be combined to evaluate the group's overall active state
        /// </summary>
        /// <param name="logicOperator">The logic operator to be applied to the group's active state.</param>
        public void InjectOptionalLogicOperator(ActiveStateGroupLogicOperator logicOperator)
        {
            _logicOperator = logicOperator;
        }

        #endregion
    }
}
