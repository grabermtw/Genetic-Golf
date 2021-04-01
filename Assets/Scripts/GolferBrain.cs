/* 
    This script is attached to each agent that gets instantiated.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolferBrain : MonoBehaviour
{
    [SerializeField]
    private Rigidbody[] joints; // Think of these as muscles
    [SerializeField]
    private FixedJoint secondHandJoint; // joint for the second hand which will be destroyed if going one-handed
    [SerializeField]
    private GameObject[] golfClubs;
    [SerializeField]
    private Rigidbody golfBall;
    [SerializeField]
    private Transform hole;
    [SerializeField]
    private bool useGravity;

    private Chromosome chrom;
    private GolferSettings settings;
    private Rigidbody[] jointsInUse; // the joints that are actually being used in this sim (may or may not be full body)
    private bool swinging;
    private float actualHoleDist;
    

    // This should be called right after the golfer is instantiated
    public void InitializeAgent(Chromosome newChrom, GolferSettings newSettings, float holeDistOffset = 0)
    {
        chrom = newChrom;
        jointsInUse = new Rigidbody[chrom.torques.Length];
        settings = newSettings;
        // hide the golf hole if we don't need it
        if (settings.fitnessFunc == GolferSettings.Fitness.drivingDist)
        {
            hole.gameObject.SetActive(false);
        }
        else 
        {   // position the hole based on the distance it's supposed to be from the golfer, maintaining the y-coordinate
            hole.position = transform.position - Vector3.right * (settings.holeDist + holeDistOffset) + new Vector3(0, hole.position.y, 0);
            actualHoleDist = Vector3.Distance(transform.position, hole.position);
        }
        
        // Destroy the joint connecting the club to the second hand if we're going one-handed
        if (settings.clubGrip == GolferSettings.ClubGrip.oneHand)
            Destroy(secondHandJoint);
    }

    // Begin the golfing simulation with this agent by allowing it to swing the club
    public void BeginSwinging()
    {
        /*  Prepare joints for movement.
            By default, all joints will be kinematic (meaning physics does not act upon them).
            Here, we set all joints that are added to the joints array to not be kinematic,
            allowing them to move. */
        int jointIndex = 0;
        foreach(Rigidbody joint in joints)
        {
            // unlock every joint if we're doing the full body, and if we're just doing torso and arms,
            // don't unlock the legs
            if (settings.moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ||
                (!joint.gameObject.name.Contains("Leg") && !joint.gameObject.name.Contains("Hips")))
            {
                joint.isKinematic = false;
                joint.useGravity = useGravity;
                if (!joint.gameObject.name.Contains("Hips"))
                    jointsInUse[jointIndex++] = joint;
            }
        }
        StartCoroutine(MoveJoints());
    }

    // Call this to interrupt the MoveJoints coroutine
    public void StopSwinging()
    {
        swinging = false;
    }

    // This coroutine adds torque to each joint.
    /*  could change this later if we have a more complex chromosome
        with multiple torques per joint */
    private IEnumerator MoveJoints()
    {
        swinging = true;
        while (swinging)
        {
            for (int i = 0; i < jointsInUse.Length; i++)
            {
                // multiplying by Time.fixedDeltaTime keeps it framerate-independent
                /*  consider multiplying by the distance to the hole to
                    explore if that makes the agent better able to hit holes at different
                    distances */
                // no consideration of distance to hole
                // joints[i].AddRelativeTorque(chrom.torques[i] * Time.fixedDeltaTime); 
                // consideration of distance to hole (see if this helps when hole position is randomized)
                jointsInUse[i].AddRelativeTorque(chrom.torques[i] * Time.fixedDeltaTime * actualHoleDist);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    /*  Calculates and returns the fitness of this agent
        based on what our chromosome says our fitness function should be */
    public float GetFitness()
    {
        float fitness = 0;
        switch(settings.fitnessFunc)
        {
            case GolferSettings.Fitness.drivingDist:
                // Calculate fitness based on total distance ball has traveled in agent's direction
                float ballDist = Vector3.Distance(golfBall.position, transform.position);
                Vector3 ballDir = golfBall.position - transform.position;
                float ballAngle = Vector3.Angle(ballDir, -transform.right);
                fitness = ballDist * Mathf.Cos(ballAngle);
                break;
            case GolferSettings.Fitness.accuracy:
                // Calculate fitness based on accuracy of hitting it toward the hole
                // make this negative so that more fit agents will always have higher fitness values
                fitness = - Vector3.Distance(golfBall.position, hole.position);
                break;
            default:
                Debug.LogWarning("Unrecognized fitness function");
                break;
        }
        return fitness;
    }

}
