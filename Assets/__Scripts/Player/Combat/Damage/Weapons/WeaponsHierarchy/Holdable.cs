using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;
using UnityEngine.Video;

namespace WeaponHandling
{
    public abstract class Holdable : NetworkBehaviour
    {
        public virtual void OnPullOut() { }
        public virtual void OnPutAway() { }

        public virtual void OnPickUp() { }
        public virtual void OnDrop() { }

        public virtual void OnPrimaryUseKeyDown() { }
        public virtual void OnPrimaryUseKeyUp() { }

        public virtual void OnSecondaryUseKeyDown() { }
        public virtual void OnSecondaryUseKeyUp() { }

        public virtual void OnTertiaryUseKeyDown() {  }
        public virtual void OnTertiaryUseKeyUp() {  }

#nullable enable
        protected T? SearchComponentInChildren<T>(Transform? transformNullable = null) where T : Component
        {
            Transform transform_ = transformNullable ?? transform;

            if (transform_.gameObject.TryGetComponent<T>(out var component))
            {
                return component;
            }

            if (transform_.childCount == 0) {  return null; }

            T? result;
            int i = 0;

            do
            {
                result = SearchComponentInChildren<T>(transform_.GetChild(i));
            } while (i++ < transform_.childCount && (result == null));

            return result;
        }

        protected T[] SearchComponentsInChildren<T>(Transform? transformNullable = null, List<T>? resultNullable = null) where T : Component
        {
            Transform transform_ = transformNullable ?? transform;
            List<T> result = resultNullable ?? new List<T>();

            if (transform_.gameObject.TryGetComponent<T>(out var component))
            {
                result.Add(component);
            }

            for (int i = 0; i < transform_.childCount; i++)
            {
                SearchComponentsInChildren<T>(transform_.GetChild(i), result);
            }

            return result.ToArray();
        }
#nullable disable
    }
}