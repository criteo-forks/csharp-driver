// 
//       Copyright (C) DataStax Inc.
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//       http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using Cassandra.DataStax.Graph;
using Cassandra.Serialization.Graph.Dse;
using Cassandra.Serialization.Graph.Tinkerpop.Structure.IO.GraphSON;
using Newtonsoft.Json.Linq;

namespace Cassandra.Serialization.Graph.GraphSON2
{
    internal class CustomGraphSON2Reader : GraphSON2Reader
    {
        private static readonly Func<JToken, GraphNode> GraphNodeFactory;

        private static readonly IDictionary<string, IGraphSONDeserializer> CustomGraphSON2SpecificDeserializers =
            new Dictionary<string, IGraphSONDeserializer>
            {
                { InstantSerializer.TypeName, new InstantSerializer() },
                { LocalTimeSerializer.TypeName, new LocalTimeSerializer() },
                { LocalDateSerializer.TypeName, new LocalDateSerializer() },
                { InetAddressSerializer.TypeName, new InetAddressSerializer() },

                { BlobSerializer.TypeName, new BlobSerializer() },
                { LineStringSerializer.TypeName, new LineStringSerializer() },
                { PointSerializer.TypeName, new PointSerializer() },
                { PolygonSerializer.TypeName, new PolygonSerializer() },

                { VertexDeserializer.TypeName, new VertexDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },
                { VertexPropertyDeserializer.TypeName, new VertexPropertyDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },
                { EdgeDeserializer.TypeName, new EdgeDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },
                { PathDeserializer.TypeName, new PathDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },
                { PropertyDeserializer.TypeName, new PropertyDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },

                { TraverserDeserializer.TypeName, new TraverserDeserializer(CustomGraphSON2Reader.GraphNodeFactory) },
            };

        static CustomGraphSON2Reader()
        {
            CustomGraphSON2Reader.GraphNodeFactory = token => new GraphNode(new GraphSONNode(token));
        }
        
        /// <summary>
        ///     Creates a new instance of <see cref="GraphSONReader"/>.
        /// </summary>
        public CustomGraphSON2Reader()
        {
            foreach (var kv in CustomGraphSON2Reader.CustomGraphSON2SpecificDeserializers)
            {
                Deserializers[kv.Key] = kv.Value;
            }
        }
    }
}