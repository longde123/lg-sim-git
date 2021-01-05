using System;
/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;

namespace Simulator.Api.Commands
{
    class VehicleFollowLaneSegment : ICommand
    {
        public string Name => "vehicle/follow_lane_segment";

        public void Execute(JSONNode args)
        {
            var uid = args["uid"].Value;
            var maxSpeed = args["max_speed"].AsFloat;
            var lane_ids_json = args["lane_ids"].AsArray;
            string[] lane_ids_str = new string[lane_ids_json.Count];
            var api = ApiManager.Instance;

            if (api.Agents.TryGetValue(uid, out GameObject obj))
            {
                var npc = obj.GetComponent<NPCController>();
                if (npc == null)
                {
                    api.SendError($"Agent '{uid}' is not a NPC agent");
                    return;
                }

                for(int i = 0; i < lane_ids_json.Count; i++)
                {
                    lane_ids_str[i] = lane_ids_json[i].Value;
                }
                  
                npc.SetFollowLaneSegment(maxSpeed, lane_ids_str);

                api.SendResult();
            }
            else
            {
                api.SendError($"Agent '{uid}' not found");
            }
        }
    }
}
