using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Direction
{
    Forward,
    ForwardRight,
    Right,
    BackwardRight,
    Backward,
    BackwardLeft,
    Left,
    ForwardLeft,
    None
}

public static class DirectionUtility
{
    public static Direction GetDirectionFromAxises(int forwardAxis, int sidewayAxis)
    {
        if (forwardAxis == 0)
        {
            if (sidewayAxis == 0) { return Direction.None; }

            return sidewayAxis > 0 ? Direction.Right : Direction.Left;
        }

        if (sidewayAxis == 0)
        {
            return forwardAxis > 0 ? Direction.Forward : Direction.Backward;
        }

        return sidewayAxis > 0 ? forwardAxis > 0 ? Direction.ForwardRight : Direction.BackwardRight : forwardAxis > 0 ? Direction.ForwardLeft : Direction.BackwardLeft;
    }

    public static Direction GetSimpleDirectionFromAxis(int axis, bool wantSidewayDirection)
    {
        if (axis == 0) { return Direction.None; }

        return wantSidewayDirection ? (axis > 0 ? Direction.Right : Direction.Left) : (axis > 0 ? Direction.Forward : Direction.Backward);
    }

    public static Direction InvertDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Forward => Direction.Backward,
            Direction.ForwardRight => Direction.BackwardLeft,
            Direction.Right => Direction.Left,
            Direction.BackwardRight => Direction.ForwardLeft,
            Direction.Backward => Direction.Forward,
            Direction.BackwardLeft => Direction.ForwardRight,
            Direction.Left => Direction.Right,
            Direction.ForwardLeft => Direction.BackwardRight,
            Direction.None => Direction.None,
            _ => Direction.None
        };
    }
}
