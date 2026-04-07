using Glai.Allocator;

namespace Glai.Collection
{
    public struct FixedDictionary<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
    {
        private FixedArray<TKey> keys;
        private FixedArray<TValue> values;
        private FixedArray<int> hashes;
        private FixedArray<int> distance;

        public FixedDictionary(int capacity, MemoryStateHandle memoryStateHandle, MemoryState memoryState)
        {
            if (capacity <= 0)
            {
                throw new System.ArgumentException("Capacity must be greater than zero.", nameof(capacity));
            }

            hashes = new FixedArray<int>(capacity, memoryStateHandle, memoryState);
            values = new FixedArray<TValue>(capacity, memoryStateHandle, memoryState);
            keys = new FixedArray<TKey>(capacity, memoryStateHandle, memoryState);
            distance = new FixedArray<int>(capacity, memoryStateHandle, memoryState);
        }

        public void Dispose(MemoryState memoryState)
        {
            hashes.Dispose(memoryState);
            values.Dispose(memoryState);
            keys.Dispose(memoryState);
            distance.Dispose(memoryState);
        }

        public void Add(TKey key, TValue value)
        {
            int hash = key.GetHashCode();
            hash = hash == 0 ? 1 : hash; 
            int index = (hash & 0x7fffffff) % hashes.Capacity;
            int dist = 0;

            while (hashes[index] != 0)
            {
                if (hashes[index] == hash && keys[index].Equals(key))
                {
                    values[index] = value;
                    return;
                }

                if (distance[index] < dist)
                {
                    int tempHash = hashes[index];
                    TKey tempKey = keys[index];
                    TValue tempValue = values[index];
                    int tempDist = distance[index];

                    hashes[index] = hash;
                    keys[index] = key;
                    values[index] = value;
                    distance[index] = dist;

                    hash = tempHash;
                    key = tempKey;
                    value = tempValue;
                    dist = tempDist;
                }

                index = (index + 1) % hashes.Capacity;
                dist++;
            }

            keys[index] = key;
            hashes[index] = hash;
            values[index] = value;
            distance[index] = dist;
        }

        public bool ContainsKey(TKey key)
        {
            int hash = key.GetHashCode();
            hash = hash == 0 ? 1 : hash;
            int index = (hash & 0x7fffffff) % hashes.Capacity;
            int dist = 0;

            while (hashes[index] != 0)
            {
                if (hashes[index] == hash && keys[index].Equals(key))
                {
                    return true;
                }

                if (distance[index] < dist)
                {
                    break;
                }

                index = (index + 1) % hashes.Capacity;
                dist++;
            }

            return false;
        }

        public void Remove(TKey key)
        {
            int hash = key.GetHashCode();
            hash = hash == 0 ? 1 : hash;
            int index = (hash & 0x7fffffff) % hashes.Capacity;
            int dist = 0;

            while (hashes[index] != 0)
            {
                if (hashes[index] == hash && keys[index].Equals(key))
                {
                    hashes[index] = 0;
                    keys[index] = default;
                    values[index] = default;
                    distance[index] = 0;
                    return;
                }

                if (distance[index] < dist)
                {
                    break;
                }

                index = (index + 1) % hashes.Capacity;
                dist++;
            }

            if (hashes[index] == 0)
                return;

            int next = (index + 1) % hashes.Capacity;

            while (hashes[next] != 0 && distance[next] > 0)
            {
                hashes[index] = hashes[next];
                keys[index] = keys[next];
                values[index] = values[next];
                distance[index] = distance[next] - 1;

                index = next;
                next = (next + 1) % hashes.Capacity;
            }
        }

        public TValue Get(TKey key)
        {
            if (TryGetValue(key, out TValue value))
            {
                return value;
            }

            throw new System.Exception($"Key {key} not found in FixedDictionary.");
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = key.GetHashCode();
            hash = hash == 0 ? 1 : hash;
            int index = (hash & 0x7fffffff) % hashes.Capacity;
            int dist = 0;

            while (hashes[index] != 0)
            {
                if (hashes[index] == hash && keys[index].Equals(key))
                {
                    value = values[index];
                    return true;
                }

                if (distance[index] < dist)
                {
                    break;
                }

                index = (index + 1) % hashes.Capacity;
                dist++;
            }

            value = default;
            return false;
        }

        public void SetValue(TKey key, TValue value)
        {
            int hash = key.GetHashCode();
            hash = hash == 0 ? 1 : hash;
            int index = (hash & 0x7fffffff) % hashes.Capacity;
            int dist = 0;

            while (hashes[index] != 0)
            {
                if (hashes[index] == hash && keys[index].Equals(key))
                {
                    values[index] = value;
                    return;
                }

                if (distance[index] < dist)
                {
                    break;
                }

                index = (index + 1) % hashes.Capacity;
                dist++;
            }

            throw new System.Exception($"Key {key} not found in FixedDictionary.");
        }

        public TValue this[TKey key]
        {
            get { return Get(key); }
            set { SetValue(key, value); }
        }
    }
}
