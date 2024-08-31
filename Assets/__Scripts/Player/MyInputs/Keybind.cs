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
        protected string relevantKeyAsStr;
        [SerializeField] protected PlayerInputType howToActivate;
        protected string howToActivateAsStr;

        protected Func<bool> shouldOutput;
        protected bool active;
        protected float heldSince;
        [SerializeField] protected float holdForSeconds;

        public virtual void Init()
        {
            heldSince = float.PositiveInfinity;
            SetRelevantOutputSettings();
        }

        public virtual void ResetHeldSince()
        {
            heldSince = Time.time;
        }

        public void SetKey(KeyCode newKey)
        {
            RelevantKey = newKey;
            relevantKeyAsStr = newKey.ToString();
        }

        public KeyCode GetKey()
        {
            return RelevantKey;
        }

        public void SetInputType(PlayerInputType howToActivate_)
        {
            howToActivate = howToActivate_;
            howToActivateAsStr = howToActivate_.ToString();
            SetRelevantOutputSettings();
        }

        protected void SetRelevantOutputSettings()
        {
            shouldOutput = howToActivate switch
            {
                PlayerInputType.OnKeyDown => CheckKeyDown,
                PlayerInputType.OnKeyUp => CheckKeyUp,
                PlayerInputType.OnKeyHeld => CheckKeyHeld,
                PlayerInputType.Toggle => CheckToggle,
                PlayerInputType.OnHeldForTime => CheckKeyHeldForTime,
                _ => throw new Exception("This activatioon type does not exist")
            };
        }

        protected Func<bool> GetRelevantOutputSettingsFromParam(PlayerInputType param)
        {
            return param switch
            {
                PlayerInputType.OnKeyDown => CheckKeyDown,
                PlayerInputType.OnKeyUp => CheckKeyUp,
                PlayerInputType.OnKeyHeld => CheckKeyHeld,
                PlayerInputType.Toggle => CheckToggle,
                PlayerInputType.OnHeldForTime => CheckKeyHeldForTime,
                _ => throw new Exception("This activvation type does not exist")
            };
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

        public abstract void OnRenderRebindMenu();

        public virtual void DisplayCurrentKey()
        {
            GUILayout.Label(name);
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box);
            GUILayout.Label(relevantKeyAsStr);
            GUILayout.EndHorizontal();
        }
    }
}