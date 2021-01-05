/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Utilities;
using UnityEngine;
using Simulator.Sensors.UI;
using System.Collections.Generic;

namespace Simulator.Sensors
{
    [SensorType("Snowball Control", new[] { typeof(SnowballControlData) })]
    public class SnowballControlSensor : SensorBase, IVehicleInputs
    {
        VehicleController Controller;
        VehicleDynamics Dynamics;

        float LastControlUpdate = 0f;

        public float SteerInput { get; private set; } = 0f;
        public float AccelInput { get; private set; } = 0f;
        public float BrakeInput { get; private set; } = 0f;

        public AnimationCurve AccelerationInputCurve;
        public AnimationCurve BrakeInputCurve;

        SnowballControlData controlData;

        H2VechicleAccModel H2VechicleControl_;

        Rigidbody RigidBody;

        private void Awake()
        {
            LastControlUpdate = Time.time;
            Controller = GetComponentInParent<VehicleController>();
            Dynamics = GetComponentInParent<VehicleDynamics>();

            H2VechicleControl_ = new H2VechicleAccModel();
            H2VechicleControl_.Init();

            RigidBody = GetComponentInParent<Rigidbody>();
        }

        private void Update()
        {

        }

        private void FixedUpdate()
        {
            // Control Timeout
            if (Time.time - LastControlUpdate > 0.2f)
            {
                AccelInput = 0.0f;
                SteerInput = 0.0f;
            }
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            bridge.AddReader<SnowballControlData>(Topic, data =>
            {
                // Debug.Log($"SnowballControlData steer = {data.steering_angle} acc: {data.acceleration} received");

                LastControlUpdate = Time.time;
                controlData = data;

                // Steering (%)
                SteerInput = -(float)(data.steering_angle / Dynamics.SteeringRatio) / Dynamics.maxSteeringAngle;
                SteerInput = Mathf.Clamp(SteerInput, -1.0f, 1.0f);

                // H7 ACC Emulator
                // Converts Acceleration Command (m/s^2) to throttle & brake
                // Which gets repacked into a single 'AccelInput' that is consumed by Vehicle Dynamics
                float inputAccel = (float)data.acceleration;
                float currentSpeed = RigidBody.velocity.magnitude; ;

                var throttle = H2VechicleControl_.ConvertToThrottle(currentSpeed, inputAccel, ImuSensor.currentAccel);
                if (inputAccel > 0)
                {
                    AccelInput = throttle;
                }
                else
                {
                    AccelInput = -H2VechicleControl_.ConvertToBrake(inputAccel);
                }
            });
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            var graphData = new Dictionary<string, object>()
            {
                {"Accel In", controlData.acceleration},
                {"Accel Out", AccelInput},
                {"Steer In", controlData.steering_angle},
                {"Steer Out", SteerInput},
                {"Last Update", Time.time - LastControlUpdate},

                {"SteeringRatio", Dynamics.SteeringRatio},
                {"maxSteeringAngle", Dynamics.maxSteeringAngle},
            };

            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {

        }
    }
}
