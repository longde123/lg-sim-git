/**
 * Copyright (c) 2018 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Map;
using Simulator.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Sensors.UI;

namespace Simulator.Sensors
{
    [SensorType("Novatel", new[] { typeof(novatelInsPvaData), typeof(novatelRawImuXData) })]
    public class NovatelSensor : SensorBase
    {
        // SPAN Manual Table 14
        public enum ImuTypeEnum : byte
        {
            IMU_UNKNOWN = 0,
            IMU_KVH_1750 = 33
        };


        // Sensor Parameters
        [SensorParameter]
        public string TopicInsPVA = "/novatel/inspva";

        [SensorParameter]
        public string TopicRawImuX = "/novatel/rawimux";
        [SensorParameter]
        public string TopicCorrImu = "/novatel/corrimu";

        [SensorParameter]
        public string TopicMarkCount = "/novatel/markcount";

        [SensorParameter]
        public string TopicBestPos = "/novatel/bestpos";
        [SensorParameter]
        public string TopicHeading = "/novatel/heading";
        [SensorParameter]
        public string FrameId = "novatel";

        [SensorParameter]
        [Range(1f, 200f)]
        public float Frequency = 20.0f;

        [SensorParameter]
        public bool IgnoreMapOrigin = false;

        [SensorParameter]
        public ImuTypeEnum ImuType = ImuTypeEnum.IMU_UNKNOWN;

        [SensorParameter]
        public int ImuCelsius = 38;

        [SensorParameter]
        public float EncoderRPMtoPPS = 1000.0f / 60.0f;
        

        // State Variables
        MapOrigin MapOrigin;
        Rigidbody RigidBody;
        VehicleDynamics Dynamics;
        Vector3 prev_velocity;
        byte rawSeqNum = 0;

        // Communications
        IBridge Bridge;
        IWriter<novatelInsPvaData> WriterInsPVA;
        IWriter<novatelRawImuXData> WriterRawImuX;
        IWriter<novatelMarkCountData> WriterMarkCount;
        IWriter<novatelCorrIMUData> WriterCorrIMU;
        IWriter<novatelBestPosData> WriterBestPos;
        IWriter<novatelHeadingData> WriterHeading;
        DateTime lastUpdateTime = DateTime.Now;
        DateTime lastUpdateTimeMarkCount = DateTime.Now;

        private void Awake()
        {
            MapOrigin = MapOrigin.Find();
            RigidBody = GetComponentInParent<Rigidbody>();
            Dynamics = GetComponentInParent<VehicleDynamics>();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            WriterInsPVA = Bridge.AddWriter<novatelInsPvaData>(TopicInsPVA);
            WriterRawImuX = Bridge.AddWriter<novatelRawImuXData>(TopicRawImuX);
            WriterMarkCount = Bridge.AddWriter<novatelMarkCountData>(TopicMarkCount);
            WriterBestPos = Bridge.AddWriter<novatelBestPosData>(TopicBestPos);
            WriterCorrIMU = Bridge.AddWriter<novatelCorrIMUData>(TopicCorrImu);
            WriterHeading =Bridge.AddWriter<novatelHeadingData>(TopicHeading);
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
            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);


            // INSPVA
            WriterInsPVA.Write(new novatelInsPvaData()
            {
                stamp = DateTime.Now,
                frame_id = FrameId,

                week = 0,
                seconds = 0,
                latitude = location.Latitude,
                longitude = location.Longitude,
                height = location.Altitude,
                north_velocity = RigidBody.velocity.z,
                east_velocity = RigidBody.velocity.x,
                up_velocity = RigidBody.velocity.y,
                roll = transform.rotation.eulerAngles.x,
                pitch = transform.rotation.eulerAngles.z,
                azimuth = transform.rotation.eulerAngles.y,
                status = 3
            });


            // BESTPOS
            WriterBestPos.Write(new novatelBestPosData()
            {
                stamp = DateTime.Now,
                frame_id = FrameId,

                lat = location.Latitude,
                lon = location.Longitude,
                hgt = location.Altitude,
                sol_status = 0,
                sol_status_str = "SOL_COMPUTED",
                pos_type = 50,
                pos_type_str = "NARROW_INT",
            });


            // RAWIMUX 
            Vector3 deltaV = prev_velocity - RigidBody.velocity;
            prev_velocity = RigidBody.velocity;
            
            // Gyro
            Vector3 deltaA = RigidBody.angularVelocity;

            // IMU Status
            int imuStatus = 0;

            // Status Sequence Counter
            rawSeqNum++;

            // Device-Specific values
            switch (ImuType)
            {
                case ImuTypeEnum.IMU_KVH_1750:
                    // LSB Scale Factors
                    deltaV /= (float)(0.1 / (3600.0 * 256.0));
                    deltaA /= (float)(0.05 / Math.Pow(2, 15)); 

                    // Gyro & Accel Valid Status
                    imuStatus |= 0x00000077;
                    imuStatus |= (rawSeqNum << 8);
                    imuStatus |= (ImuCelsius << 16);
                    break;

                default:
                    Debug.Log("Unknown Novatel IMU type: " + ImuType);
                    break;
            }


            WriterRawImuX.Write(new novatelRawImuXData()
            {
                stamp = DateTime.Now,
                frame_id = FrameId,

                error = 0,
                type = (byte)ImuType,
                week = 0,
                seconds = 0,
                status = imuStatus,
                z_accel = (int)deltaV.y,
                y_accel_neg = -(int)deltaV.z,
                x_accel = (int)deltaV.x,
                z_gyro = -(int)deltaA.y,
                y_gyro_neg = -(int)deltaA.z,
                x_gyro = (int)deltaA.x,
            });
            WriterCorrIMU.Write(new novatelCorrIMUData()
            {
                stamp = DateTime.Now,
                frame_id = FrameId,

                week = 0,
                seconds = 0,
                pitch_rate = (int)deltaA.x,
                roll_rate = (int)deltaA.z,
                yaw_rate = -(int)deltaA.y, 
                lateral_acc = (int)deltaV.x,
                longitudinal_acc = (int)deltaV.z,
                vertical_acc=(int)deltaV.y,
            });
            WriterHeading.Write(new novatelHeadingData()
            {
                stamp = DateTime.Now,
                frame_id = FrameId,

                sol_status=0,
                sol_status_str="0",
                pos_type=0,
                pos_type_str="0",
                length=0,
                heading=0,
                pitch=0,
                heading_std_dev=0,
                pitch_std_dev=0,
                station_id="0",
                num_svs=0,
                num_sol_in_svs=0,
                num_obs=0,
                num_multi=0,
                sol_source=0,
                ext_sol_stat=0,
                galileo_beidou_sig_mask=0,
                gps_glonass_sig_mask=0,
            });
            // MARK3COUNT        
            if (DateTime.Now > lastUpdateTimeMarkCount)
            {
                // Publish Frequency Limit (1Hz) 
                lastUpdateTimeMarkCount = DateTime.Now.AddSeconds(1.0f);

                // Get Wheel Collider
                WheelCollider RR = Dynamics.axles[1].right;
                
                // Publish Message
                WriterMarkCount.Write(new novatelMarkCountData()
                {
                    stamp = DateTime.Now,
                    frame_id = FrameId,

                    mark_num = 3,
                    period = 1000000,
                    count = (ushort)Math.Round(RR.rpm * EncoderRPMtoPPS),
                });
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            UnityEngine.Debug.Assert(visualizer != null);

            var location = MapOrigin.GetGpsLocation(transform.position, IgnoreMapOrigin);

            var graphData = new Dictionary<string, object>()
            {
                {"Ignore MapOrigin", IgnoreMapOrigin},
                {"Latitude", location.Latitude},
                {"Longitude", location.Longitude},
                {"Altitude", location.Altitude},
                {"Northing", location.Northing},
                {"Easting", location.Easting},
                {"Orientation", transform.rotation}
            };
            visualizer.UpdateGraphValues(graphData);
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
