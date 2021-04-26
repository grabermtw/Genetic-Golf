using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class ComplexGeneticManager : MonoBehaviour
{
    [SerializeField]
    private GameObject agentPrefab;

    // References to menu input stuff
    [SerializeField]
    private GameObject menuUIParent;
    [SerializeField]
    private TMP_Dropdown fitnessDropdown;
    [SerializeField]
    private TMP_Dropdown gripDropdown;
    [SerializeField]
    private TMP_Dropdown jointsDropdown;
    [SerializeField]
    private TMP_InputField numAgentsInput;
    [SerializeField]
    private TMP_InputField holeDistInput;
    [SerializeField]
    private TMP_InputField holeDistRandInput;
    [SerializeField]
    private TMP_InputField timePerGenInput;
    [SerializeField]
    private TMP_InputField crossoverProbInput;
    [SerializeField]
    private TMP_InputField mutationProbInput;
    [SerializeField]
    private TMP_InputField numGensInput;
    [SerializeField]
    private TextMeshProUGUI approxTime;

    [SerializeField]
    private GameObject simulationUI; // UI appearing in the top corner while running the simulation
    [SerializeField]
    private TextMeshProUGUI currentGenText; // displays the current generation number

    private GolferSettings.Fitness fitnessFunc;
    private GolferSettings.MoveableJointsExtent moveableJoints;
    private GolferSettings.ClubGrip clubGrip;
    private int numAgents; // number of agents (golfers) in each generation
    private float holeDist; // distance to the hole
    private float holeDistRand; // range of offset that should be applied to the hole distance each generation
    private float timePerGen; // amount of time dedicated to each generation before the golfers are cleared out and a new gen is created
    private float crossoverProb;
    private float mutationProb;
    private int numGens;

    private ComplexChromosome[] chroms;
    private GameObject[] agents;
    private float[] fitnesses;
    
    private ComplexChromosome bestChrom; 
    private float bestFitness;
    private float [,] fitnessTrack;
    private GolferSettings settings;

    private const float INIT_TORQUE_MAG = 1000; // highest possible magnitude a torque component can have initially
    private const float DIST_BETWEEN_AGENTS = 5; // distance between active agents
    private const int COMPLEX_CHROMOSOME_LENGTH = 30; // length of each array in the ComplexChromosome structure
    private const float MAX_TIME_BETWEEN_TORQUES = 1.5f; // maximum time between two torques
    private const int ELITISM = 4; // how many of the most fit individuals should be preserved.
                                    // please keep this to an even number
    private const int NUM_SWINGS_PER_GEN = 3; // how many swings each agent should try in each generation


    // Called when the user clicks the "Begin Simulation" button
    // Processes the input
    public void BeginComplexSimulation()
    {
        // Get numbers from the input fields
        try
        {
            numAgents = int.Parse(numAgentsInput.text);
            holeDist = float.Parse(holeDistInput.text);
            holeDistRand = float.Parse(holeDistRandInput.text);
            timePerGen = float.Parse(timePerGenInput.text);
            crossoverProb = float.Parse(crossoverProbInput.text);
            mutationProb = float.Parse(mutationProbInput.text);
            numGens = int.Parse(numGensInput.text);
            if (mutationProb < 0 || mutationProb > 1 || crossoverProb < 0 || crossoverProb > 1)
            {
                Debug.LogWarning("Crossover and mutation values should be between 0 and 1!");
                return;
            }
            if (numAgents < 0 || holeDist < 0 || holeDistRand < 0 || timePerGen < 0 || numGens < 0)
            {
                Debug.LogWarning("None of these numbers should be negative!");
                return;
            }
        }
        catch
        {
            Debug.LogWarning("Only numbers are allowed in the input fields!");
            return;
        }
        // Get the values from the dropdown options
        fitnessFunc = (GolferSettings.Fitness) fitnessDropdown.value;
        moveableJoints = (GolferSettings.MoveableJointsExtent) jointsDropdown.value;
        clubGrip = (GolferSettings.ClubGrip) gripDropdown.value;

        // Create the GolferSettings object
        settings = new GolferSettings(fitnessFunc, moveableJoints, clubGrip, holeDist);

        // hide the main menu UI
        menuUIParent.SetActive(false);
        // display the simulation UI
        simulationUI.SetActive(true);
        // Begin the simulation coroutine
        StartCoroutine(Simulate());
    }



    // --------------- ACTUAL GENETIC ALGORITHM STUFF BELOW ----------------



    // Here is where the actual simulations are managed
    private IEnumerator Simulate()
    {

        // ----------- CREATE INITIAL POPULATION, SET UP SIMULATION ------------

        float holeDistOffset;

        // Initialize arrays
        chroms = new ComplexChromosome[numAgents];
        agents = new GameObject[numAgents];
        fitnesses = new float[numAgents];
        fitnessTrack = new float[numAgents, numGens]; // 2D array with fitness for each gen

        // determine the number of joints based on whether we're using the golfer's full body or not
        int numJoints = (moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ? 12 : 8);

        // initialize chromosomes with random values
        for (int i = 0; i < chroms.Length; i++)
        {
            // create Vector3 array of random torques to give to the chromosome
            Tuple<float, Vector3>[][] initJointMovements = new Tuple<float, Vector3>[numJoints][];
            for (int j = 0; j < initJointMovements.Length; j++)
            {
                initJointMovements[j] = new Tuple<float, Vector3>[COMPLEX_CHROMOSOME_LENGTH];
                for (int k = 0; k < COMPLEX_CHROMOSOME_LENGTH; k++)
                {
                    initJointMovements[j][k] = new Tuple<float, Vector3>(Random.Range(0f, MAX_TIME_BETWEEN_TORQUES),
                                                                         new Vector3(Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                                                                     Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                                                                     Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG)));
                }
            }
            chroms[i] = new ComplexChromosome(initJointMovements);
        }

        // ----------- RUN THE SIMULATION ------------

        for (int i = 0; i < numGens; i++)
        {
            // display the current generation number
            currentGenText.text = "" + (i + 1);

            // Determine the random hole distance offset for this generation
            // (if holeDistRand == 0 then it will just be 0)
            holeDistOffset = Random.Range(-holeDistRand, holeDistRand);

            
            // have each agent do several swings and average the fitnesses of those swings
            // clear out fitnesses array
            fitnesses = new float[numAgents];
            for (int swingNum = 0; swingNum < NUM_SWINGS_PER_GEN; swingNum++)
            {
                // Instantiate new agents, give them their respective chromosomes, tell them to start swinging their clubs
                for (int j = 0; j < agents.Length; j++)
                {
                    agents[j] = Instantiate(agentPrefab, new Vector3(0, 0, - j * DIST_BETWEEN_AGENTS), Quaternion.identity);
                    agents[j].GetComponent<ComplexGolferBrain>().InitializeAgent(chroms[j], settings, holeDistOffset);
                    agents[j].GetComponent<ComplexGolferBrain>().BeginSwinging();
                }

                // Allow this generation to run for the specified time.
                // Execution of this code pauses here until time is up, then automatically resumes.
                yield return new WaitForSeconds(timePerGen);

                // Get the fitnesses of the agents and then clear them out
                for (int j = 0; j < agents.Length; j++)
                {
                    fitnesses[j] += agents[j].GetComponent<ComplexGolferBrain>().GetFitness();
                    Destroy(agents[j]);
                }
            }
            // complete the calculation of the average fitnesses over those three swings for the gen
            for (int j = 0; j < fitnesses.Length; j++)
            {
                fitnesses[j] = fitnesses[j] / NUM_SWINGS_PER_GEN;
            }
            
            // Track fitness
            // Initialize best chrom & fit as the first one
            if (i == 0) {
                bestChrom = chroms[0];
                bestFitness = fitnesses[0];
            }

            // Add fitnesses to 2d fitnessTrack array
            // Assign new best chrom if there is one
            for (int j = 0; j < agents.Length; j++)
            {   
                fitnessTrack[j,i] = fitnesses[j]; 

                if (fitnessTrack[j,i] > bestFitness) { 
                    bestFitness = fitnessTrack[j,i];
                    bestChrom = chroms[j];
                }
            }

            // create new chromosome array
            ComplexChromosome[] newChroms = new ComplexChromosome[chroms.Length];

            /* find most fit individuals
                by sorting the fitnesses array in descending order,
                then taking the first however many biggest fitnesses
                and putting their respective chromosomes in the new chromosome array
                */
            float[] sortedFitnesses = fitnesses;
            Array.Sort(sortedFitnesses);
            Array.Reverse(sortedFitnesses);
            for (int j = 0; j < ELITISM; j++)
            {
                try {
                    newChroms[i] = chroms[Array.FindIndex(fitnesses, x => x == sortedFitnesses[j])];
                }
                catch {
                    Debug.LogWarning("somethting weird happened");
                }
            }

            // Crossover selection
            for (int pair = ELITISM; pair < chroms.Length; pair += 2)
            {
                //Tournament selection with 2 candidates for each parent
                int cand1idx = Random.Range(0, numAgents);
                int cand2idx = cand1idx;
                while(cand2idx == cand1idx){
                    cand2idx = Random.Range(0, numAgents);
                }
                int par1idx;
                int par2idx;
                if(fitnesses[cand1idx] > fitnesses[cand2idx]){
                    par1idx = cand1idx;
                    par2idx = cand2idx;
                } else {
                    par2idx = cand1idx;
                    par1idx = cand2idx;
                }

                // determine if we crossover
                float crossValue = Random.Range(0.0f, 1.0f);
                if (crossValue < crossoverProb){
                    // crossover
                    Tuple<ComplexChromosome, ComplexChromosome> xresult = Crossover(chroms[par1idx], chroms[par2idx]);
                    newChroms[pair] = xresult.Item1;
                    newChroms[pair + 1] = xresult.Item2;
                }
                else
                {   // don't crossover, just send the parents on to the new array
                    newChroms[pair] = chroms[par1idx];
                    newChroms[pair + 1] = chroms[par2idx];
                }
            }

            // Mutation selection
            for (int j = ELITISM; j < newChroms.Length; j++)
            {
                float mutateValue = Random.Range(0.0f, 1.0f);
                if (mutateValue <= mutationProb)
                {
                    newChroms[j] = Mutate(newChroms[j]);
                }
            }

            /* ---------- TODO: HANDLE CHROMOSOMES ----------


                This is where we should do the crossover/mutation stuff.
                Need to fill out Crossover() and Mutate() methods.


                Vlad
                Need to handle deciding when mutation occurs
                and for which chromosomes.
                Use mutationProb for probabilities
                Then just need to fill out the chroms array again with the new chromosomes.





                Azhdaha
                Also, we should keep track of fitnessess across generations so that we can
                call ExportCSV() at the end.
                We should also keep track of the most fit chromosome.
                We can just save this in a variable for now, later on we can display
                this info about it more usefully.
            */


        }
        yield break;
    }


    private Tuple<ComplexChromosome, ComplexChromosome> Crossover(ComplexChromosome parentOne, ComplexChromosome parentTwo)
    {
        int jointsLength = parentOne.jointMovements.Length;
        int crossPoint = Random.Range(1,jointsLength);
        for(int i = crossPoint; i < jointsLength; i++){
            Tuple<float, Vector3>[] temp = parentOne.jointMovements[i];
            parentOne.jointMovements[i] = parentTwo.jointMovements[i];
            parentTwo.jointMovements[i] = temp;
        }

        return new Tuple<ComplexChromosome, ComplexChromosome>(parentOne, parentTwo);
    }


    /*
      Handles mutating a chromosome passed in by adding a randomly generated value
      to the torques of the chromosome
      @author Ernest Essuah Mensah
    */
    private ComplexChromosome Mutate(ComplexChromosome parent)
    {
        // Copy torques from parent to mutate
        Tuple<float, Vector3>[][] jointMovements = parent.jointMovements;

        int mutationIndex = Random.Range(0, parent.jointMovements.Length);

        for (int i = 0; i < COMPLEX_CHROMOSOME_LENGTH; i++)
        {
            Tuple<float, Vector3> current = parent.jointMovements[mutationIndex][i];

            // Mutate along the three coordinates
            float mutationX = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            float mutationY = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            float mutationZ = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            
            Vector3 newMovement = new Vector3(current.Item2.x + mutationX, current.Item2.y + mutationY, current.Item2.z + mutationZ);

            // Mutate the time between joint movements
            float mutationTime = Random.Range(-MAX_TIME_BETWEEN_TORQUES, MAX_TIME_BETWEEN_TORQUES);

            float newTime = Mathf.Max(current.Item1 + mutationTime, 0);

            jointMovements[mutationIndex][i] = new Tuple<float, Vector3>(newTime, newMovement);
        }     

        return new ComplexChromosome(jointMovements);
    }



    public void ExportCSV()
    {
        // TODO - John
        /*  Export a CSV showing best fitness and avg fitness for each generation.
            Maybe include info about the parameters used for this run as well
            either at the beginning or the end, such as the fields in GolfSettings
            as well as the timePerGen, mutationProb, crossoverProb, holeDist, holeDistRand etc.
        */
    }

}
