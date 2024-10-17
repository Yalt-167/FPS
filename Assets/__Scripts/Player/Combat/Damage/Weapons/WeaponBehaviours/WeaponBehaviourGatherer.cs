using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponBehaviourGatherer<BehaviourType> : IEnumerable<BehaviourType>
    {
        private BehaviourType[] internalArray;
        private int currentIndex = 0;
        public int Index => currentIndex;


        public BehaviourType Current => internalArray[currentIndex % internalArray.Length];

        public BehaviourType GetCurrentAndIndex(out int index)
        {
            index = currentIndex;
            return Current;
        }

        public void GoNext()
        {
            currentIndex++;
        }

        public WeaponBehaviourGatherer(BehaviourType[] components)
        {
            if (components.Length == 0) { throw new Exception("No BehaviourFound"); }

            internalArray = components;
        }

        public void Setup()
        {
            currentIndex = 0;
        }

        public IEnumerable<BehaviourType> GetElements()
        {
            foreach (var component in internalArray)
            {
                yield return component;
            }
        }

        public IEnumerator<BehaviourType> GetEnumerator()
        {
            foreach (var component in internalArray)
            {
                yield return component;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }      
    }
}