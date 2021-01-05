/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Bridge;
using Simulator.Bridge.Data;
using Simulator.Sensors.UI;
using Simulator.Utilities;
using Simulator.Map;

namespace Simulator.Sensors
{
    [SensorType("SignalDataAdhoc", new[] { typeof(SignalDataAdhoc) })]
    public class SignalSensorAdhoc : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        [SensorParameter]
        [Range(1f, 1000f)]
        public float MaxDistance = 100.0f;

        private IBridge Bridge;
        private IWriter<SignalDataAdhoc> Writer;

        private uint SeqId;
        private float NextSend;

        private Dictionary<MapSignal, SignalData> DetectedSignals = new Dictionary<MapSignal, SignalData>();
        private SignalDataAdhoc sData = new SignalDataAdhoc();
        private MapSignal[] Visualized = Array.Empty<MapSignal>();
        private MapManager MapManager;
        public GameObject signalGO;
        private WireframeBoxes WireframeBoxes;

        void Start()
        {
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
            MapManager = SimulatorManager.Instance.MapManager;
            NextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            if (Bridge != null && Bridge.Status == Status.Connected)

            {

                if (Time.time < NextSend)
                {
                    return;
                }
                NextSend = Time.time + 1.0f / Frequency;
                
                GameObject[] signals = GameObject.FindGameObjectsWithTag("TrafficLight");

                foreach (GameObject signal in signals){

                    signalGO = signal;

                    var sigComp = signalGO.GetComponentInChildren<MapLine>();

                    var sigs = sigComp.signals;

                    string state = sigComp.currentState.ToString("F");

                    Writer.Write(new SignalDataAdhoc()
                    {
                        Time = SimulatorManager.Instance.CurrentTime,
                        id = signalGO.name,
                        label = state,
                        xoff = 2,
                        yoff = 2,
                        height = 2,
                        width = 2,
                    });

                }
            }
            Visualized = DetectedSignals.Keys.ToArray();
            DetectedSignals.Clear();
        }

        public uint[] Getbbxparams(Renderer signalMesh)
            {
                Bounds bounds = signalMesh.bounds;
                Vector3 topLeft = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
                Vector3 topRight = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
                Vector3 bottomLeft = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
                uint height =  (uint)topLeft.y - (uint)bottomLeft.y;
                uint width = (uint)topLeft.x - (uint)topRight.x;
                uint[] ret = { (uint)bounds.min.x , (uint)bounds.max.y , height , width };
                return ret;
            }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<SignalDataAdhoc>(Topic);
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (var signal in Visualized)
            {
                Color color;
                switch (signal.CurrentState)
                {
                    case "green":
                        color = Color.green;
                        break;
                    case "yellow":
                        color = Color.yellow;
                        break;
                    case "red":
                        color = Color.red;
                        break;
                    default:
                        color = Color.black;
                        break;
                }

                WireframeBoxes.Draw(signal.gameObject.transform.localToWorldMatrix, signal.boundOffsets, signal.boundScale, color);
            }
        }

        public override void OnVisualizeToggle(bool state) {}
    }
}