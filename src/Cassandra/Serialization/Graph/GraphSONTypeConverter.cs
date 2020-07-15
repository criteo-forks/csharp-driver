﻿//
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
using System.Linq;
using System.Reflection;

using Cassandra.DataStax.Graph;
using Cassandra.Mapping.TypeConversion;
using Cassandra.Serialization.Graph.GraphSON2;
using Cassandra.Serialization.Graph.Tinkerpop.Structure.IO.GraphSON;

using Newtonsoft.Json.Linq;

namespace Cassandra.Serialization.Graph
{
    internal class GraphSONTypeConverter : IGraphSONTypeConverter
    {
        private readonly TypeConverter _typeConverter;
        private readonly GraphSONReader _reader;
        
        public const string TypeKey = "@type";
        public const string ValueKey = "@value";

        public static IGraphSONTypeConverter DefaultInstance = 
            new GraphSONTypeConverter(new DefaultTypeConverter(), new CustomGraphSON2Reader());

        public GraphSONTypeConverter(
            TypeConverter typeConverter, GraphSONReader reader)
        {
            _typeConverter = typeConverter;
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public T To<T>(JToken token)
        {
            var type = typeof(T);
            if (TryConvert(token, type, out var result))
            {
                return (T)result;
            }

            // No converter is available but the types don't match, so attempt to cast
            try
            {
                return (T)result;
            }
            catch (Exception ex)
            {
                var message = $"It is not possible to convert type {result.GetType().FullName} to target type {type.FullName}";
                throw new InvalidTypeException(message, ex);
            }
        }

        public object To(JToken token, Type type)
        {
            if (TryConvert(token, type, out var result))
            {
                return result;
            }

            // No converter is available but the types don't match, so attempt to do:
            //     (TFieldOrProp) row.GetValue<T>(columnIndex);
            try
            {
                return _typeConverter.DynamicCast(result, type);
            }
            catch (Exception ex)
            {
                var message = $"It is not possible to convert type {result.GetType().FullName} to target type {type.FullName}";
                throw new InvalidTypeException(message, ex);
            }
        }

        private bool TryConvert(JToken token, Type type, out object result)
        {
            if (type == typeof(object) || type == typeof(GraphNode) || type == typeof(IGraphNode))
            {
                result = new GraphNode(new GraphSONNode(token));
                return true;
            }

            if (token is JValue)
            {
                return ConvertFromDb(_reader.ToObject(token), type, out result);
            }

            var typeName = string.Empty;
            if (token is JObject)
            {
                typeName = (string)token[GraphSONTokens.TypeKey];
            }

            if (token is JArray || typeName.Equals("g:List") || typeName.Equals("g:Set"))
            {
                Type elementType = null;
                if (type.IsArray)
                {
                    elementType = type.GetElementType();
                }
                else if (type.GetTypeInfo().IsGenericType
                         && (TypeConverter.ListGenericInterfaces.Contains(type.GetGenericTypeDefinition())
                             || type.GetGenericTypeDefinition() == typeof(ISet<>)
                             || type.GetGenericTypeDefinition() == typeof(IList<>)
                             || type.GetGenericTypeDefinition() == typeof(HashSet<>)
                             || type.GetGenericTypeDefinition() == typeof(SortedSet<>)
                             || type.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    elementType = type.GetTypeInfo().GetGenericArguments()[0];
                }

                if (elementType == typeof(object) || elementType == typeof(GraphNode) || elementType == typeof(IGraphNode))
                {
                    if (!(token is JArray))
                    {
                        return ConvertFromDb(ToArray((JArray)token[GraphSONTokens.ValueKey], elementType), type, out result);
                    }

                    return ConvertFromDb(ToArray((JArray)token, elementType), type, out result);
                }
            }

            return ConvertFromDb(_reader.ToObject(token), type, out result);
        }

        private bool ConvertFromDb(object obj, Type targetType, out object result)
        {
            if (obj == null)
            {
                result = null;
                return true;
            }

            var objType = obj.GetType();

            if (targetType == objType || targetType.IsAssignableFrom(objType))
            {
                // No casting/conversion needed
                result = obj;
                return true;
            }

            // Check for a converter
            Delegate converter = _typeConverter.TryGetFromDbConverter(objType, targetType);
            if (converter == null)
            {
                result = obj;
                return false;
            }

            // Invoke the converter function on getValueT (taking into account whether it's a static method):
            //     converter(row.GetValue<T>(columnIndex));
            result = converter.DynamicInvoke(obj);
            return true;
        }

        private Array ToArray(JArray jArray, Type elementType)
        {
            if (elementType == null)
            {
                elementType = typeof(GraphNode);
            }
            var arr = Array.CreateInstance(elementType, jArray.Count);
            var isGraphNode = elementType == typeof(GraphNode);
            for (var i = 0; i < arr.Length; i++)
            {
                var value = isGraphNode
                    ? new GraphNode(new GraphSONNode(jArray[i]))
                    : To(jArray[i], elementType);
                arr.SetValue(value, i);
            }
            return arr;
        }
    }
}