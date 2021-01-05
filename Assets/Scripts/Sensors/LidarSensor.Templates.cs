/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System.Collections.Generic;
namespace Simulator.Sensors
{
    public partial class LidarSensor
    {
        public struct Template
        {
            public string Name;
            public int LaserCount;
            public float MinDistance;
            public float MaxDistance;
            public float RotationFrequency;
            public int MeasurementsPerRotation;
            public float FieldOfView;
            public List<float> VerticalRayAngles;
            public float CenterAngle;

            public static readonly Template[] Templates =
            {
                new Template()
                {
                    Name = "Custom",
                },

                new Template()
                {
                    Name = "Lidar16",
                    LaserCount = 16,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1000, // 900 .. 3600
                    FieldOfView = 30.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 0.0f,
                },

                new Template()
                {
                    Name = "Lidar16b",
                    LaserCount = 16,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 20.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 0.0f,
                },

                new Template()
                {
                    Name = "Lidar32",
                    LaserCount = 32,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 41.33f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 10.0f,
                },

                //new LidarTemplate()
                //{
                //    Name = "Lidar32b",
                //    RayCount = 32,
                //    MinDistance = 0.5f,
                //    MaxDistance = 200.0f,
                //    RotationFrequency = 10, // 5 .. 20
                //    MeasurementsPerRotation = 2000, // 900 .. 3600
                //    FieldOfView = 40.0f,
                //    CenterAngle = 5.0f,
                //},
                new Template()
                {
                    Name = "Lidar32-NonUniform",
                    LaserCount = 32,
                    MinDistance = 0.5f,
                    MaxDistance = 100.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 1500, // 900 .. 3600
                    FieldOfView = 41.33f,
                    VerticalRayAngles = new List<float> {
                        -25.0f,   -1.0f,    -1.667f,  -15.639f,
                        -11.31f,   0.0f,    -0.667f,   -8.843f,
                         -7.254f,  0.333f,  -0.333f,   -6.148f,
                         -5.333f,  1.333f,   0.667f,   -4.0f,
                         -4.667f,  1.667f,   1.0f,     -3.667f,
                         -3.333f,  3.333f,   2.333f,   -2.667f,
                         -3.0f,    7.0f,     4.667f,   -2.333f,
                         -2.0f,   15.0f,    10.333f,   -1.333f
                        },
                    CenterAngle = 10.0f,
                },

                new Template()
                {
                    Name = "Lidar64",
                    LaserCount = 64,
                    MinDistance = 0.5f,
                    MaxDistance = 120.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 2083, // 1028 .. 4500
                    FieldOfView = 26.9f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 11.45f,
                },

                new Template()
                {
                    Name = "Lidar64-NonUniform",
                    LaserCount = 64,
                    MinDistance = 0.5f,
                    MaxDistance = 120.0f,
                    RotationFrequency = 10, // 5 .. 20
                    MeasurementsPerRotation = 2083, // 1028 .. 4500
                    FieldOfView = 26.9f,
                    VerticalRayAngles = new List<float> {
                         2.0f, 1.661f, 1.323f, 0.984f, 
                         0.645f, 0.306f, -0.032f, -0.371f, 
                        -0.710f, -1.048f, -1.387f, -1.726f, 
                        -2.065f, -2.403f, -2.742f, -3.081f,
                        -3.419f, -3.758f, -4.097f, -4.435f,
                        -4.774f, -5.113f, -5.452f, -5.790f,
                        -6.129f, -6.468f, -6.806f, -7.145f,
                        -7.484f, -7.823f, -8.161f, -8.5f,
                        -8.9f, -9.416f, -9.932f, -10.448f,
                        -10.965f, -11.481f, -11.997f, -12.513f,
                        -13.029f, -13.545f, -14.061f, -14.577f,
                        -15.094f, -15.610f, -16.126f, -16.642f,
                        -17.158f, -17.674f, -18.190f, -18.706f,
                        -19.223f, -19.739f, -20.255f, -20.771f,
                        -21.287f, -21.803f, -22.319f, -22.835f,
                        -23.352f, -23.868f, -24.384f, -24.9f
                    },
                    CenterAngle = 11.45f,
                },

                new Template()
                {
                    Name = "Lidar128",
                    LaserCount = 128,
                    MinDistance = 0.5f,
                    MaxDistance = 300.0f,
                    RotationFrequency = 10,
                    MeasurementsPerRotation = 3272,
                    FieldOfView = 40.0f,
                    VerticalRayAngles = new List<float> { },
                    CenterAngle = 5.0f,
                },
            };
        }
    }
}
