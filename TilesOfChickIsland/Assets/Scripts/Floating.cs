using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Floating : MonoBehaviour
{
    [SerializeField] private AnimationCurve curve;

    /// <summary>
    /// Called once per frame.
    /// </summary>
    private void Update()
    {
        // make this object float by following the animation curve
        transform.position = new Vector3(transform.position.x, curve.Evaluate(Time.time % 1), transform.position.z);
    }
}
