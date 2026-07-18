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
using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// A numerical identifier guaranteed to be unique within an instance of an application.
    /// </summary>
    /// <remarks>
    /// This is used to uniquely associate instances with data they generate, most commonly <see cref="PointerEvent"/>s
    /// retaining the <see cref="IInteractorView.Identifier"/> of their originating interactors as their own
    /// <see cref="PointerEvent.Identifier"/>.
    /// </remarks>
    public class UniqueIdentifier
    {
        private class Decorator : ValueToClassDecorator<int, object>
        {
            private Decorator() { }

            public static Decorator GetFromContext(Context context)
            {
                return context.GetOrCreateSingleton<Decorator>(() => new());
            }
        }

        /// <summary>
        /// The numerical identifier associated with this UniqueIdentifier. This number is guaranteed to be unique
        /// at any given time within an instance of an application.
        /// </summary>
        /// <remarks>
        /// "Unique at any given time" means there is never more than one valid UniqueIdentifier with the same ID
        /// at the same time within an instance of an application. However, different UniqueIdentifiers may have the
        /// same numerical ID if they do not exist at the same time as long as <see cref="Release(UniqueIdentifier)"/>
        /// has been called on the earlier instance before <see cref="Generate"/> produces the later.
        ///
        /// This number is often used in place of the <see cref="UniqueIdentifier"/> itself; for example,
        /// <see cref="IInteractorView.Identifier"/> and the <see cref="PointerEvent.Identifier"/>s which refer to
        /// it both leveral the numerical value of the interactor's underlying UniqueIdentifier.ID.
        /// </remarks>
        public int ID { get; private set; }

        private Context _context;

        private UniqueIdentifier(int identifier, Context context)
        {
            ID = identifier;
            _context = context;
        }

        private static System.Random Random = new System.Random();
        private static HashSet<int> _identifierSet = new HashSet<int>();

        /// <summary>
        /// Creates a new UniqueIdentifier with a numerical <see cref="ID"/> which is guaranteed to be unique in the
        /// application for the lifespan of the new UniqueIdentifier (i.e., until <see cref="Release(UniqueIdentifier)"/>
        /// is called upon it).
        /// </summary>
        /// <returns>The new UniqueIdentifier</returns>
        [Obsolete]
        public static UniqueIdentifier Generate()
        {
            while (true)
            {
                int identifier = Random.Next(Int32.MaxValue);
                if (_identifierSet.Contains(identifier)) continue;
                _identifierSet.Add(identifier);
                return new UniqueIdentifier(identifier, Context.Global.GetInstance());
            }
        }

        /// <summary>
        /// Creates a new UniqueIdentifier with a numerical <see cref="ID"/> which is guaranteed to be unique in the
        /// application for the lifespan of the new UniqueIdentifier (i.e., until <see cref="Release(UniqueIdentifier)"/>
        /// is called upon it). Associates that identifier with the provided <paramref name="instance"/> for future
        /// retrieval via <see cref="TryGetInstanceFromIdentifier(Context, int, out object)"/>.
        /// </summary>
        /// <param name="context">The context in which the <paramref name="instance"/> will be associated with the new identifier</param>
        /// <param name="instance">The instance associated with the new identifier</param>
        /// <returns>The new UniqueIdentifier</returns>
        public static UniqueIdentifier Generate(Context context, object instance)
        {
            while (true)
            {
                int identifier = Random.Next(Int32.MaxValue);
                if (_identifierSet.Contains(identifier)) continue;

                _identifierSet.Add(identifier);
                Decorator.GetFromContext(context).AddDecoration(identifier, instance);
                return new UniqueIdentifier(identifier, context);
            }
        }

        /// <summary>
        /// Invalidates a UniqueIdentifier and returns its <see cref="ID"/> to the space of allowable <see cref="ID"/>s
        /// for new UniqueIdentifiers. Any further use of the <paramref name="identifier"/> after release is unsupported
        /// and can cause undefined behavior.
        /// </summary>
        /// <param name="identifier">The UniqueIdentifier to be invalidated</param>
        public static void Release(UniqueIdentifier identifier)
        {
            _identifierSet.Remove(identifier.ID);
            Decorator.GetFromContext(identifier._context).RemoveDecoration(identifier.ID);
        }

        /// <summary>
        /// Attempts to retrieve the <paramref name="instance"/> associated with the provided <paramref name="identifier"/>
        /// in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> in which to look for an instance association</param>
        /// <param name="identifier">The <see cref="ID"/> of the UniqueIdentifier for which to find the instance</param>
        /// <param name="instance">Out parameter, the discovered associated instance, if one exists</param>
        /// <returns>True if the instance was found, false otherwise</returns>
        public static bool TryGetInstanceFromIdentifier(Context context, int identifier, out object instance)
        {
            return Decorator.GetFromContext(context).TryGetDecoration(identifier, out instance);
        }

        /// <summary>
        /// Attempts to retrieve the <paramref name="instance"/> associated with the provided <paramref name="identifier"/>
        /// in the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> in which to look for an instance association</param>
        /// <param name="identifier">The <see cref="ID"/> of the UniqueIdentifier for which to find the instance</param>
        /// <param name="instance">Out parameter, the discovered associated instance, if one exists</param>
        /// <returns>True if the instance was found, false otherwise</returns>
        public static bool TryGetInstanceFromIdentifier<TInstance>(Context context, int identifier, out TInstance instance)
        {
            if (Decorator.GetFromContext(context).TryGetDecoration(identifier, out var instanceObj) &&
                instanceObj is TInstance instanceT)
            {
                instance = instanceT;
                return true;
            }
            instance = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the instance associated with the provided <paramref name="identifier"/> in the provided
        /// <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> in which to look for an instance association</param>
        /// <param name="identifier">The <see cref="ID"/> of the UniqueIdentifier for which to find the instance</param>
        /// <returns>A task returning the associated instance, if one exists</returns>
        public static Task<object> GetInstanceFromIdentifierAsync(Context context, int identifier)
        {
            return Decorator.GetFromContext(context).GetDecorationAsync(identifier);
        }
    }
}
