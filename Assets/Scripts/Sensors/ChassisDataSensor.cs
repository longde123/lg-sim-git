/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using UnityEngine;
using System;
using Simulator.Sensors.UI;
using System.Collections.Generic;

namespace Simulator.Sensors
{
    [SensorType("ChassisData", new[] { typeof(ChassisData) })]
    public partial class ChassisDataSensor : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 20.0f;

        uint SendSequence;
        float NextSend;

        bool creep_state = false;
        float temp_vehicle_speed;
        
        IBridge Bridge;
        IWriter<ChassisData> Writer;

        Rigidbody RigidBody;
        VehicleDynamics Dynamics;
        VehicleActions Actions;
        MapOrigin MapOrigin;

        ChassisData msg;

        DateTime lastUpdateTime = DateTime.Now;

        private void Awake()
        {
            RigidBody = GetComponentInParent<Rigidbody>();
            Actions = GetComponentInParent<VehicleActions>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
            MapOrigin = MapOrigin.Find();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = bridge.AddWriter<ChassisData>(Topic);
        }

        public void Start()
        {
            NextSend = Time.time + 1.0f / Frequency;
        }

        void FixedUpdate()
        {
            // Publish Frequency Limit          
            if (lastUpdateTime.AddSeconds(1.0f / Frequency) > DateTime.Now)
            {
                return;
            }
            lastUpdateTime = DateTime.Now;

            // Sanity Check
            if (MapOrigin == null || Bridge == null || Bridge.Status != Status.Connected)
            {
                return;
            }

            // Get Data for Packing
            float speed = RigidBody.velocity.magnitude;
            var gps = MapOrigin.GetGpsLocation(transform.position);
            
            if(creep_state == true)
            {
                creep_state = false;
            }
            else
            {
                temp_vehicle_speed = speed * 3.60f;
            }
        
            // Pack message data
            msg = new ChassisData()
            {
                stamp = DateTime.Now,
                frame_id = "",

                steering_torque = 0.0f,
                engine_rpm = Dynamics.CurrentRPM,
                vehicle_speed = temp_vehicle_speed,
                throttle_percent = Dynamics.AccellInput > 0 ? Dynamics.AccellInput : 0,
                brake_percent = Dynamics.AccellInput < 0 ? -Dynamics.AccellInput : 0,
                steering_angle = -Dynamics.CurrentSteerAngle * Dynamics.SteeringRatio,
                brake_status = false,
                cruise_start = false,
                cruise_cancel = false,
                takeover = false,
            };

            //speed during creep behaviour
            if(msg.brake_percent == 0.0f && msg.vehicle_speed >= 7.92f && msg.throttle_percent == 0.0f)
            {
               creep_state = true;
               msg.vehicle_speed = 2.2f * 3.60f;
               temp_vehicle_speed = msg.vehicle_speed;
            }
            
            // Write Data to Message Queue
            Writer.Write(msg, null); 
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            Debug.Assert(visualizer != null);

            if (msg == null)
            {
                return;
            }

            var graphData = new Dictionary<string, object>()
            {
                {"vehicle_speed (kph)", msg.vehicle_speed},
                {"vehicle_speed (mph)", msg.vehicle_speed * 0.621371f},
                {"steering_angle (deg)", msg.steering_angle},
                {"steering_torque", msg.steering_torque},
                {"engine_rpm", msg.engine_rpm},
                {"throttle_percent", msg.throttle_percent},
                {"brake_percent", msg.brake_percent},
                {"brake_status", msg.brake_status},
                {"takeover", msg.takeover},

             };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            // NOOP
        }
    }
}
