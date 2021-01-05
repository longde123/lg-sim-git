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
    [SensorType("3D Ground Truth Perc", new[] { typeof(Detected3DObjectData) })]
    public class GroundTruth3DSensorPerc : SensorBase
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
        private IWriter<Detected3DObjectDataPerc> Writer;

        private Dictionary<Collider, Detected3DObjectPerc> Detected = new Dictionary<Collider, Detected3DObjectPerc>();
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

                Writer.Write(new Detected3DObjectDataPerc()
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
            Writer = Bridge.AddWriter<Detected3DObjectDataPerc>(Topic);
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
                string label = null;
                float linear_vel;
                float angular_vel;
                float egoPosY = egoGO.GetComponent<VehicleActions>().Bounds.center.y;
                float score = 0.0f;
                if (parent.layer == LayerMask.NameToLayer("Agent"))
                {
                    var egoC = parent.GetComponent<VehicleController>();
                    var egoA = parent.GetComponent<VehicleActions>();
                    var rb = parent.GetComponent<Rigidbody>();
                    id = egoC.GTID;
                    label = "Sedan";
                    linear_vel = UnityEngine.Vector3.Dot(rb.velocity, parent.transform.forward);
                    angular_vel = -rb.angularVelocity.y;
                }
                else if (parent.layer == LayerMask.NameToLayer("NPC"))
                {
                    var npcC = parent.GetComponent<NPCController>();
                    id = npcC.GTID;
                    label = npcC.NPCType;
                    linear_vel = UnityEngine.Vector3.Dot(npcC.GetVelocity(), parent.transform.forward);
                    angular_vel = -npcC.GetAngularVelocity().y;
                }
                else if (parent.layer == LayerMask.NameToLayer("Pedestrian"))
                {
                    var pedC = parent.GetComponent<PedestrianController>();
                    id = pedC.GTID;
                    label = "Pedestrian";
                    linear_vel = UnityEngine.Vector3.Dot(pedC.CurrentVelocity, parent.transform.forward);
                    angular_vel = -pedC.CurrentAngularVelocity.y;
                }
                else
                {
                    return;
                }

                UnityEngine.Vector3 size = ((BoxCollider)other).size;
                // Convert from (Right/Up/Forward) to (Forward/Left/Up)
                size.Set(size.z, size.x, size.y);

                if (size.magnitude == 0)
                {
                    return;
                }

                //Bounding Box Orientation
                //var Rot = Quaternion.Euler(parent.transform.rotation.eulerAngles);
                var rot_euler = parent.transform.rotation.eulerAngles;
                var Rot = UnityEngine.Quaternion.Euler(rot_euler);

                //Bounding Box Position
                if(MapOrigin == null)
                {
                    return;
                }
                UnityEngine.Vector3 bounding_box_center = parent.transform.TransformPoint(((BoxCollider)other).center);                
                var location = MapOrigin.GetGpsLocation(bounding_box_center/*parent.transform.position*/);

                if(!string.IsNullOrEmpty(label))
                {
                    score = 1.0f;
                }

                Detected.Add(other, new Detected3DObjectPerc()
                {
                    Id = id,
                    Label = label,
                    Score = score,
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
