/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using UnityEngine;

namespace Simulator.Bridge.Data
{
    public class SignalData
    {
        public uint Id;
        public string Label;
        public double Score;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    public class SignalDataArray
    {
        public double Time;
        public string Frame;
        public uint Sequence;
        public SignalData[] Data;
    }

    public class SignalDataAdhoc : SignalData
    {
        public double Time;
        public string id;
        public string Frame;
        public string label;
        public uint xoff;
        public uint yoff;
        public uint height;
        public uint width;

    }
}
