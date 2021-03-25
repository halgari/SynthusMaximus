using System;
using System.Collections.Generic;
using Wabbajack.Common;

namespace SynthusMaximus.Data.Enums
{
    /// <summary>
    /// Defines a enum that gets its values from the keys in a json hashmap
    /// </summary>
    public class DynamicEnum<T>
    {
        private RelativePath _file;
        public OverlayLoader Loader { get; }
        private Lazy<IDictionary<string, T>> _data;

        public DynamicEnum(RelativePath file, OverlayLoader loader)
        {
            _file = file;
            Loader = loader;
            _data = new Lazy<IDictionary<string, T>>(() => Loader.LoadDictionaryCaseInsensitive<T>(file));
        }

        public bool TryGetValue(string k, out DynamicEnumMember v)
        {
            if (_data.Value.ContainsKey(k))
            {
                v = new DynamicEnumMember(k, this);
                return true;
            }

            throw new KeyNotFoundException($"Enum {k} does not exist in {typeof(T).Name} enum");
        }

        private bool HasKey(string k)
        {
            return _data.Value.ContainsKey(k);
        }

        public DynamicEnumMember this[string member]
        {
            get
            {
                if (HasKey(member))
                    return new DynamicEnumMember(member, this);

                var othermember = member.Replace("_", "").Replace(" ", "");
                if (HasKey(othermember))
                    return new DynamicEnumMember(othermember, this);
                
                throw new KeyNotFoundException($"Enum {member} does not exist in {typeof(T).Name} enum");
            }
        }
        

        public readonly struct DynamicEnumMember : IEquatable<DynamicEnumMember>
        {
            private readonly string _member;
            private readonly DynamicEnum<T> _denum;

            public DynamicEnumMember(string member, DynamicEnum<T> denum)
            {
                _member = member;
                _denum = denum;
            }

            public T Data => _denum._data.Value[_member];

            public bool Equals(DynamicEnumMember other)
            {
                return _member == other._member && _denum.Equals(other._denum);
            }

            public override bool Equals(object? obj)
            {
                return obj is DynamicEnumMember other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_member, _denum);
            }

            public override string ToString()
            {
                return $"{typeof(T).Name}.{_member}";
            }
        }


    }
}