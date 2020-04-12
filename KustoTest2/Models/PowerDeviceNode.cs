//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PowerDeviceNode.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using DocDb;
    using Newtonsoft.Json;

    public class PowerDeviceNode : IGremlinVertex
    {
        public PowerDevice Device { get; set; }

        [JsonProperty("id")]
        public string Id
        {
            get => Device.DeviceName;
            set => Device.DeviceName = value;
        }

        [JsonProperty("partitionKey")]
        public string PartitionKey
        {
            get => Device.DcName;
            set => Device.DcName = value;
        }

        public string Label
        {
            get => Device.DeviceName;
            set => Device.DeviceName = value;
        }


        public List<PropertyInfo> GetFlattenedProperties()
        {
            var props = typeof(PowerDevice).GetProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();
            return props;
        }

        public Dictionary<string, string> GetPropertyValues(List<PropertyInfo> props)
        {
            var propValues = new Dictionary<string, string>();

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(Device);
                if (propValue != null)
                {
                    var camelCasePropName = prop.Name.Substring(0, 1).ToLower() + prop.Name.Substring(1);
                    propValues.Add(camelCasePropName, propValue.ToString());
                }
            }

            return propValues;
        }
    }
}