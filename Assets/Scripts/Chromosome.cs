using UnityEngine;

public class Chromosome
{
    public enum Fitness
    {
        drivingDist,
        accuracy
    }

    public enum MoveableJointsExtent
    {
        fullBody,
        armsTorso
    }

    public enum ClubGrip
    {
        oneHand,
        twoHands
    }

    /*  Each element is a torque that will be applied to the
        corresponding joint every frame.
        This can be made more elaborate later.
        (maybe make this an array of Lists of Vector3 torques,
        with each list corresponding to a joint, and each torque in
        the list gets applied successively at regular intervals?
        Could result in more complex movement) */
    public Vector3[] torques;

    public Fitness fitnessFunc;


    /* Add other fields here later perhaps (golf clubs?) */

    public Chromosome(Vector3[] torques, Fitness fitnessFunc)
    {
        this.torques = torques;
        this.fitnessFunc = fitnessFunc;
    }

}