/**
 * Copyright (c) 2019 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using SimpleJSON;
using UnityEngine;
using System.Collections;
using System;

namespace Simulator.Api.Commands
{
    class ClearScenarioResults : ICommand
    {
        public string Name => "simulator/scenario/results/clear";
        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            SimulatorManager.Instance.ClearScenarioResults();
            api.SendResult();
        }
    }
}
