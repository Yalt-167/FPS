using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace WeaponHandling
{
    public sealed class WeaponBehaviourGatherer<BehaviourType>
    {
        private BehaviourType[] internalArray;
        private int currentIndex = 0;

        public BehaviourType Current => internalArray[currentIndex++ % internalArray.Length];


        public WeaponBehaviourGatherer(BehaviourType[] components)
        {
            internalArray = components;
        }

        public void Setup()
        {
            currentIndex = 0;
        }
    }
}