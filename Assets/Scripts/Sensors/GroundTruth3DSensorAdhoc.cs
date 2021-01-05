using System.Numerics;
using System.Data;
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
using Simulator.Utilities;
using Simulator.Sensors.UI;
using Simulator.Map;

namespace Simulator.Sensors
{
    [SensorType("3D Ground Truth Adhoc", new[] { typeof(Detected3DObjectDataAdhoc) })]
    public class GroundTruth3DSensorAdhoc : SensorBase
    {
        [SensorParameter]
        [Range(1f, 100f)]
        public float Frequency = 10.0f;

        [SensorParameter]
        [Range(1f, 1000f)]
        public float MaxDistance = 100.0f;

        public RangeTrigger rangeTrigger;

        WireframeBoxes WireframeBoxes;

        private uint seqId;
        private uint objId;
        private float nextSend;

        private IBridge Bridge;
        private IWriter<Detected3DObjectDataAdhoc> Writer;

        private Dictionary<Collider, Detected3DObjectAdhoc> Detected = new Dictionary<Collider, Detected3DObjectAdhoc>();
        private Collider[] Visualized = Array.Empty<Collider>();
        MapOrigin MapOrigin;

        void Start()
        {
            MapOrigin = MapOrigin.Find();
            WireframeBoxes = SimulatorManager.Instance.WireframeBoxes;
            rangeTrigger.SetCallbacks(WhileInRange);
            rangeTrigger.transform.localScale = MaxDistance * UnityEngine.Vector3.one;
            nextSend = Time.time + 1.0f / Frequency;
        }

        void Update()
        {
            if (Bridge != null && Bridge.Status == Status.Connected)
            {
                if (Time.time < nextSend)
                {
                    return;
                }
                nextSend = Time.time + 1.0f / Frequency;

                Writer.Write(new Detected3DObjectDataAdhoc()
                {
                    Name = Name,
                    Frame = Frame,
                    Time = SimulatorManager.Instance.CurrentTime,
                    Sequence = seqId++,
                    objects = Detected.Values.ToArray(),
                });
            }

            Visualized = Detected.Keys.ToArray();
            Detected.Clear();
        }

        public override void OnBridgeSetup(IBridge bridge)
        {
            Bridge = bridge;
            Writer = Bridge.AddWriter<Detected3DObjectDataAdhoc>(Topic);
        }

        void WhileInRange(Collider other)
        {
            GameObject egoGO = transform.parent.gameObject;
            GameObject parent = other.transform.parent.gameObject;
            if (parent == egoGO)
            {
                return;
            }

            if (!(other.gameObject.layer == LayerMask.NameToLayer("GroundTruth")) || !parent.activeInHierarchy)
            {
                return;
            }

            if (!Detected.ContainsKey(other))
            {
                uint id;
                string label;
                float linear_vel;
                float angular_vel;
                if (parent.layer == LayerMask.NameToLayer("NPC"))
                {
                    var npcC = parent.GetComponent<NPCController>();
                    id = npcC.GTID;
                    label = npcC.NPCType;
                    linear_vel = UnityEngine.Vector3.Dot(npcC.GetVelocity(), parent.transform.forward);
                    angular_vel = -npcC.GetAngularVelocity().y;
                }
                else
                {
                    return;
                }

                //Bounding Box Size
                UnityEngine.Vector3 size = ((BoxCollider)other).size;
                if (size.magnitude == 0)
                {
                    return;
                }
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                size.Set(size.z, size.x, size.y);

                //Bounding Box Orientation
                //var Rot = Quaternion.Euler(parent.transform.rotation.eulerAngles);
                var rot_euler = parent.transform.rotation.eulerAngles;
                //90 degree phase shift to match with localization
                rot_euler.y = 90 - rot_euler.y;
                var Rot = UnityEngine.Quaternion.Euler(rot_euler);

                //Bounding Box Position
                if(MapOrigin == null)
                {
                    return;
                }
                UnityEngine.Vector3 bounding_box_center = parent.transform.TransformPoint(((BoxCollider)other).center);                
                var location = MapOrigin.GetGpsLocation(bounding_box_center/*parent.transform.position*/);

                Detected.Add(other, new Detected3DObjectAdhoc()
                {
                    Id = id,
                    Label = label,
                    Score = 1.0f,
                    easting = location.Easting,
                    northing = location.Northing,
                    Rotation = Rot,
                    Scale = size,
                    LinearVelocity = new UnityEngine.Vector3(linear_vel, 0, 0),  // Linear velocity in forward direction of objects, in meters/sec
                    AngularVelocity = new UnityEngine.Vector3(0, 0, angular_vel),  // Angular velocity around up axis of objects, in radians/sec
                });
            }
        }

        public override void OnVisualize(Visualizer visualizer)
        {
            foreach (var other in Visualized)
            {
                if (other.gameObject.activeInHierarchy)
                {
                    GameObject parent = other.gameObject.transform.parent.gameObject;
                    Color color = Color.green;
                    if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
                    {
                        color = Color.yellow;
                    }

                    BoxCollider box = other as BoxCollider;
                    WireframeBoxes.Draw(box.transform.localToWorldMatrix, new UnityEngine.Vector3(0f, box.bounds.extents.y, 0f), box.size, color);
                }
            }
        }

        public override void OnVisualizeToggle(bool state)
        {
            //
        }
    }
}
