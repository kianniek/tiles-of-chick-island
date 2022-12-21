using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorExtension 
{
    /// <summary>
    /// Determine whether the two colors are relatively similar.
    /// </summary>
    /// <param name="c1">The first color</param>
    /// <param name="c2">The second color</param>
    /// <param name="tolerance">The tolarence for difference</param>
    /// <returns></returns>
    public static bool IsSimilarTo(this Color c1, Color c2, float tolerance)
    {
        return Mathf.Abs(c1.r - c2.r) < tolerance &&
               Mathf.Abs(c1.g - c2.g) < tolerance &&
               Mathf.Abs(c1.b - c2.b) < tolerance;
    }
}
