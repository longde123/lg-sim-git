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
    class GetScenarioResults : ICommand
    {
        public string Name => "simulator/scenario/results/get";
        public void Execute(JSONNode args)
        {
            var api = ApiManager.Instance;
            ArrayList Results = SimulatorManager.Instance.GetScenarioResults();
            var ResultsJSON = new JSONArray();
            for(int i = 0; i < Results.Count; i++)
            {
                //ResultsJSON[i] = new JSONString((String)Results[i]);
                ResultsJSON[i] = (String)Results[i];
            }
           api.SendResult(ResultsJSON);
        }
    }
}
