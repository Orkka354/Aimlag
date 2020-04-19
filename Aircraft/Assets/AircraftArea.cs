using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using System.Linq;

namespace Aircraft
{

    public class AircraftArea : MonoBehaviour
    {
        [Tooltip("the Path the race will take")]
        public CinemachineSmoothPath racePath;
        public GameObject checkpointprefab;
        public GameObject finishedCheckpointprefab;
        [Tooltip("if true enable training mode")]
        public bool trainingMode;

        public List<AircraftAgent> AircraftAgents { get; private set; }
        public List<GameObject> Checkpoints { get; private set; }
        public AircraftAcademy AircraftAcademy { get; private set; }
        private void Awake()
        {
            //find the agents
            AircraftAgents = transform.GetComponentsInChildren<AircraftAgent>().ToList();
            Debug.Assert(AircraftAgents.Count > 0, "no aircraft agents found");

            AircraftAcademy = FindObjectOfType<AircraftAcademy>();
        }
        private void Start()
        {
            Debug.AssertFormat(racePath != null, "Race path was not set");
            Checkpoints = new List<GameObject>();
            int numchekcpoints = (int)racePath.MaxUnit(CinemachinePathBase.PositionUnits.PathUnits);
            for (int i = 0; i <numchekcpoints; i++)
            {
                //instatiate either chekcpoint or fnish line
                GameObject checkpoint;
                if (i == numchekcpoints - 1) checkpoint =Instantiate<GameObject>(finishedCheckpointprefab);
                else checkpoint = Instantiate<GameObject>(checkpointprefab);

                // set parent postitoin and rotation
                checkpoint.transform.SetParent(racePath.transform);
                checkpoint.transform.localPosition = racePath.m_Waypoints[i].position;
                checkpoint.transform.rotation = racePath.EvaluateOrientationAtUnit(i, CinemachinePathBase.PositionUnits.PathUnits);

                Checkpoints.Add(checkpoint);
            }
        }
        //Resets the postition of the agent using its current next chekcpoint index unless randomize is true it will pick a new random chekcpoint
        public void ResetAgentPostition(AircraftAgent agent, bool randomize = false)
        {
            if (randomize)
            {
                agent.NextCheckpointIndex = Random.Range(0, Checkpoints.Count);
            }

            // Set startpotion to previous checkpoint
            int previousChekcpointIndex = agent.NextCheckpointIndex - 1;
            if (previousChekcpointIndex == -1) previousChekcpointIndex = Checkpoints.Count - 1;

            float startPostition = racePath.FromPathNativeUnits(previousChekcpointIndex, CinemachinePathBase.PositionUnits.PathUnits);

            //convet the position on the race path to a position in 3d space 
            Vector3 basePosition = racePath.EvaluatePosition(startPostition);

            // get orientation at that race path
            Quaternion orientation = racePath.EvaluateOrientation(startPostition);
            Vector3 postionOffset = Vector3.right * (AircraftAgents.IndexOf(agent) - AircraftAgents.Count / 2f) * 10f;

            agent.transform.position = basePosition + orientation * postionOffset;
            agent.transform.rotation = orientation;
        }
    }
    

}
