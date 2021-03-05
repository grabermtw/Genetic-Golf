using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolferBrain : MonoBehaviour
{
    public Rigidbody[] joints; // Think of these as muscles
    public GameObject[] golfClubs;
    public bool useGravity;

    private Chromosome chrom;
    private bool swinging;
    

    // Start is called before the first frame update
    void Start()
    {
        // Code for testing muscles, remove later
        Vector3[] testTorques = new Vector3[joints.Length];
        for (int i = 0; i < testTorques.Length; i++)
        {
            testTorques[i] = new Vector3(Random.Range(0,1000),Random.Range(0,1000),Random.Range(0,1000));
        }
        SetChromosome(new Chromosome(testTorques));
        BeginSwinging();
    }

    // This should be called right after the golfer is instantiated
    public void SetChromosome(Chromosome newChrom)
    {
        chrom = newChrom;
    }

    // Returns the number of joints in use
    public int GetNumberOfJoints()
    {
        return joints.Length;
    }

    // Begin the golfing simulation with this agent by allowing it to swing the club
    public void BeginSwinging()
    {
        /*  Prepare joints for movement.
            By default, all joints will be kinematic (meaning physics does not act upon them).
            Here, we set all joints that are added to the joints array to not be kinematic,
            allowing them to move. */
        foreach(Rigidbody joint in joints)
        {
            joint.isKinematic = false;
            joint.useGravity = useGravity;
        }
        StartCoroutine(MoveJoints());
    }

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
            for (int i = 0; i < joints.Length; i++)
            {
                // multiplying by Time.fixedDeltaTime keeps it framerate-independent
                /*  consider multiplying by the distance to the hole to
                    explore if that makes the agent better able to hit holes at different
                    distances */
                joints[i].AddRelativeTorque(chrom.torques[i] * Time.fixedDeltaTime);
            }
            yield return new WaitForFixedUpdate();
        }
    }

}
