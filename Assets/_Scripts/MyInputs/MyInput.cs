using System.Collections;
using System.Collections.Generic;

public static class MyInput
{
    public static int GetAxis(bool minExtent, bool maxExtent)
    {
        return minExtent ?
            maxExtent ? 0 : -1
            :
            maxExtent ? 1 : 0;
    }
}
