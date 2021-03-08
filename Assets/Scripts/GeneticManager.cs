using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GeneticManager : MonoBehaviour
{
    // References to menu input stuff
    [SerializeField]
    private TMP_Dropdown fitnessDropdown;
    [SerializeField]
    private TMP_Dropdown gripDropdown;
    [SerializeField]
    private TMP_Dropdown jointsDropdown;
    [SerializeField]
    private TMP_InputField numAgentsInput;

    private int numAgents; // number of agents (golfers) in each generation
    private GolferSettings.Fitness fitnessFunc;
    private GolferSettings.MoveableJointsExtent moveableJoints;
    private GolferSettings.ClubGrip clubGrip;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BeginSimulation()
    {
        // Get number of agents from the input field
        try
        {
            numAgents = int.Parse(numAgentsInput.text);
        }
        catch
        {
            Debug.LogWarning("The number of agents must be a number!");
            return;
        }
        // Get the values from the dropdown options
        fitnessFunc = (GolferSettings.Fitness) fitnessDropdown.value;
        moveableJoints = (GolferSettings.MoveableJointsExtent) jointsDropdown.value;
        clubGrip = (GolferSettings.ClubGrip) gripDropdown.value;

        
        
    }
}
