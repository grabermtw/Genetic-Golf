using UnityEngine;
using System;

public class ComplexChromosome
{
    /*  Each element is a list of <float, Vector3> tuples, with
        each list corresponding to a particular joint.
        The float in the tuple is the amount of time that the joint waits for
        before adding the Vector3 as an impulse torque to the joint */
    public Tuple<float, Vector3>[][] jointMovements;

    /* Add other fields here later perhaps (golf clubs?) */
    public ComplexChromosome(Tuple<float, Vector3>[][] jointMovements)
    {
        this.jointMovements = jointMovements;
    }

}