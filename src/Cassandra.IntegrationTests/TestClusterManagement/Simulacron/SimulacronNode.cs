//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Cassandra.IntegrationTests.TestClusterManagement.Simulacron
{
    public class SimulacronNode : SimulacronBase
    {
        public string ContactPoint { get; set; }
        
        public SimulacronNode(string id) : base(id)
        {

        }

        public Task Stop()
        {
            return SimulacronBase.DeleteAsync($"/listener/{Id}?type=stop");
        }

        public Task Start()
        {
            return Put($"/listener/{Id}", null);
        }

        /// <summary>
        /// Gets the list of established connections to a node.
        /// </summary>
        public new IList<IPEndPoint> GetConnections()
        {
            var nodeInfo = base.GetConnections();
            IEnumerable connections = nodeInfo["data_centers"][0]["nodes"][0]["connections"];

            return (from object element in connections
                    select element.ToString().Split(':')
                    into parts
                    select new IPEndPoint(IPAddress.Parse(parts[0]), Convert.ToInt32(parts[1]))).ToList();
        }
    }
}