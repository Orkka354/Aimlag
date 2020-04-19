using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System;

namespace Aircraft
{

    public class AircraftAgent : Agent
    {
        [Header("Movement Parameters")]
        public float thrust = 10000f;
        public float pitchSpeed = 100f;
        public float yawSpeed = 100f;
        public float rollSpeed = 100f;
        public float boostMultiplier = 2f;

        [Header("Explosion stuff")]
        [Tooltip(" The aircraft mesh that will disappear on explosion")]
        public GameObject meshObject;
        [Tooltip("The game object of teh explostion partilce effect")]
        public GameObject explosioneffect;
        [Header("training")]
        [Tooltip("Number of steps to timeout after")]
        public int stepTimeout = 300;
        public int NextCheckpointIndex { get; set; }

        // components to keep track of 
        private AircraftArea area;
        new private Rigidbody rigidbody;
        private TrailRenderer trail;
        private RayPerception3D rayperception;

        //
        private float nextStepTimeout;
        private bool frozen = false;

        // Controls
        private float pitchChange = 0f;
        private float smoothpitchChange = 0f;
        private float maxPitchAngle = 45f;
        private float yawChange = 0f;
        private float smoothYawChange = 0f;
        private float rollChange = 0f;
        private float smoothRollChange = 0f;
        private float maxRollAngle = 45f;
        private bool boost;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            area = GetComponentInParent<AircraftArea>();
            rigidbody = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();

            rayperception = GetComponent<RayPerception3D>();
            //Overide max step set in inspector
            //max 5000 if training infinate if not
            agentParameters.maxStep = area.trainingMode ? 5000 : 0;

        }
        /// <summary>
        /// Read actions from vector action
        /// </summary>
        /// <param name="vectorAction"></param>
        /// <param name="textAction"></param>
        public override void AgentAction(float[] vectorAction, string textAction)
        {
            // Read value for pitch and yaw
            pitchChange = vectorAction[0];
            if (pitchChange == 2) pitchChange = -1f;
            yawChange = vectorAction[1];
            if (yawChange == 2) yawChange = -1f;

            //enable boost and trail
            boost = vectorAction[2] == 1;
            if (boost && !trail.emitting) trail.Clear();
            trail.emitting = boost;

            if (frozen) return;

            ProcessMovment();

            if (area.trainingMode)
            {
                AddReward(-1f / agentParameters.maxStep);

                // make sure not out of time
                if(GetStepCount() > nextStepTimeout)
                {
                    AddReward(-.5f);
                    Done();
                    

                }

                Vector3 localCheckpointDir = VectorToNextCheckpoint();
                if (localCheckpointDir.magnitude < area.AircraftAcademy.resetParameters["checkpoint_radius"])
                {
                    GotCheckpoint();
                }
            }
        }

        public override void CollectObservations()
        {
            AddVectorObs(transform.InverseTransformDirection(rigidbody.velocity));

            AddVectorObs(VectorToNextCheckpoint());

            Vector3 nextChekpointForward = area.Checkpoints[NextCheckpointIndex].transform.forward;
            AddVectorObs(transform.InverseTransformDirection(nextChekpointForward));

            //oberverse ray perception results
            string[] detectableObjects = { "Untagged", "checkpoint" };
            //2tags + hit/not + 1 distance to obejct 
            AddVectorObs(rayperception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f, 90f, 120f }, detectableObjects: detectableObjects, startOffset: 0f, endOffset: 75f));

            //2tags + hit/not + 1 distance to obejct 
            AddVectorObs(rayperception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f,70f, 80f, 90f, 100f, 110f, 120f }, detectableObjects: detectableObjects, startOffset: 0f, endOffset: 0f));

            //2tags + hit/not + 1 distance to obejct 
            AddVectorObs(rayperception.Perceive(rayDistance: 250f, rayAngles: new float[] { 60f, 90f, 120f }, detectableObjects: detectableObjects, startOffset: 0f, endOffset: -75f)); 

        }
        public override void AgentReset()
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            trail.emitting = false;
            area.ResetAgentPostition(agent:this, randomize: area.trainingMode);

            // update the step timeout if training
            if (area.trainingMode) nextStepTimeout = GetStepCount() + stepTimeout;
        }
        public void FreezeAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/thaw not supported in training");
            frozen = true;
            rigidbody.Sleep();
            trail.emitting = false;
        }

        public void ThawAgent()
        {
            Debug.Assert(area.trainingMode == false, "Freeze/thaw not supported in training");
            frozen = false;
            rigidbody.WakeUp();
        }
        //callled when an agent flys thru right chekcpoint
        private void GotCheckpoint()
        {
            // next chekcpoint reached
            NextCheckpointIndex = (NextCheckpointIndex + 1) % area.Checkpoints.Count;

            if (area.trainingMode)
            {
                AddReward(.5f);
                nextStepTimeout = GetStepCount() + stepTimeout;
            }
        }

        //gets vector for agent to flythru
        private Vector3 VectorToNextCheckpoint()
        {
            Vector3 nextCheckpointDir = area.Checkpoints[NextCheckpointIndex].transform.position - transform.position;
            Vector3 localCheckpointDir = transform.InverseTransformDirection(nextCheckpointDir);
            return localCheckpointDir;
        }

        private void ProcessMovment()
        {
            //calculate boost
            float boostModifier = boost ? boostMultiplier : 1f;
            //apply forward
            rigidbody.AddForce(transform.forward * thrust * boostModifier, ForceMode.Force);
            //get the current rotation
            Vector3 curRot = transform.rotation.eulerAngles;
            //calculate the roll angle
            float rollAngle = curRot.z > 180f ? curRot.z - 360f : curRot.z;
            if(yawChange == 0f)
            {
                rollChange = -rollAngle / maxRollAngle;
            }
            else
            {
                rollChange = -yawChange;
            }

            //Calculate smooth deltas
            smoothpitchChange = Mathf.MoveTowards(smoothpitchChange, pitchChange, 2f * Time.fixedDeltaTime);
            smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * 2f * Time.fixedDeltaTime);
            smoothRollChange = Mathf.MoveTowards(smoothRollChange, rollChange, 2f * Time.fixedDeltaTime);

            //
            float pitch = ClampAngle(curRot.x + smoothpitchChange * Time.fixedDeltaTime * pitchSpeed,
                -maxPitchAngle,
                maxPitchAngle);
            float yaw = curRot.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;
            float roll = ClampAngle(curRot.z + smoothRollChange * Time.fixedDeltaTime * rollSpeed,
                -maxRollAngle,
                maxRollAngle);

            transform.rotation = Quaternion.Euler(pitch, yaw, roll);
        }
        private static float ClampAngle (float angle, float from, float to)
        {
            if (angle < 0f) angle = 360f + angle;
            if (angle > 180f) return Mathf.Max(angle, 360f + from);
            return Mathf.Min(angle, to);
        }
        private void OnTriggerEnter(Collider other)
        {
            if(other.transform.CompareTag("checkpoint") && other.gameObject == area.Checkpoints[NextCheckpointIndex])
            {
                GotCheckpoint();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.transform.CompareTag("agent"))
            {
                //hit something reset
                if (area.trainingMode)
                {
                    AddReward(-1f);
                    Done();
                    return;
                }

            }
            else
            {
                StartCoroutine(ExplosionReset());

            }
        }

        private IEnumerator ExplosionReset()
        {
            FreezeAgent();
            meshObject.SetActive(false);
            //explosioneffect.SetActive(true);
            yield return new WaitForSeconds(2f);

            meshObject.SetActive(true);
           // explosioneffect.SetActive(false);
            area.ResetAgentPostition(agent: this);
            yield return new WaitForSeconds(1f);

            ThawAgent();
        }
    }

}