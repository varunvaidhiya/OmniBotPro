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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Abstract base class for "decorator" types use in Context-based decoration. While you can inherit from this directly
    /// to create new "decorator" types, in most cases you probably want to inherit instead from
    /// <see cref="ValueToClassDecorator{InstanceT, DecorationT}"/>, <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/>,
    /// or <see cref="ClassToValueDecorator{InstanceT, DecorationT}"/>.
    /// </summary>
    /// <typeparam name="InstanceT">The type of what is being decorated</typeparam>
    /// <typeparam name="DecorationT">The type of the decoration</typeparam>
    /// <remarks>
    /// Context-based decoration is a pattern for easily augmenting existing data with additional information ("decorating" it).
    /// This is useful for any situation where you want more data to be associated with a specific instance than is provided by
    /// that instance by default. For example, <see cref="PokeInteractor"/>s do not expose a notion of "handedness" indicating
    /// whether the user is poking with the left or right hand; however, if knowing that information is important for your
    /// scenario, you can make it available by decorating the interactor with a handedness value.
    ///
    /// The simplest mechanism of decoration is to simply make a Dictionary in which you could associate an individual
    /// interactor with a handedness value. This solution is perfectly viable, but it leaves certain question unaddressed.
    /// Where is that Dictionary? How do you access it? Who is responsible for removing decorations when the information becomes
    /// outdated (such as when the interactor is destroyed)? And, if decorations are added at runtime, how can they be safely
    /// queried without risking order-of-operations dependencies?
    ///
    /// Another simple mechanism of decoration in Unity is appending MonoBehaviours as decorations. For example, you could make
    /// an `InteractorHandedness` MonoBehaviour with a queryable `Handedness` field, then attach an instance of that
    /// MonoBehaviour to the GameObject of each PokeInteractor you wished to decorate; then, to retrieve your decoration, you
    /// could call `GetComponent<InteractorHandedness>` on the PokeInteractor. This, too, is viable and addresses some of the
    /// unanswered questions of a pure Dictionary-based approach. However, it also introduces some limitations and
    /// inconveniences: decorations must be characterized as MonoBehaviours or other types fetchable using `GetComponent`,
    /// limiting the types of data that can be used as decorations; and they must be attached to specific GameObjects, which
    /// can complicate their integration with prefabs and can have other undesirable side effects such as cluttering the Editor
    /// interface.
    ///
    /// DecoratorBase and its descendant types (<see cref="ValueToClassDecorator{InstanceT, DecorationT}"/>,
    /// <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/>, and <see cref="ClassToValueDecorator{InstanceT, DecorationT}"/>)
    /// support Context-based decoration, an alternative pattern to the two described above which avoids some of the issues with
    /// each. Conceptually, these decorators are simply Dictionaries that live as singletons on the <see cref="Context"/>,
    /// resolving questions about the decorator's ownership and accessibility. These decorators also automatically manage the
    /// lifecycle of their entries (the exact mechanism of which varies by concrete type), resolving questions about removing
    /// outdated information. The decorators also all provide both synchronous and asynchronous query mechanisms to deal with the
    /// the fact that decorations can be added at any time, providing callers methods to either try to retrieve any decorations
    /// available immediately or asynchronously wait until a decoration becomes available. All these capabilities are provided
    /// without any constraint on either what can be decorated or what can be used as a decoration: decorations need not be Unity
    /// types, and they needn't show up in the Editor unless the developer of the decoration wants them to. In sum, Context-based
    /// decoration, supported by DecoratorBase and its descendant types, provides a convenient and flexible way to decorate
    /// arbitrary instances with arbitrary data without incurring some of the costs of other approaches.
    ///
    /// This is not the say that DecoratorBase and its descendant types should supersede all other forms of decoration. These
    /// tools are powerful and valuable for appropriate uses, but may not be valuable for others. Note the conspicuous absence of
    /// a `ValueToValueDecorator` type; this is omitted because value-to-value decoration provides no way for decorators to
    /// automatically manage the lifecycle of the decorative relationship (because values have no lifecycle), reducing the utility
    /// of a value-to-value decorator over a simple Dictionary to the point where a simple Context-local Dictionary singleton
    /// is more appropriate than a specialized decorator type. For further information on the lifecycle management features of
    /// the decorators, see the remarks on <see cref="ValueToClassDecorator{InstanceT, DecorationT}"/>,
    /// <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/>, and <see cref="ClassToValueDecorator{InstanceT, DecorationT}"/>.
    /// </remarks>
    public abstract class DecoratorBase<InstanceT, DecorationT>
    {
        private readonly Dictionary<InstanceT, TaskCompletionSource<DecorationT>> _instanceToCompletionSource = new();

        protected void CompleteAsynchronousRequests(InstanceT instance, DecorationT decoration)
        {
            if (_instanceToCompletionSource.TryGetValue(instance, out var source))
            {
                source.SetResult(decoration);
                _instanceToCompletionSource.Remove(instance);
            }
        }

        protected Task<DecorationT> GetAsynchronousRequest(InstanceT instance)
        {
            if (!_instanceToCompletionSource.TryGetValue(instance, out var source))
            {
                source = new();
                _instanceToCompletionSource.Add(instance, source);
            }
            return source.Task;
        }
    }

    /// <summary>
    /// Base class for Context-based decoration, specifically where the instance being decorated
    /// (<typeparamref name="InstanceT"/>) is not a class type, but the data being used as a decoration
    /// (<typeparamref name="DecorationT"/>) is. Automatically removes decoration relationships when the decoration
    /// is cleaned up.
    /// </summary>
    /// <typeparam name="InstanceT">The type of the instance being decorated</typeparam>
    /// <typeparam name="DecorationT">The type of the decoration being added to instances</typeparam>
    /// <remarks>
    /// As with <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/> and other descendants of
    /// <see cref="DecoratorBase{InstanceT, DecorationT}"/>, a goal of ValueToClassDecorator is to be non-invasive and
    /// avoid altering behavior based on decoration. However, because <see cref="InstanceT"/> is not a class type, its
    /// lifecycle cannot be used to determine when to automatically remove its entry in the decorator; thus, even if
    /// the value decorated is no longer in use, decoration based on the instance lifecycle would continue to "pin the
    /// decoration into existence" indefinitely. To avoid this, ValueToClassDecorator automatically cleans up its
    /// decoration relationships based on the lifecycle of the _decoration_, meaning once no references to the decoration
    /// remain outside the decorator, the relationship will be cleaned up to avoid leaks.
    ///
    /// There are scenarios where this behavior is undesirable --- where it is better for the decorator to own a
    /// reference to the decoration such that it can "pin it into existence" despite the fact that
    /// <typeparamref name="InstanceT"/> has no lifecycle. In such scenarios, a simple dictionary singleton on the
    /// Context is a better choice than any descendent of <see cref="DecoratorBase{InstanceT, DecorationT}"/>
    /// as only manual lifecycle management is appropriate.
    /// </remarks>
    public abstract class ValueToClassDecorator<InstanceT, DecorationT> : DecoratorBase<InstanceT, DecorationT> where InstanceT : struct where DecorationT : class
    {
        private readonly Dictionary<InstanceT, WeakReference<DecorationT>> _instanceToDecoration = new();
        private readonly ConditionalWeakTable<DecorationT, Dictionary<InstanceT, FinalAction>> _cleanupActions = new();

        protected ValueToClassDecorator() { }

        /// <summary>
        /// Associates an instance with a decoration. If this instance was already decorated, the old decoration is
        /// removed. If there were any outstanding asynchronous requests for the decoration of this instance (from prior
        /// calls to <see cref="GetDecorationAsync(InstanceT)"/>, those requests will be fulfilled as part of this operation.
        /// </summary>
        /// <param name="instance">The instance to be decorated</param>
        /// <param name="decoration">The decoration to be added to this instance</param>
        public void AddDecoration(InstanceT instance, DecorationT decoration)
        {
            if (_instanceToDecoration.ContainsKey(instance))
            {
                RemoveDecoration(instance);
            }

            _instanceToDecoration.Add(instance, new WeakReference<DecorationT>(decoration));

            if (!_cleanupActions.TryGetValue(decoration, out var finalActions))
            {
                _cleanupActions.Add(decoration, finalActions = new());
            }
            finalActions.Add(instance, new FinalAction(() =>
            {
                if (_instanceToDecoration.ContainsKey(instance))
                {
                    _instanceToDecoration.Remove(instance, out var _);
                }
            }));

            CompleteAsynchronousRequests(instance, decoration);
        }

        /// <summary>
        /// Removes the association between an instance and a decoration.
        /// </summary>
        /// <param name="instance">The instance from which decoration should be removed</param>
        /// <remarks>
        /// If this instance had no decoration, this does nothing. If a decoration was removed, note that
        /// <see cref="GetDecorationAsync(InstanceT)"/> will resume returning uncompleted tasks for this instance, which
        /// will be completed if and when the instance is decorated again.
        /// </remarks>
        public void RemoveDecoration(InstanceT instance)
        {
            if (_instanceToDecoration.TryGetValue(instance, out var weakRef) &&
                weakRef.TryGetTarget(out var decoration))
            {
                if (_cleanupActions.TryGetValue(decoration, out var finalActions) &&
                    finalActions.TryGetValue(instance, out var finalAction))
                {
                    finalAction.Cancel();
                    finalActions.Remove(instance);
                    if (finalActions.Count == 0)
                    {
                        _cleanupActions.Remove(decoration);
                    }
                }
                else
                {
                    Debug.LogAssertion("Failed to remove cleanup action");
                }

                _instanceToDecoration.Remove(instance);
            }
        }

        /// <summary>
        /// Synchronously retrieves the decoration associated with an instance, if one is available.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <param name="decoration">Out parameter to be populated with the decoration if one exists, otherwise null</param>
        /// <returns>True if <paramref name="decoration"/> was populated with a valid decoration, otherwise false</returns>
        public bool TryGetDecoration(InstanceT instance, out DecorationT decoration)
        {
            if (_instanceToDecoration.TryGetValue(instance, out var reference))
            {
                return reference.TryGetTarget(out decoration);
            }
            else
            {
                decoration = null;
                return false;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the decoration associated with an instance.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <returns>A task resulting in the decoration associated with <paramref name="instance"/></returns>
        /// <remarks>
        /// If the instance already has a decoration, the returned task is immediately completed with the currently-available
        /// decoration. If no decoration is yet available for this instance, the returned task will only be completed during
        /// the call to <see cref="AddDecoration(InstanceT, DecorationT)"/> for the instance in question.
        /// </remarks>
        public Task<DecorationT> GetDecorationAsync(InstanceT instance)
        {
            if (_instanceToDecoration.TryGetValue(instance, out var reference) && reference.TryGetTarget(out var decoration))
            {
                return Task.FromResult(decoration);
            }
            else
            {
                return GetAsynchronousRequest(instance);
            }
        }
    }

    /// <summary>
    /// Base class for Context-based decoration, specifically where the instance being decorated
    /// (<typeparamref name="InstanceT"/>) and the  (<typeparamref name="DecorationT"/>) are not a class type.
    /// Note that this class won't remove decoration relationships automatically and it needs to be manually
    /// removed by the client.
    /// </summary>
    /// <typeparam name="InstanceT">The type of the instance being decorated</typeparam>
    /// <typeparam name="DecorationT">The type of the decoration being added to instances</typeparam>
    /// <remarks>
    /// As with <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/> and other descendants of
    /// <see cref="DecoratorBase{InstanceT, DecorationT}"/>, a goal of ValueToValueDecorator is to be non-invasive and
    /// avoid altering behavior based on decoration. However, because neither <see cref="InstanceT"/> nor <see cref="DecorationT"/> 
    /// are a class type, its lifecycle cannot be used to determine when to automatically remove its entry in the decorator; thus,
    /// even if the value decorated is no longer in use, decoration based on the instance lifecycle would continue to "pin the
    /// decoration into existence" indefinitely, until the client actively calls RemoveDecoration.
    /// </remarks>
    public abstract class ValueToValueDecorator<InstanceT, DecorationT> : DecoratorBase<InstanceT, DecorationT> where InstanceT : struct where DecorationT : struct
    {
        private readonly Dictionary<InstanceT, DecorationT> _instanceToDecoration = new();

        protected ValueToValueDecorator() { }

        /// <summary>
        /// Associates an instance with a decoration. If this instance was already decorated, the old decoration is
        /// removed. If there were any outstanding asynchronous requests for the decoration of this instance (from prior
        /// calls to <see cref="GetDecorationAsync(InstanceT)"/>, those requests will be fulfilled as part of this operation.
        /// </summary>
        /// <param name="instance">The instance to be decorated</param>
        /// <param name="decoration">The decoration to be added to this instance</param>
        public void AddDecoration(InstanceT instance, DecorationT decoration)
        {
            if (_instanceToDecoration.ContainsKey(instance))
            {
                RemoveDecoration(instance);
            }

            _instanceToDecoration.Add(instance, decoration);

            CompleteAsynchronousRequests(instance, decoration);
        }

        /// <summary>
        /// Removes the association between an instance and a decoration.
        /// </summary>
        /// <param name="instance">The instance from which decoration should be removed</param>
        /// <remarks>
        /// If this instance had no decoration, this does nothing. If a decoration was removed, note that
        /// <see cref="GetDecorationAsync(InstanceT)"/> will resume returning uncompleted tasks for this instance, which
        /// will be completed if and when the instance is decorated again.
        /// </remarks>
        public void RemoveDecoration(InstanceT instance)
        {
            if (_instanceToDecoration.TryGetValue(instance, out var reference))
            {
                _instanceToDecoration.Remove(instance);
            }
        }

        /// <summary>
        /// Synchronously retrieves the decoration associated with an instance, if one is available.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <param name="decoration">Out parameter to be populated with the decoration if one exists, otherwise null</param>
        /// <returns>True if <paramref name="decoration"/> was populated with a valid decoration, otherwise false</returns>
        public bool TryGetDecoration(InstanceT instance, out DecorationT decoration)
        {
            if (_instanceToDecoration.TryGetValue(instance, out decoration))
            {
                return true;
            }
            else
            {
                decoration = default;
                return false;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the decoration associated with an instance.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <returns>A task resulting in the decoration associated with <paramref name="instance"/></returns>
        /// <remarks>
        /// If the instance already has a decoration, the returned task is immediately completed with the currently-available
        /// decoration. If no decoration is yet available for this instance, the returned task will only be completed during
        /// the call to <see cref="AddDecoration(InstanceT, DecorationT)"/> for the instance in question.
        /// </remarks>
        public Task<DecorationT> GetDecorationAsync(InstanceT instance)
        {
            if (_instanceToDecoration.TryGetValue(instance, out DecorationT decoration))
            {
                return Task.FromResult(decoration);
            }
            else
            {
                return GetAsynchronousRequest(instance);
            }
        }
    }

    /// <summary>
    /// Base class for Context-based decoration, specifically where both the instance being decorated
    /// (<typeparamref name="InstanceT"/>) and the data being used as a decoration (<typeparamref name="DecorationT"/>)
    /// are class types. Automatically removes decorations when the decorated instance is cleaned up.
    /// </summary>
    /// <typeparam name="InstanceT">The type of the instance being decorated</typeparam>
    /// <typeparam name="DecorationT">The type of the decoration being added to instances</typeparam>
    /// <remarks>
    /// As described in the remarks for <see cref="DecoratorBase{InstanceT, DecorationT}"/>, the simplest way to associate
    /// an instance with a decoration is to create a map or Dictionary from <typeparamref name="InstanceT"/> to
    /// <typeparamref name="DecorationT"/>, then store the association there. One often-unintended effect of this
    /// is that, because C# is a managed language, both the instance and the decoration will be "pinned into existence":
    /// because the (long-lived) decorator dictionary contains references to the instance, the garbage collector will
    /// never be able to clean it up, even if nothing else refers to it, which means the fact of _being decorated_ can
    /// change the lifespan of the instance. ClassToClassDecorator avoids this problem by capturing only a weak reference
    /// to the instance (the decoration is still strongly referenced and thus "pinned into existence"). Consequently,
    /// decorating instances using a ClassToClassDecorator is a non-invasive operation which will not alter the lifespan
    /// or other behavior of the decorated instance.
    /// </remarks>
    public abstract class ClassToClassDecorator<InstanceT, DecorationT> : DecoratorBase<InstanceT, DecorationT> where InstanceT : class where DecorationT : class
    {
        private readonly ConditionalWeakTable<InstanceT, DecorationT> _instanceToDecoration = new();

        protected ClassToClassDecorator() { }

        /// <summary>
        /// Associates an instance with a decoration. If this instance was already decorated, the old decoration is
        /// removed. If there were any outstanding asynchronous requests for the decoration of this instance (from prior
        /// calls to <see cref="GetDecorationAsync(InstanceT)"/>, those requests will be fulfilled as part of this operation.
        /// </summary>
        /// <param name="instance">The instance to be decorated</param>
        /// <param name="decoration">The decoration to be added to this instance</param>
        public void AddDecoration(InstanceT instance, DecorationT decoration)
        {
            _instanceToDecoration.Add(instance, decoration);

            CompleteAsynchronousRequests(instance, decoration);
        }

        /// <summary>
        /// Removes the association between an instance and a decoration.
        /// </summary>
        /// <param name="instance">The instance from which decoration should be removed</param>
        /// <remarks>
        /// If this instance had no decoration, this does nothing. If a decoration was removed, note that
        /// <see cref="GetDecorationAsync(InstanceT)"/> will resume returning uncompleted tasks for this instance, which
        /// will be completed if and when the instance is decorated again.
        /// </remarks>
        public void RemoveDecoration(InstanceT instance)
        {
            _instanceToDecoration.Remove(instance);
        }

        /// <summary>
        /// Synchronously retrieves the decoration associated with an instance, if one is available.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <param name="decoration">Out parameter to be populated with the decoration if one exists, otherwise null</param>
        /// <returns>True if <paramref name="decoration"/> was populated with a valid decoration, otherwise false</returns>
        public bool TryGetDecoration(InstanceT instance, out DecorationT decoration)
        {
            return _instanceToDecoration.TryGetValue(instance, out decoration);
        }

        /// <summary>
        /// Asynchronously retrieves the decoration associated with an instance.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <returns>A task resulting in the decoration associated with <paramref name="instance"/></returns>
        /// <remarks>
        /// If the instance already has a decoration, the returned task is immediately completed with the currently-available
        /// decoration. If no decoration is yet available for this instance, the returned task will only be completed during
        /// the call to <see cref="AddDecoration(InstanceT, DecorationT)"/> for the instance in question.
        /// </remarks>
        public Task<DecorationT> GetDecorationAsync(InstanceT instance)
        {
            if (_instanceToDecoration.TryGetValue(instance, out var decoration))
            {
                return Task.FromResult(decoration);
            }
            else
            {
                return GetAsynchronousRequest(instance);
            }
        }
    }

    /// <summary>
    /// Base class for Context-based decoration, specifically where the instance being decorated
    /// (<typeparamref name="InstanceT"/>) is a class type, but the data being used as a decoration
    /// (<typeparamref name="DecorationT"/>) is not. Automatically removes decorations when the decorated instance is
    /// cleaned up.
    /// </summary>
    /// <typeparam name="InstanceT">The type of the instance being decorated</typeparam>
    /// <typeparam name="DecorationT">The type of the decoration being added to instances</typeparam>
    /// <remarks>
    /// As with the <see cref="ClassToClassDecorator{InstanceT, DecorationT}"/>, decorating instances using a
    /// ClassToValueDecorator is a non-invasive operation which will not alter the lifespan or other behavior of the
    /// decorated instance.
    /// </remarks>
    public abstract class ClassToValueDecorator<InstanceT, DecorationT> where InstanceT : class where DecorationT : struct
    {
        private class Wrapper
        {
            public DecorationT _decoration;
        }

        private class InternalDecorator : ClassToClassDecorator<InstanceT, Wrapper> { }
        private readonly InternalDecorator _decorator;

        protected ClassToValueDecorator()
        {
            _decorator = new InternalDecorator();
        }

        /// <summary>
        /// Associates an instance with a decoration. If this instance was already decorated, the old decoration is
        /// removed. If there were any outstanding asynchronous requests for the decoration of this instance (from prior
        /// calls to <see cref="GetDecorationAsync(InstanceT)"/>, those requests will be fulfilled as part of this operation.
        /// </summary>
        /// <param name="instance">The instance to be decorated</param>
        /// <param name="decoration">The decoration to be added to this instance</param>
        public void AddDecoration(InstanceT instance, DecorationT decoration)
        {
            _decorator.AddDecoration(instance, new Wrapper() { _decoration = decoration });
        }

        /// <summary>
        /// Removes the association between an instance and a decoration.
        /// </summary>
        /// <param name="instance">The instance from which decoration should be removed</param>
        /// <remarks>
        /// If this instance had no decoration, this does nothing. If a decoration was removed, note that
        /// <see cref="GetDecorationAsync(InstanceT)"/> will resume returning uncompleted tasks for this instance, which
        /// will be completed if and when the instance is decorated again.
        /// </remarks>
        public void RemoveDecoration(InstanceT instance)
        {
            _decorator.RemoveDecoration(instance);
        }

        /// <summary>
        /// Synchronously retrieves the decoration associated with an instance, if one is available.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <param name="decoration">Out parameter to be populated with the decoration if one exists, otherwise null</param>
        /// <returns>True if <paramref name="decoration"/> was populated with a valid decoration, otherwise false</returns>
        public bool TryGetDecoration(InstanceT instance, out DecorationT decoration)
        {
            if (_decorator.TryGetDecoration(instance, out var wrapper))
            {
                decoration = wrapper._decoration;
                return true;
            }
            else
            {
                decoration = default;
                return false;
            }
        }

        /// <summary>
        /// Asynchronously retrieves the decoration associated with an instance.
        /// </summary>
        /// <param name="instance">The instance for which to retrieve the decoration</param>
        /// <returns>A task resulting in the decoration associated with <paramref name="instance"/></returns>
        /// <remarks>
        /// If the instance already has a decoration, the returned task is immediately completed with the currently-available
        /// decoration. If no decoration is yet available for this instance, the returned task will only be completed during
        /// the call to <see cref="AddDecoration(InstanceT, DecorationT)"/> for the instance in question.
        /// </remarks>
        public Task<DecorationT> GetDecorationAsync(InstanceT instance)
        {
            const TaskContinuationOptions options = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion;
            return _decorator.GetDecorationAsync(instance).ContinueWith(wrapper => wrapper.Result._decoration, options);
        }
    }
}
