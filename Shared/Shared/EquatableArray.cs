using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shared;

/// <summary>
/// Wrapper for <see cref="T"/>[] that implements <see cref="IEquatable{T}"/>,
/// comparing each item using the provided <see cref="EqualityComparer{T}"/>.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
{
    private readonly T[]? _array;
    private readonly EqualityComparer<T>? _comparer;

    private T[] Array => _array ?? [];
    private EqualityComparer<T> Comparer => _comparer ?? EqualityComparer<T>.Default;

    public EquatableArray(IEnumerable<T> array, EqualityComparer<T>? itemComparer = null)
    {
        _array = array.ToArray();
        _comparer = itemComparer;
    }
    
    public T this[int index] => Array[index];

    public int Length => Array.Length;

    public bool Equals(EquatableArray<T> other)
    {
        if (Array.Length != other.Array.Length)
            return false;

        for (int i = 0; i < Array.Length; i++)
        {
            if (!Comparer.Equals(Array[i], other.Array[i]))
                return false;
        }
        return true;
    }
    
    public override bool Equals(object? obj) => obj is EquatableArray<T> arr && Equals(arr);

    public override int GetHashCode()
    {
        unchecked
        {
            const int seed = 487;
            const int modifier = 31;

            int hash = seed;
            
            foreach (var item in Array)
                hash = hash * modifier + Comparer.GetHashCode(item);
            return hash;
        }
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !(left == right);
    
    // ReSharper disable once NotDisposedResourceIsReturned
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    
}