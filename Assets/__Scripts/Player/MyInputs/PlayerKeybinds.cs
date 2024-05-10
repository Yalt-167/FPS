using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class MovementInputQuery : InputQuery
{

    public FixedKeybind InitiateForward = new(KeyCode.Z, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind Forward = new(KeyCode.Z, PlayerActionActivationType.OnKeyHeld);

    public FixedKeybind InitiateBack = new(KeyCode.S, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind Back = new(KeyCode.S, PlayerActionActivationType.OnKeyHeld);

    public FixedKeybind InitiateRight = new(KeyCode.D, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind HoldRightForTime = new(KeyCode.D, PlayerActionActivationType.OnHeldForTime);
    public FixedKeybind Right = new(KeyCode.D, PlayerActionActivationType.OnKeyHeld);

    public FixedKeybind InitiateLeft = new(KeyCode.Q, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind HoldLeftForTime = new(KeyCode.Q, PlayerActionActivationType.OnHeldForTime); 
    public FixedKeybind Left = new(KeyCode.Q, PlayerActionActivationType.OnKeyHeld);

    public FixedKeybind InitiateJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind HoldJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyHeld);
    public FixedKeybind InterruptJump = new(KeyCode.Space, PlayerActionActivationType.OnKeyUp);

    public FixedKeybind InitiateCrouch = new(KeyCode.Space, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind HoldCrouch = new(KeyCode.Space, PlayerActionActivationType.OnKeyHeld);

    public FixedKeybind Dash = new(KeyCode.Alpha9, PlayerActionActivationType.OnKeyDown);

    public FixedKeybind InitiateGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyDown);
    public FixedKeybind HoldGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyHeld);
    public FixedKeybind ReleaseGrapplingHook = new(KeyCode.X, PlayerActionActivationType.OnKeyUp);

    public FixedKeybind SwitchCameraPosition = new(KeyCode.W, PlayerActionActivationType.OnKeyDown);
    public VariableKeybind QuickReset = new(KeyCode.Alpha9, new List<PlayerActionActivationType>() { PlayerActionActivationType.OnKeyDown, PlayerActionActivationType.OnHeldForTime }, .5f);

    public FixedKeybind Pause = new(KeyCode.Escape, PlayerActionActivationType.OnKeyDown);

    public override void Init()
    {
        InitiateForward.Init();
        Forward.Init();

        InitiateBack.Init();
        Back.Init();

        InitiateRight.Init();
        Right.Init();
        HoldRightForTime.Init();

        InitiateLeft.Init();
        Left.Init();
        HoldLeftForTime.Init();


        InitiateJump.Init();
        HoldJump.Init();
        InterruptJump.Init();


        InitiateCrouch.Init();
        HoldCrouch.Init();
        Dash.Init();


        InitiateGrapplingHook.Init();
        HoldGrapplingHook.Init();
        ReleaseGrapplingHook.Init();


        SwitchCameraPosition.Init();


        QuickReset.Init();
        Pause.Init();
    }
}

[Serializable]
public class CombatInputQuery : InputQuery
{
    public FixedKeybind Shoot;
    public VariableKeybind Aim;
    public FixedKeybind Reload;
    public FixedKeybind Slash;
    public FixedKeybind Parry;
    public FixedKeybind ChangeCrosshair;

    public override void Init()
    {
        Shoot.Init();
        Aim.Init();
        Reload.Init();
        Slash.Init();
        Parry.Init();

        ChangeCrosshair.Init();
    }
}

public abstract class InputQuery
{
    public abstract void Init();
}

public abstract class KeyBind
{
    [SerializeField] protected KeyCode RelevantKey;
    [SerializeField] protected PlayerActionActivationType howToActivate;

    protected Func<bool> shouldOutput;
    protected bool active;
    protected float heldSince;
    [SerializeField] protected float holdForSeconds;

    public void Init()
    {
        heldSince = float.PositiveInfinity;
        SetRelevantOutputSettings();
    }

    public void ResetState()
    {
        heldSince = Time.time;
    }

    public void SetKey(KeyCode newKey)
    {
        RelevantKey = newKey;
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

    public static implicit operator bool(KeyBind bind)
    {
        return bind.shouldOutput();
    }
}

[Serializable]
public class VariableKeybind : KeyBind
{
    [SerializeField] private List<PlayerActionActivationType> allowedActivationTypes;

   
    private int activationTypeIndex;
    private int activationTypesLength;

    /// <summary>
    /// First provided activation type is gonna be selected as default
    /// </summary>
    /// <param name="relevantKey"></param>
    /// <param name="_allowedActivationTypes"></param>
    public VariableKeybind(KeyCode relevantKey, List<PlayerActionActivationType> _allowedActivationTypes)
    {
        RelevantKey = relevantKey;
        allowedActivationTypes = _allowedActivationTypes;
        howToActivate = allowedActivationTypes[0];
    }

    public VariableKeybind(KeyCode relevantKey, List<PlayerActionActivationType> _allowedActivationTypes, float _holdForSeconds)
    {
        RelevantKey = relevantKey;
        allowedActivationTypes = _allowedActivationTypes;
        howToActivate = allowedActivationTypes[0];
        holdForSeconds = _holdForSeconds;
    }

    public new void Init()
    {
        base.Init();
        activationTypeIndex = allowedActivationTypes.IndexOf(howToActivate);
        activationTypesLength = allowedActivationTypes.Count;
    }

    public void SetActivationType(PlayerActionActivationType newActivationType)
    {
        howToActivate = newActivationType;
        SetRelevantOutputSettings();
    }

    public void NextActivationType()
    {
        howToActivate = allowedActivationTypes[++activationTypeIndex % activationTypesLength];
        SetRelevantOutputSettings();
    }
}

[Serializable]
public class FixedKeybind : KeyBind
{
    public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes)
    {
        RelevantKey = relevantKey;
        howToActivate = activationTypes;
    }

    public FixedKeybind(KeyCode relevantKey, PlayerActionActivationType activationTypes, float _holdForSeconds)
    {
        RelevantKey = relevantKey;
        howToActivate = activationTypes;
        holdForSeconds = _holdForSeconds;
    }
}

public enum PlayerActionActivationType
{
    OnKeyDown,
    OnKeyUp,
    OnKeyHeld,
    Toggle,
    OnHeldForTime,
    //StartedHoldingAfterEvent
}