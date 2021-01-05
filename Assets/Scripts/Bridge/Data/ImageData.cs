/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */
using System.Collections.Generic;
using System;
using UnityEngine;
namespace Simulator.Bridge.Data
{
    // this is always Jpeg compressed image
    public class ImageData
    {
        public string Name;
        public string Frame;
        public double Time;
        public uint Sequence;

        public int Width;
        public int Height;

        public byte[] Bytes;
        public int Length;
    }

    public class StaticTFData
    {
        public double Time;
        public string frame_id;

        public string child_frame_id;

        public Vector3 translation;
        public Quaternion rotation;
    }

     public class CameraInfoData
    {
        public string frame_id;
        public double Time;
        public uint Sequence;

        public int Width;
        public int Height;

        public double[] d; // 5 parameter

        public double[] k; // 3x3 row-major matrix

        public double[] r; // 3x3 row-major matrix

        public double[] p; // 3x4

    }
}
