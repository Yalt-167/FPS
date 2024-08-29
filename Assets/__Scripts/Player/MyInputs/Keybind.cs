using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    public abstract class Keybind
    {
        protected string name;
        [SerializeField] protected KeyCode RelevantKey;
        [SerializeField] protected PlayerActionActivationType howToActivate;

        protected Func<bool> shouldOutput;
        protected bool active;
        protected float heldSince;
        [SerializeField] protected float holdForSeconds;

        public void Init(IInputQuery inputQuery)
        {
            heldSince = float.PositiveInfinity;
            SetRelevantOutputSettings();
            inputQuery.RegisterKeybind(this);
        }

        public void ResetState()
        {
            heldSince = Time.time;
        }

        public void SetKey(KeyCode newKey)
        {
            RelevantKey = newKey;
        }

        public KeyCode GetKey()
        {
            return RelevantKey;
        }

        protected void SetRelevantOutputSettings()
        {
            switch (howToActivate)
            {
                case PlayerActionActivationType.OnKeyDown:
                    shouldOutput = CheckKeyDown;
                    break;

                case PlayerActionActivationType.OnKeyUp:
                    shouldOutput = CheckKeyUp;
                    break;

                case PlayerActionActivationType.OnKeyHeld:
                    shouldOutput = CheckKeyHeld;
                    break;

                case PlayerActionActivationType.Toggle:
                    shouldOutput = CheckToggle;
                    break;

                case PlayerActionActivationType.OnHeldForTime:
                    shouldOutput = CheckKeyHeldForTime;
                    break;

                default:
                    Debug.Log("sth wrong");
                    break;
            }
        }

        protected bool CheckKeyDown()
        {
            return Input.GetKeyDown(RelevantKey);
        }

        protected bool CheckKeyUp()
        {
            return Input.GetKeyUp(RelevantKey);
        }

        protected bool CheckKeyHeld()
        {
            return Input.GetKey(RelevantKey);
        }

        protected bool CheckKeyHeldForTime()
        {
            if (CheckKeyDown())
            {
                heldSince = Time.time;
            }
            else if (!CheckKeyHeld())
            {
                heldSince = float.PositiveInfinity;
            }

            return Time.time - heldSince > holdForSeconds;
        }

        protected bool CheckToggle()
        {
            if (CheckKeyDown())
            {
                active = !active;
            }

            return active;
        }

        public static implicit operator bool(Keybind bind)
        {
            return bind.shouldOutput();
        }

        public abstract void OnRenderRebingMenu();
    }
}