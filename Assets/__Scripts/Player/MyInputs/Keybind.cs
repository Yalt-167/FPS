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
        [SerializeField] protected InputType inputType;
        protected string inputTypeAsStr;

        protected Func<bool> shouldOutput;
        protected bool active;
        protected float heldSince;
        [SerializeField] protected float holdForSeconds;
        bool currentlyRebinding;
        private static readonly string listeningForInput = "Listening for input";

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

        public void SetInputType(InputType howToActivate_)
        {
            inputType = howToActivate_;
            inputTypeAsStr = howToActivate_.ToString();
            SetRelevantOutputSettings();
        }

        protected void SetRelevantOutputSettings()
        {
            shouldOutput = inputType switch
            {
                InputType.OnKeyDown => CheckKeyDown,
                InputType.OnKeyUp => CheckKeyUp,
                InputType.OnKeyHeld => CheckKeyHeld,
                InputType.Toggle => CheckToggle,
                InputType.OnKeyHeldForTime => CheckKeyHeldForTime,
                _ => throw new Exception("This activatioon type does not exist")
            };
        }

        protected Func<bool> GetRelevantOutputSettingsFromParam(InputType param)
        {
            return param switch
            {
                InputType.OnKeyDown => CheckKeyDown,
                InputType.OnKeyUp => CheckKeyUp,
                InputType.OnKeyHeld => CheckKeyHeld,
                InputType.Toggle => CheckToggle,
                InputType.OnKeyHeldForTime => CheckKeyHeldForTime,
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

        private IEnumerator Rebind()
        {
            currentlyRebinding = true;

            yield return new WaitUntil(() => Input.anyKeyDown);

            foreach (KeyCode keycode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keycode))
                {
                    SetKey(keycode);
                }
            }

            currentlyRebinding = false;
        }

        public virtual void DisplayCurrentKey()
        {
            GUILayout.Label(name, GUILayout.Width(200));
            GUI.enabled = !currentlyRebinding;
            if (GUILayout.Button(currentlyRebinding ? listeningForInput : relevantKeyAsStr, GUILayout.Width(200)))
            {
                Utility.CoroutineStarter.Instance.HandleCoroutine(Rebind());
            }
            GUI.enabled = true;
        }

        public virtual void DisplayInputType()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(200));
            GUILayout.Label(inputTypeAsStr);
            GUILayout.EndHorizontal();
        }
    }
}