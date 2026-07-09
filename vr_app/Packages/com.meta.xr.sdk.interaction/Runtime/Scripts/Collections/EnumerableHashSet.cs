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

namespace Oculus.Interaction.Collections
{
    /// <summary>
    /// Exposes a GetEnumerator method with a non-allocating HashSet.Enumerator struct.
    /// </summary>
    /// <remarks>
    /// This is an advanced performance optimization for scenarios where an interface representation of
    /// a hash set is needed, but the allocations associated with boxing the enumerator as an IEnumerable<T>
    /// are problematic (for example, because of invocation in per-frame logic as in
    /// <see cref="Interactable{TInteractor, TInteractable}"/>). In general, iteration over concrete collection
    /// types avoids the need for this interface; but for scenarios where exposing the full contract of a HashSet
    /// would be improper (as again in <see cref="Interactable{TInteractor, TInteractable}"/>s, where
    /// <see cref="Interactable{TInteractor, TInteractable}.Interactors"/> should expose the ability to
    /// _enumerate_ the collection without the ability to _modify_ the collection), this interface and its
    /// implementing type <see cref="EnumerableHashSet{T}"/> can fulfill the required contract without unintended
    /// allocations.
    /// </remarks>
    public interface IEnumerableHashSet<T> : IEnumerable<T>
    {
        /// <summary>
        /// Gets the number of elements that are contained in a set.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.count?view=net-8.0#system-collections-generic-hashset-1-count).
        /// </remarks>
        int Count { get; }

        /// <summary>
        /// Returns a non-allocating enumerator that iterates through an IEnumerableHashSet<T> object.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.getenumerator?view=net-8.0#system-collections-generic-hashset-1-getenumerator).
        /// </remarks>
        new HashSet<T>.Enumerator GetEnumerator();

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object contains the specified element.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.contains?view=net-8.0#system-collections-generic-hashset-1-contains(-0)).
        /// </remarks>
        bool Contains(T item);

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object is a proper subset of the specified collection.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.ispropersubsetof?view=net-8.0#system-collections-generic-hashset-1-ispropersubsetof(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        bool IsProperSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object is a proper superset of the specified collection.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.ispropersupersetof?view=net-8.0#system-collections-generic-hashset-1-ispropersupersetof(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        bool IsProperSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object is a subset of the specified collection.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.issubsetof?view=net-8.0#system-collections-generic-hashset-1-issubsetof(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        bool IsSubsetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object is a superset of the specified collection.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.issupersetof?view=net-8.0#system-collections-generic-hashset-1-issupersetof(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        bool IsSupersetOf(IEnumerable<T> other);

        /// <summary>
        /// Determines whether the current IEnumerableHashSet<T> object and a specified collection share common elements.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.Overlaps. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.overlaps?view=net-8.0#system-collections-generic-hashset-1-overlaps(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        public bool Overlaps(IEnumerable<T> other);

        /// <summary>
        /// Determines whether an IEnumerableHashSet<T> object and the specified collection contain the same elements.
        /// </summary>
        /// <remarks>
        /// The implementation of this method in <see cref="EnumerableHashSet{T}"/> is the built-in method HashSet<T>.SetEquals. For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.setequals?view=net-8.0#system-collections-generic-hashset-1-setequals(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        public bool SetEquals(IEnumerable<T> other);
    }

    /// <summary>
    /// A hash set that implements the <see cref="IEnumerableHashSet{T}"/> interface, to use for non-allocating
    /// iteration of a HashSet.
    /// </summary>
    /// <remarks>
    /// For an overview of the relevance and applicability of this type, see the remarks
    /// on <see cref="IEnumerableHashSet{T}"/>.
    /// </remarks>
    public class EnumerableHashSet<T> : HashSet<T>, IEnumerableHashSet<T>
    {
        /// <summary>
        /// Default constructor, functionally a wrapper for the underlying hash set's equivalent constructor.
        /// </summary>
        /// <remarks>
        /// For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.-ctor?view=net-8.0#system-collections-generic-hashset-1-ctor).
        /// </remarks>
        public EnumerableHashSet() : base() { }

        /// <summary>
        /// Enumerable constructor, functionally a wrapper for the underlying hash set's equivalent constructor.
        /// </summary>
        /// <remarks>
        /// For more information, please consult the
        /// [official documentation for HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.-ctor?view=net-8.0#system-collections-generic-hashset-1-ctor(system-collections-generic-ienumerable((-0)))).
        /// </remarks>
        public EnumerableHashSet(IEnumerable<T> values) : base(values) { }
    }
}
