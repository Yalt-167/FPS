using SaveAndLoad;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MyCollections
{
    public sealed class ReferenceTypeStruct<T> where T : struct
    {
        public T Value;

        public ReferenceTypeStruct(T value)
        {
            Value = value;
        }
    }
}