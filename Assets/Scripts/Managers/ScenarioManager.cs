using System.Numerics;
//using System.Runtime.Intrinsics.X86;
using System.Threading;
using SimpleJSON;
using System.Diagnostics;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Simulator.Utilities;
using Simulator.Map;
using Simulator.Api;

    static class BoundsHelper
    {
        public static IEnumerable<UnityEngine.Vector3> GetCorners(this Bounds obj)
        {
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        yield return obj.center + UnityEngine.Vector3.Scale(obj.size / 2, new UnityEngine.Vector3(x, y, z));
                    }
                }
            }
        }
    }

    public class ScenarioManager : MonoBehaviour
    {
        String TestResult;
        private ArrayList Results = new ArrayList();

        const float ego_speed_limit = 11.1f;
        const float ego_to_lane_center_distance_limit = 0.3f;
        const float ego_to_lane_boundary_distance_limit = 0.3f;
        const float ego_to_lane_center_relative_angle_limit = 5.0f;
        const float ego_lateral_accelaration_limit = 0.08f;
        const float ego_longitudinal_acceleration_limit = 0.17f;
        const float ego_lateral_jerk_limit = 0.17f;
        const float ego_longitudinal_jerk_limit = 0.2f;

        int[] counters = new int[10]{1, 1, 1, 1, 1, 1, 1, 1, 1, 1};
        UnityEngine.Vector3 route_point_position, ego_fc_pos, npc_rc_pos;
        float route_point_distance_limit;
        float route_point_speed_limit;
        UnityEngine.Vector3 ego_local_last_velocity = UnityEngine.Vector3.zero;
        UnityEngine.Vector3 ego_local_last_accelaration = UnityEngine.Vector3.zero;



        void FixedUpdate()
        {
            ego_limit_check();
            egofc_to_npcrc_distance_check();
            ego_to_route_point_distance_check();
        }
        void ego_fc_pos_cal()
        {
            var ego_bounds = new Bounds();
            if(ApiManager.Instance.agentEGO)
            {
                foreach (var filter in ApiManager.Instance.agentEGO.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh != null)
                    {
                        foreach (var corner in filter.sharedMesh.bounds.GetCorners())
                        {
                            var pt = filter.transform.TransformPoint(corner);
                            pt = ApiManager.Instance.agentEGO.transform.InverseTransformPoint(pt);
                            ego_bounds.Encapsulate(pt);
                        }
                    }
                }
                foreach (var sk in ApiManager.Instance.agentEGO.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (sk.sharedMesh != null)
                    {
                        foreach (var corner in sk.sharedMesh.bounds.GetCorners())
                        {
                            var pt = sk.transform.TransformPoint(corner);
                            pt = ApiManager.Instance.agentEGO.transform.InverseTransformPoint(pt);
                            ego_bounds.Encapsulate(pt);
                        }
                    }
                }
                UnityEngine.Vector3 ego_bounds_fc = ego_bounds.center + UnityEngine.Vector3.Scale(ego_bounds.size, UnityEngine.Vector3.forward) * 0.5f;
                ego_fc_pos = ApiManager.Instance.agentEGO.transform.TransformPoint(ego_bounds_fc);
            }
        }
        void npc_rc_pos_cal()
        {
            if(ApiManager.Instance.agentNPC)
            {
                var npc_bounds = new Bounds();
                foreach (var filter in ApiManager.Instance.agentNPC.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh != null)
                    {
                        foreach (var corner in filter.sharedMesh.bounds.GetCorners())
                        {
                            var pt = filter.transform.TransformPoint(corner);
                            pt = ApiManager.Instance.agentNPC.transform.InverseTransformPoint(pt);
                            npc_bounds.Encapsulate(pt);
                        }
                    }
                }
                foreach (var sk in ApiManager.Instance.agentNPC.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (sk.sharedMesh != null)
                    {
                        foreach (var corner in sk.sharedMesh.bounds.GetCorners())
                        {
                            var pt = sk.transform.TransformPoint(corner);
                            pt = ApiManager.Instance.agentNPC.transform.InverseTransformPoint(pt);
                            npc_bounds.Encapsulate(pt);
                        }
                    }
                }
                UnityEngine.Vector3 npc_bounds_rc = npc_bounds.center + UnityEngine.Vector3.Scale(npc_bounds.size, UnityEngine.Vector3.back) * 0.5f;
                npc_rc_pos = ApiManager.Instance.agentNPC.transform.TransformPoint(npc_bounds_rc);
            }
        }
        void egofc_to_npcrc_distance_check()
        {
            if(ApiManager.Instance.agentEGO && ApiManager.Instance.agentNPC)
            {
                ego_fc_pos_cal();
                npc_rc_pos_cal();
                float ego_fc_to_npc_rc_distance = UnityEngine.Vector3.Distance(ego_fc_pos, npc_rc_pos);
                //UnityEngine.Debug.DrawLine(ego_fc_pos, npc_rc_pos, Color.red);
                //UnityEngine.Debug.Log($"ego front to npc rear distance:{ego_fc_to_npc_rc_distance}");
                if(ego_fc_to_npc_rc_distance < 0.1f)
                {
                    TestResult = "Distance between ego and npc is less than 0.1m";
                    Results.Add(TestResult);
                }
            }
        }
        void ego_limit_check()
        {
            if(ApiManager.Instance.agentEGO)
            {
                UnityEngine.Vector3 ego_local_acceleration = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 ego_local_jerk = UnityEngine.Vector3.zero;
                UnityEngine.Vector3 ego_position, lane_center_position, lane_boundary_position;
                UnityEngine.Quaternion lane_center_rotation, lane_boundary_rotation;
                UnityEngine.Vector3 ego_rotation;
                Rigidbody rb = ApiManager.Instance.agentEGO.GetComponent<Rigidbody>();
                ego_position = ApiManager.Instance.agentEGO.transform.position;
                ego_rotation = ApiManager.Instance.agentEGO.transform.rotation.eulerAngles;
                UnityEngine.Vector3 ego_local_velocity = rb.transform.InverseTransformDirection(rb.velocity);
                if(ego_local_velocity.z > ego_speed_limit)
                {
                    if(counters[0] == 1)
                    {
                        String StrBuff1 = "Expected Speed limit " + ego_speed_limit + " m/s.";
                        String StrBuff2 = "Actual Speed " + ego_local_velocity.z + " m/s.";
                        TestResult = StrBuff1 + StrBuff2 + "Ego speed exceeding speed limit";
                        Results.Add(TestResult);
                        counters[0]++;
                    }
                }
                if(ego_local_velocity.magnitude != 0 && ego_local_last_velocity.magnitude != 0)
                {
                    ego_local_acceleration = (ego_local_velocity - ego_local_last_velocity) / Time.fixedDeltaTime;
                    if(ego_local_acceleration.x >= ego_lateral_accelaration_limit)
                    {
                        if(counters[6] == 1)
                        {
                            String StrBuff1 = "Expected lateral acceleration should be less than " + ego_lateral_accelaration_limit + " g.";
                            String StrBuff2 = "Actual lateral acceleration " + ego_local_acceleration.x + " g.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego lateral acceleration exceeding limit";
                            Results.Add(TestResult);
                            counters[6]++;
                        }

                    }
                    if(ego_local_acceleration.z >= ego_longitudinal_acceleration_limit)
                    {
                        if(counters[7] == 1)
                        {
                            String StrBuff1 = "Expected longitudinal acceleration should be less than " + ego_longitudinal_acceleration_limit + " g.";
                            String StrBuff2 = "Actual longitudinal acceleration " + ego_local_acceleration.z + " g.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego longitudinal acceleration exceeding limit";
                            Results.Add(TestResult);
                            counters[7]++;
                        }

                    }
                }
                if(ego_local_acceleration.magnitude != 0 && ego_local_last_accelaration.magnitude != 0)
                {
                    ego_local_jerk = (ego_local_acceleration - ego_local_last_accelaration) / Time.fixedDeltaTime;
                    if(ego_local_jerk.x >= ego_lateral_jerk_limit)
                    {
                        if(counters[8] == 1)
                        {
                            String StrBuff1 = "Expected lateral jerk should be less than " + ego_lateral_jerk_limit + " g/s.";
                            String StrBuff2 = "Actual lateral jerk " + ego_local_jerk.x + " g/s.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego lateral jerk exceeding limit";
                            Results.Add(TestResult);
                            counters[8]++;
                        }

                    }
                    if(ego_local_jerk.z >= ego_longitudinal_jerk_limit)
                    {
                        if(counters[9] == 1)
                        {
                            String StrBuff1 = "Expected longitudinal jerk should be less than " + ego_longitudinal_jerk_limit + " g/s.";
                            String StrBuff2 = "Actual longitudinal jerk " + ego_local_jerk.z + " g/s.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego longitudinal jerk exceeding limit";
                            Results.Add(TestResult);
                            counters[9]++;
                        }

                    }
                }
                // UnityEngine.Debug.Log($"Ego velocity:{ego_local_velocity.z}");
                // UnityEngine.Debug.Log($"Ego lateral accl:{ego_local_acceleration.x}");
                // UnityEngine.Debug.Log($"Ego logitudinal accl:{ego_local_acceleration.z}");
                // UnityEngine.Debug.Log($"Ego lateral jerk:{ego_local_jerk.x}");
                // UnityEngine.Debug.Log($"Ego longitudinal jerk:{ego_local_jerk.z}");
                ego_local_last_velocity = ego_local_velocity;
                ego_local_last_accelaration = ego_local_acceleration;
                if(SimulatorManager.Instance.MapManager)
                {
                    SimulatorManager.Instance.MapManager.GetPointOnLane(ego_position, out lane_center_position, out lane_center_rotation);
                    SimulatorManager.Instance.MapManager.GetPointOnLine(ego_position, out lane_boundary_position, out lane_boundary_rotation);
                    UnityEngine.Vector2 ego_pos_2d = new UnityEngine.Vector2(ego_position.x, ego_position.z);
                    UnityEngine.Vector2 lane_center_pos_2d = new UnityEngine.Vector2(lane_center_position.x, lane_center_position.z);
                    UnityEngine.Vector2 lane_boundary_pos_2d = new UnityEngine.Vector2(lane_boundary_position.x, lane_boundary_position.z);
                    UnityEngine.Vector3 ego_closest_pos = rb.ClosestPointOnBounds(lane_boundary_position);
                    UnityEngine.Vector2 ego_closest_pos_2d = new UnityEngine.Vector2(ego_closest_pos.x, ego_closest_pos.z);
                    //float ego_to_lanecenter_dist = UnityEngine.Vector3.Distance(ApiManager.Instance.agentEGO.transform.position, lane_center_position);
                    //float ego_to_lane_boundary_distance = UnityEngine.Vector3.Distance(rb.ClosestPointOnBounds(lane_boundary_position), lane_boundary_position);
                    float ego_to_lanecenter_dist = UnityEngine.Vector2.Distance(ego_pos_2d, lane_center_pos_2d);
                    float ego_to_lane_boundary_distance = UnityEngine.Vector2.Distance(ego_closest_pos_2d, lane_boundary_pos_2d);
                    float ego_relative_yaw_angle = UnityEngine.Vector2.Angle(ego_pos_2d, lane_center_pos_2d);
                    //UnityEngine.Debug.Log($"ego to lane center angle:{ego_relative_yaw_angle}");
                    if(ego_to_lanecenter_dist > ego_to_lane_center_distance_limit)
                    {
                        if(counters[1] == 1)
                        {
                            String StrBuff1 = "Expected egocenter to lanecenter distance limit " + ego_to_lane_center_distance_limit + " m.";
                            String StrBuff2 = "Actual egocenter to lanecenter distance " + ego_to_lanecenter_dist + " m.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego deviating from lane center limit.";
                            Results.Add(TestResult);
                            counters[1]++;
                        }
                    }
                    if(ego_to_lane_boundary_distance < ego_to_lane_boundary_distance_limit)
                    {
                        if(counters[4] == 1)
                        {
                            String StrBuff1 = "Expected ego to lane boundary distance limit " + ego_to_lane_boundary_distance_limit + " m.";
                            String StrBuff2 = "Actual ego to lane boundary distance " + ego_to_lane_boundary_distance + " m.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego deviating from lane boundary limit.";
                            Results.Add(TestResult);
                            counters[4]++;
                        } 
                    }
                    if(ego_relative_yaw_angle >= ego_to_lane_center_relative_angle_limit)
                    {
                        if(counters[5] == 1)
                        {
                            String StrBuff1 = "Expected ego to lane center maximum angle " + ego_to_lane_center_relative_angle_limit + " degree.";
                            String StrBuff2 = "Actual ego to lane center angle " + ego_relative_yaw_angle + " degree.";
                            TestResult = StrBuff1 + StrBuff2 + "Ego deviating from lanecenter angle limit.";
                            Results.Add(TestResult);
                            counters[5]++;
                        }
                    }
                }
            }
        }

        public void ScenarioRunner(int use_case_id, int scenario_id, int test_case_id)
        {
            switch(use_case_id)
            {
                case 1:
                    switch(scenario_id)
                    {                       
                        case 1:
                            switch(test_case_id)
                            {
                                case 1:                             
                                    StartCoroutine(UC01SC01_001());
                                    break;
                                case 2:                             
                                    StartCoroutine(UC01SC01_002());
                                    break;
                            }
                            break;
                    }
                    break;
                default:
                    StartCoroutine(UC01SC01_001());
                    StartCoroutine(UC01SC01_002());
                    break;
            }
        }

        public ArrayList GetResults()
        {
            return Results;
        }

        public void Clear()
        {
            if(Results.Count != 0)
            {
                Results.Clear();
            }
            for (int i = 0; i < counters.Length; i++)
            {
                counters[i] = 1;
            }
        }
        IEnumerator UC01SC01_001()
        {
            if(ApiManager.Instance.controllable != null)
            { 
                UnityEngine.Vector3 distance = ApiManager.Instance.controllable.transform.position - ApiManager.Instance.agentEGO.transform.position;
                if(Mathf.Sqrt((distance.x * distance.x) + (distance.z * distance.z)) <= 57.0)
                {
                    yield return new WaitForSeconds(ApiManager.Instance.controllable.SignalWaitTime-1);
                    float ego_velocity_x = Mathf.Abs(Mathf.Ceil(ApiManager.Instance.agentEGO.GetComponent<Rigidbody>().velocity.x));
                    if(ApiManager.Instance.controllable.CurrentState == "green")
                    {
                        if(ego_velocity_x == 0 || ego_velocity_x == 1)
                        {
                            TestResult = "UC01SC01_001-FAILED";
                        }
                        else
                        {
                            TestResult = "UC01SC01_001-PASSED";
                        }
                        if(counters[2] == 1)
                        {
                            Results.Add(TestResult);
                            counters[2]++;
                        }
                    }
                }     
            }
            else
            {
                if(counters[2] == 1)
                {
                    TestResult = "UC01SC01_001-Traffic Light Not Available in the map";
                    Results.Add(TestResult);
                    counters[2]++;
                }
            }
        }
        IEnumerator UC01SC01_002()
        {
            if(ApiManager.Instance.controllable != null)
            {
                UnityEngine.Vector3 distance = ApiManager.Instance.controllable.transform.position - ApiManager.Instance.agentEGO.transform.position;
                if(Mathf.Sqrt((distance.x * distance.x) + (distance.z * distance.z)) <= 57.0)
                {
                    yield return new WaitForSeconds(ApiManager.Instance.controllable.SignalWaitTime-1);
                    float ego_velocity_x = Mathf.Abs(Mathf.Ceil(ApiManager.Instance.agentEGO.GetComponent<Rigidbody>().velocity.x));
                    if(ApiManager.Instance.controllable.CurrentState == "red")
                    {
                        if(ego_velocity_x == 0 || ego_velocity_x == 1)
                        {
                            TestResult = "UC01SC01_002-PASSED";
                        }
                        else
                        {
                            TestResult = "UC01SC01_002-FAILED";
                        }
                        if(counters[3] == 1)
                        {
                            Results.Add(TestResult);
                            counters[3]++;
                        }
                    }
                }

            }
            else
            {
                if(counters[3] == 1)
                {
                    TestResult = "UC01SC01_002-Traffic Light Not Available in the map";
                    Results.Add(TestResult);
                    counters[3]++;
                }
            }
        }
        public static void UC01SC01_003()
        {
            
        }
        public static void UC01SC01_004()
        {
            
        }
        public void SetRouteDestination(UnityEngine.Vector3 position, float distance, float speed)
        {
            route_point_position = position;
            route_point_distance_limit = distance;
            route_point_speed_limit = speed;
        }
        public void ego_to_route_point_distance_check()
        {
            ego_fc_pos_cal();
            UnityEngine.Vector2 ego_pos = new UnityEngine.Vector2(ego_fc_pos.x, ego_fc_pos.z);
            UnityEngine.Vector2 route_pos = new UnityEngine.Vector2(route_point_position.x, route_point_position.z);          
            float ego_to_route_point_distance = UnityEngine.Vector2.Distance(ego_pos, route_pos);
            //UnityEngine.Debug.Log($"Ego to route point distance:{ego_to_route_point_distance}");
            if(ego_to_route_point_distance <= route_point_distance_limit)
            {
                ApiManager.Instance.agentEGO.GetComponent<Rigidbody>().velocity = UnityEngine.Vector3.zero;
                TestResult = "Ego reached the routing point";
                Results.Add(TestResult);
                ApiManager.Instance.CurrentFrame = ApiManager.Instance.FrameLimit;
            }
        }
    }