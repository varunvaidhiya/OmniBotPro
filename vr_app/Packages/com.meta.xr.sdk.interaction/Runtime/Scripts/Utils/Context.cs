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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Context is a tool for coordinating the operation of disparate instances in a generalized and decoupled way:
    /// different instances can collaborate with one another without directly depending on one another by sharing
    /// data and conventions using a Context. A common example of this kind of collaboration is for instance
    /// decoration; for detailed commentary on that usage, see <see cref="DecoratorBase{InstanceT, DecorationT}"/>.
    /// </summary>
    /// <remarks>
    /// Conceptually, a Context is a logical scope of ownership and operation which is not bound to a specific
    /// region of the implementation. Just as a code scope (the braces after an `if` or `for` statement, for example)
    /// both owns and provides access to the data it contains --- for example, an `int` variable declared in a scope
    /// will be automatically released when the scope end, but within the scope it can be accessed by its name --- so
    /// too is a Context capable of owning and providing access to data required by the operations it supports/logically
    /// "contains." Contexts are a common feature of many libraries, including examples such as
    /// [System.AppContext](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-appcontext)
    /// from the .NET API itself.
    ///
    /// The Context implementation provided here is specifically designed to support a "safe singleton" pattern in
    /// Unity applications. The "safe singleton" pattern aims to provide the majority of the benefits of singletons
    /// (mainly simplicity) while avoiding the problems associated with global state; this is achieved because
    /// "safe singletons" are not "global" to the app, but are merely "global" to a particular Context instance which
    /// owns, enforces the uniqueness of, and provides access to its singletons.
    ///
    /// The impact of this pattern on true global state is thus confined to the provided <see cref="Global"/> Context
    /// instance. This instance is provided for convenience and is very valuable for quick-and-dirty work. However,
    /// because this does have an (albeit confined) impact on centralized global state, development which must be
    /// scalable should not rely on the <see cref="Global"/> Context. Libraries, for example, which wish to leverage
    /// Context-based patterns should simply define and expose their own <see cref="Instance"/> to avoid colliding
    /// with global considerations.
    /// </remarks>
    public class Context : MonoBehaviour
    {
        /// <summary>
        /// Instance is a helper class for conveniently creating custom <see cref="Context"/>s using a code-centric
        /// approach.
        /// </summary>
        /// <remarks>
        /// When creating your own Context instances, there are two main patterns you can use: an Editor-centric
        /// pattern and a code-centric pattern. For an Editor-centric pattern, simply attach a Context to a
        /// GameObject and "wire it up" to Editor-exposed fields as you would with any other Unity Component.
        /// The accessibility and lifespan, in Editor or in Code, of a Context created this way adheres to typical
        /// Unity patterns. This usage is most appropriate small-scope uses --- for example, if an individual prefab
        /// needs a Context specific to its own usage to faciliate operations local to the context of each prefab
        /// instance.
        ///
        /// By contrast, the Instance class is intended to help with a code-centric pattern. In this pattern, the
        /// Context in question is lazily created at runtime (rather than saved as part of the scene) and thus
        /// cannot be "wired up" to dependencies in the Editor. While the Context within an Instance can be
        /// retrieved using Component accessors at runtime, it should typically be accessed through the Instance
        /// proper, which will lazily initialize the Context instance as needed. This pattern is most appropriate
        /// for large and/or long-lived usage scopes, particularly for Contexts which should not be destroyed before
        /// app teardown. Destroying Instance Contexts, as well as accessing them during app teardown, involves
        /// special considerations explored more deeply in the remarks on <see cref="GetInstance"/>.
        /// </remarks>
        public class Instance
        {
            private readonly string _name;
            private Context _instance;

            /// <summary>
            /// Basic constructor. This sets the name which will be given to the lazily-created GameObject which
            /// will host the underlying Context.
            /// </summary>
            /// <param name="name">The name which will be given to the lazily-created GameObject</param>
            public Instance(string name)
            {
                _name = name;
            }

            /// <summary>
            /// Retrieves the <see cref="Context"/> which underlies this Instance; if a valid one does not currently
            /// exist, a new one is lazily created.
            /// </summary>
            /// <returns>The Context underlying this Instance</returns>
            /// <remarks>
            /// Note that destroying the GameObject and/or Context created by an Instance _is_ supported and will
            /// successfully tear down the Context; however, if GetInstance is subsequently queried, it will simply
            /// lazily create a new Context. This has particular implications on the use of Instance Contexts during
            /// app teardown, where order of destruction is not guaranteed and attempts to access an existing
            /// Instance Context may result in the creation of a new Context instead if the prior Context happens to
            /// have been destroyed first. For this reason, if access to a specific Context is needed in teardown
            /// logic (or to a lesser degree any other logic), you should locally cache a reference to the underlying
            /// Context and only invoke GetInstance if the old instance is destroyed and you specifically need a
            /// reference to the new one.
            /// </remarks>
            public Context GetInstance()
            {
                if (_instance == null)
                {
                    GameObject go = new();
                    go.name = _name;
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                    _instance = go.AddComponent<Context>();
                }
                return _instance;
            }
        }

        private static SynchronizationContext _unityMainThreadSynchronizationContext = null;
        private static Queue<Action> _unityMainThreadWork = new();
        private static Mutex _unityMainThreadWorkMutex = new();

        /// <summary>
        /// Executes the specified work on the main Unity thread.
        /// </summary>
        /// <param name="work">The work to execute on the main thread</param>
        /// <remarks>
        /// This function provides an easy way for work triggered from arbitrary threads to be performed specifically
        /// on the main thread; for example, <see cref="FinalAction"/> uses this method to cause a callback invoked
        /// during destruction (which can happen on any thread) to be executed on the Unity main thread.
        ///
        /// Unity 6+ introduces improved support for asynchronous programming techniques, including a new
        /// [`Awaitable.MainThreadAsync`](https://docs.unity3d.com/6000.0/Documentation/Manual/async-await-support.html)
        /// which can be used for the same purpose as this method.
        /// </remarks>
        public static void ExecuteOnMainThread(Action work)
        {
            _unityMainThreadWorkMutex.WaitOne();
            if (_unityMainThreadSynchronizationContext != null)
            {
                _unityMainThreadSynchronizationContext.Post(_ =>
                {
                    work();
                }, null);
            }
            else
            {
                _unityMainThreadWork.Enqueue(work);
            }
            _unityMainThreadWorkMutex.ReleaseMutex();
        }

        /// <summary>
        /// The global <see cref="Context"/>, which can be used as a default Context for quick-and-dirty development.
        /// </summary>
        /// <remarks>
        /// This Instance is provided for convenience and can be safely used for end-product development (i.e., for
        /// the development of an individual game), but development that must incorporate or scale (i.e., library
        /// development) should rely on independently Context Instances instead. See the remarks on
        /// <see cref="Context"/> and <see cref="Instance"/> for a more detailed exploration of this topic.
        /// </remarks>
        public static Instance Global { get; } = new("Global Context");

        /// <summary>
        /// Event which signals the destruction of a <see cref="Context"/>, allowing dependent instances an opportunity
        /// to react.
        /// </summary>
        public event Action WhenDestroyed;

        private readonly ConcurrentDictionary<Type, object> _singletons = new();

        private void Awake()
        {
            if (_unityMainThreadSynchronizationContext == null)
            {
                _unityMainThreadWorkMutex.WaitOne();
                _unityMainThreadSynchronizationContext = SynchronizationContext.Current;
                while (_unityMainThreadWork.TryDequeue(out var work))
                {
                    work();
                }
                _unityMainThreadWorkMutex.ReleaseMutex();
            }
            Debug.Assert(_unityMainThreadSynchronizationContext == SynchronizationContext.Current);
        }

        /// <summary>
        /// Retrieves a Context-local or "safe" singleton instance from this Context. If an instance of the requested
        /// type already exists on this Context, that instance will be returned; otherwise, a new one will be lazily
        /// created.
        /// </summary>
        /// <typeparam name="T">The type of the singleton to be retrieved</typeparam>
        /// <returns>A Context-local singleton of the requested type</returns>
        /// <remarks>
        /// This method implements the "safe singleton" pattern discussed in the remarks on the <see cref="Context"/>.
        /// While the name "GetOrCreate" is used to clarify that the requested instance _may_ be created during this
        /// call, this method should really only be thought of as an accessor with any instantiation that may or may
        /// not happen being simply an irrelevant implementation detail. Ideally, you should think of Context-local
        /// singletons as aspects of the Context itself rather than as separate instances which can be created and
        /// destroyed independently. Adhering to this thinking will ensure that order-of-operations and independent
        /// lifecycles do not complicate the usage of Context. As long as the Context merely exists, _all_ singletons
        /// on it can _always_ be accessed identically using GetOrCreateSingleton; all other considerations are the
        /// purview of the singletons themselves.
        /// </remarks>
        public T GetOrCreateSingleton<T>() where T : class, new()
        {
            var type = typeof(T);
            object singleton;
            if (!_singletons.TryGetValue(type, out singleton))
            {
                singleton = new T();
                _singletons.TryAdd(type, singleton);
            }
            return singleton as T;
        }

        /// <summary>
        /// Retrieves a Context-local or "safe" singleton instance from this Context. If an instance of the requested
        /// type already exists on this Context, that instance will be returned; otherwise, a new one will be lazily
        /// created by invoking the provided <paramref name="factory"/> callback.
        /// </summary>
        /// <typeparam name="T">The type of the singleton to be retrieved</typeparam>
        /// <param name="factory">A callback which creates a singleton of the requested type</param>
        /// <returns>A Context-local singleton of the requested type</returns>
        /// <remarks>
        /// This method differs from <see cref="GetOrCreateSingleton{T}"/> in that it accepts a
        /// <paramref name="factory"/> callback, which it will invoke to create the singleton if necessary instead
        /// of simply calling `new()`. This is useful for singleton types which can only be constructed with
        /// arguments, or for types with private constructors. This method implements the "safe singleton" pattern
        /// discussed in the remarks on <see cref="Context"/> and further explored in the remarks on
        /// <see cref="GetOrCreateSingleton{T}"/>.
        /// </remarks>
        public T GetOrCreateSingleton<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            object singleton;
            if (!_singletons.TryGetValue(type, out singleton))
            {
                singleton = factory();
                _singletons.TryAdd(type, singleton);
            }
            return singleton as T;
        }

        private void OnDestroy()
        {
            WhenDestroyed?.Invoke();
        }
    }
}
