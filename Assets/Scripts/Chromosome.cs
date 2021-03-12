using UnityEngine;

public class Chromosome
{
    /*  Each element is a torque (represented as a Vector3)
        that will be applied to the corresponding joint every frame.
        This can be made more elaborate later.
        (maybe make this an array of Lists of Vector3 torques,
        with each list corresponding to a joint, and each torque in
        the list gets applied successively at regular intervals?
        Could result in more complex movement) */
    public Vector3[] torques;

    /* Add other fields here later perhaps (golf clubs?) */
    public Chromosome(Vector3[] torques)
    {
        this.torques = torques;
    }

}