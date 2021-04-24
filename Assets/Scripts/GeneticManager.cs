using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class GeneticManager : MonoBehaviour
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

    private Chromosome[] chroms;
    private GameObject[] agents;
    private float[] fitnesses;
    
    private Chromosome bestChrom; 
    private float bestFitness;
    private float [,] fitnessTrack;
    private GolferSettings settings;

    private const float INIT_TORQUE_MAG = 500; // highest possible magnitude a torque component can have initially
    private const float DIST_BETWEEN_AGENTS = 5; // distance between active agents

    // Called before the first frame update
    void Start()
    {
        // give a predicted time based on the initial values
        CalculateApproximateTime();
    }

    // Calculate approximate expected time to run the simulation.
    // This gets called every time the number of generations or time between generations is updated.
    public void CalculateApproximateTime()
    {
        try
        {
            timePerGen = float.Parse(timePerGenInput.text);
            numGens = int.Parse(numGensInput.text);
            float totalSeconds = numGens * timePerGen;
            int hours = TimeSpan.FromSeconds(totalSeconds).Hours;
            int minutes = TimeSpan.FromSeconds(totalSeconds).Minutes;
            int seconds = TimeSpan.FromSeconds(totalSeconds).Seconds;
            string expectedTime = "";
            if (hours > 0)
                expectedTime += hours + " hours ";
            if (minutes > 0)
                expectedTime += minutes + " minutes ";
            if (seconds > 0)
                expectedTime += seconds + " seconds ";

            approxTime.text = expectedTime;
        }
        catch
        {
            Debug.LogWarning("Time Per Generation and Number of Generations should both be numbers!");
            return;
        }
    }

    // Called when the user clicks the "Begin Simulation" button
    // Processes the input
    public void BeginSimulation()
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
        chroms = new Chromosome[numAgents];
        agents = new GameObject[numAgents];
        fitnesses = new float[numAgents];
        fitnessTrack = new float[numAgents, numGens]; // 2D array with fitness for each gen

        // determine the number of joints based on whether we're using the golfer's full body or not
        int numJoints = (moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ? 12 : 8);

        // initialize chromosomes with random values
        for (int i = 0; i < chroms.Length; i++)
        {
            // create Vector3 array of random torques to give to the chromosome
            Vector3[] initTorques = new Vector3[numJoints];
            for (int j = 0; j < initTorques.Length; j++)
            {
                initTorques[j] = new Vector3(Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                             Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                             Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG));
            }
            chroms[i] = new Chromosome(initTorques);
        }

        // ----------- RUN THE SIMULATION ------------

        for (int i = 0; i < numGens; i++)
        {
            // display the current generation number
            currentGenText.text = "" + (i + 1);

            // Determine the random hole distance offset for this generation
            // (if holeDistRand == 0 then it will just be 0)
            holeDistOffset = Random.Range(-holeDistRand, holeDistRand);

            // Instantiate new agents, give them their respective chromosomes, tell them to start swinging their clubs
            for (int j = 0; j < agents.Length; j++)
            {
                agents[j] = Instantiate(agentPrefab, new Vector3(0, 0, - j * DIST_BETWEEN_AGENTS), Quaternion.identity);
                agents[j].GetComponent<GolferBrain>().InitializeAgent(chroms[j], settings, holeDistOffset);
                agents[j].GetComponent<GolferBrain>().BeginSwinging();
            }

            // Allow this generation to run for the specified time.
            // Execution of this code pauses here until time is up, then automatically resumes.
            yield return new WaitForSeconds(timePerGen);

            // Get the fitnesses of the agents and then clear them out
            for (int j = 0; j < agents.Length; j++)
            {
                fitnesses[j] = agents[j].GetComponent<GolferBrain>().GetFitness();
                Destroy(agents[j]);
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



            //Crossover selection
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
            float crossValue = Random.Range(0.0f, 1.0f);
            if(crossValue < crossoverProb){
                Crossover(chroms[par1idx], chroms[par2idx]);
            }

            //Vladislav Dozorov
            //Handle when mutation occurs
            int candmidx = Random.Range(0, numAgents);
            
            float mutationValue = Random.Range(0.0f, 1.0f);
            if(mutationValue < mutationProb) {
                Mutate(chroms[candmidx]);
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


    private void Crossover(Chromosome parentOne, Chromosome parentTwo)
    {

        int crossPoint = Random.Range(1,3);
        //Debug.Log("Before: " + parentOne.torques[0] + ", " + parentTwo.torques[0]);
        //Debug.Log("Cross point: " + crossPoint);
        for(int i = crossPoint; i < 3; i++){
            float temp = parentOne.torques[0][i];
            parentOne.torques[0][i] = parentTwo.torques[0][i];
            parentTwo.torques[0][i] = temp;
        }
        //Debug.Log("Before: " + parentOne.torques[0] + ", " + parentTwo.torques[0]);
    }


    /*
      Handles mutating a chromosome passed in by adding a randomly generated value
      to the torques of the chromosome
      @author Ernest Essuah Mensah
    */
    private Chromosome Mutate(Chromosome parent)
    {
        // Copy torques from parent to mutate
        Vector3[] torques = new Vector3[parent.torques.Length];

        for (int i = 0; i < parent.torques.Length; i++) {

          Vector3 current = parent.torques[i];

          // Mutate along the three coordinates
          float mutationX = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
          float mutationY = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
          float mutationZ = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);

          torques[i] = new Vector3(current.x + mutationX, current.y + mutationY, current.z + mutationZ);
        }

        return new Chromosome(torques);
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
