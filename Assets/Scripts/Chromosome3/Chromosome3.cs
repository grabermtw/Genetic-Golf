using UnityEngine;
using System;

public class Chromosome3
{
    /*  Each element is a list of <float, Vector3, float> tuples, with
        each list corresponding to a particular joint.
        The first float in the tuple is the amount of time that the joint waits for
        before adding the Vector3 as a torque to the joint.
        The second float indicates the duration that the torque should be added for. */
    public Tuple<float, Vector3, float>[][] jointMovements;

    /* Add other fields here later perhaps (golf clubs?) */
    public Chromosome3(Tuple<float, Vector3, float>[][] jointMovements)
    {
        this.jointMovements = jointMovements;
    }

}