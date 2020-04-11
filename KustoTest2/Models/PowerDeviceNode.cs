//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="PowerDeviceNode.cs" company="Microsoft Corporation">
//   Copyright (C) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

namespace KustoTest2.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using DocDb;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class PowerDeviceNode : IGremlinVertex
    {
        public string DeviceName { get; set; }
        public string DcName { get; set; }
        public long DcCode { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OnboardingMode OnboardingMode { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public DeviceType DeviceType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public State DeviceState { get; set; }
        public string Hierarchy { get; set; }
        public string ColoName { get; set; }
        public long ColoId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PowerCapacity DevicePowerCapacity { get; set; }
        public double? AmpRating { get; set; }
        public double? VoltageRating { get; set; }
        public double? KwRating { get; set; }
        public double? KvaRating { get; set; }
        public int XCoordination { get; set; }
        public int YCoordination { get; set; }
        public string PrimaryParent { get; set; }
        public string SecondaryParent { get; set; }
        public string MaintenanceParent { get; set; }
        public string RedundantDeviceNames { get; set; }

        public double? PowerFactor { get; set; }
        public double? DeRatingFactor { get; set; }
        public string PanelName { get; set; }
        [EnumDataType(typeof(CommunicationProtocol))]
        [JsonConverter(typeof(StringEnumConverter))]
        public CommunicationProtocol CopaConfigType { get; set; }
        public CopaConfig CopaConfig { get; set; }
        public string Location { get; set; }
        public string ReservedTiles { get; set; }
        public string ConsumedTiles { get; set; }
        public List<DeviceAssociation> DirectUpstreamDeviceList { get; set; }
        public List<DeviceAssociation> DirectDownstreamDeviceList { get; set; }
        public bool IsMonitorable { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Tag Tags { get; set; }
        public double? Amperage { get; set; }
        public double? Voltage { get; set; }
        public double? RatedCapacity { get; set; }
        public double? DeRatedCapacity { get; set; }
        public string DataType { get; set; }
        public string ConfiguredObjectType { get; set; }
        public string DriverName { get; set; }
        public string ConnectionName { get; set; }
        public string IpAddress { get; set; }
        public string PortNumber { get; set; }
        public string NetAddress { get; set; }
        public string ProjectName { get; set; }
        public int UnitId { get; set; }

        public string GetId()
        {
            return DeviceName;
        }

        public string GetPartition()
        {
            return DcName;
        }

        public string GetLabel()
        {
            return DeviceName;
        }
    }
}