using Menus;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Inputs
{
    [Serializable]
    public abstract class Keybind
    {
        protected string name;

        [SerializeField] protected KeyCode RelevantKey;
        protected string relevantKeyAsStr;

        [SerializeField] protected InputType inputType;
        protected string inputTypeAsStr;

        private Func<bool> shouldOutput;

        protected bool active;

        protected float heldSince;
        [SerializeField] protected float holdForSeconds;

        protected bool canBeRemapped;
        protected bool currentlyRebinding;
        protected static readonly string listeningForInput = "Listening for input";


        public virtual void Init()
        {
            heldSince = float.PositiveInfinity;
            SetRelevantOutputSettings();
        }

        public void ResetHeldSince()
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
            shouldOutput = GetRelevantOutputSettingsFromParam(inputType);
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

        // <summary>
        /// This method must be called every frame because it relies on CheckKeyDown() and could miss the relevant frame<br/>
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// This method must be called every frame because it relies on CheckKeyDown() and could miss the relevant frame<br/>
        /// WARNING: remember to cache the value each frame and do not recall it else the toggle will cancel itself
        /// </summary>
        /// <returns></returns>
        protected bool CheckToggle()
        {
            return active = CheckKeyDown() ? !active : active;
        }

        public void ForceToggle(bool towardOn)
        {
            active = towardOn;
        }

        public static implicit operator bool(Keybind bind)
        {
            return bind.shouldOutput();
        }

        /// <summary>
        /// Returns wether one of the bind is being remapped
        /// </summary>
        public abstract bool OnRenderRebindMenu();

        private void Rebind()
        {
            MyUtility.CoroutineStarter.HandleCoroutine(RebindInternal());
        }

        private IEnumerator RebindInternal()
        {
            currentlyRebinding = true;

            yield return new WaitUntil(() => Input.anyKeyDown);

            foreach (KeyCode keycode in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keycode))
                {
                    SetKey(keycode == KeyCode.Escape ? KeyCode.None : keycode);
                    break;
                }
            }

            currentlyRebinding = false;
        }

        /// <summary>
        /// Returns wether a rebind is ongoing on this bind
        /// </summary>
        /// <returns></returns>
        public virtual bool DisplayCurrentKey()
        {
            GUILayout.Label(name, GUILayout.Width(MenuData.RemapInput.ActionNameDisplayWidth));
            GUI.enabled = canBeRemapped && !currentlyRebinding;
            if (GUILayout.Button(currentlyRebinding ? listeningForInput : relevantKeyAsStr, GUILayout.Width(MenuData.RemapInput.KeybindDisplayWidth)))
            {
                Rebind();
            }
            GUI.enabled = true;

            return currentlyRebinding;
        }

        public virtual void DisplayInputType()
        {
            GUILayout.BeginHorizontal(CachedGUIStylesNames.Box, GUILayout.Width(MenuData.RemapInput.InputTypeDisplayWidth));
            GUILayout.Label(inputTypeAsStr);
            GUILayout.EndHorizontal();
        }
    }
}