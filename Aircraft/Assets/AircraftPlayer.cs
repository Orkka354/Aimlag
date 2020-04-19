using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Aircraft
{

    public class AircraftPlayer : AircraftAgent
    {
        [Header("Input Bindings")]
        public InputAction pitchInput;
        public InputAction yawInput;
        public InputAction boostInput;
        public InputAction pauseInput;

        public override void InitializeAgent()
        {
            base.InitializeAgent();
            pitchInput.Enable();
            yawInput.Enable();
            boostInput.Enable();
            pauseInput.Enable();
        }
        public override float[] Heuristic()
        {
            float pitchValue = Mathf.Round(pitchInput.ReadValue<float>());

            float yawValue = Mathf.Round(yawInput.ReadValue<float>());

            float boostValue = Mathf.Round(boostInput.ReadValue<float>());
            if (pitchValue == -1f) pitchValue = 2f;
            if (yawValue == -1f) yawValue = 2f;
            return new float[] { pitchValue, yawValue, boostValue };
        }
        private void OnDestroy()
        {
            
            pitchInput.Disable();
            yawInput.Disable();
            boostInput.Disable();
            pauseInput.Disable();
        }
    }
}