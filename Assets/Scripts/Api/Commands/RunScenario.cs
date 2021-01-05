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
    class RunScenario : ICommand
    {
        public string Name => "simulator/run_scenario";

        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;

            var timeScale = args["time_scale"];
            if (timeScale == null || timeScale.IsNull)
            {
                api.TimeScale = 1f;
            }
            else
            {
                api.TimeScale = timeScale.AsFloat;
            }

            SimulatorManager.SetTimeScale(api.TimeScale);

            var timeLimit = args["time_limit"].AsFloat;
            if (timeLimit != 0)
            {
                var frameLimit = (int)(timeLimit / Time.fixedDeltaTime);
                api.FrameLimit = api.CurrentFrame + frameLimit;
            }
            else
            {
                api.FrameLimit = 0;
            }

            //Start the scenario runner here
            var use_case_id = args["use_case_id"].AsInt;
            var scenario_id = args["scenario_id"].AsInt;
            var test_case_id = args["test_case_id"].AsInt;
            //ArrayList results;
            SimulatorManager.Instance.Scenario_Config(use_case_id, scenario_id, test_case_id);
            // var results_json = new JSONArray();

            // foreach(String result in results)
            // {
            //     results_json.Add(new JSONString(result));
            // }
            // api.SendResult(results_json);
            SIM.LogAPI(SIM.API.SimulationRun, timeLimit.ToString());
        }
    }
}
