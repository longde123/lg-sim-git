using System.Collections;
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
    class DestinationSet : ICommand
    {
        public string Name => "destination/set";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var position = args["route_point"].ReadVector3();
            var distance = args["stopping_distance"].AsFloat;
            var speed = args["stopping_speed"].AsFloat;
            SimulatorManager.Instance.SetRouteDestination(position, distance, speed);
            api.SendResult();
        }
    }
}
