using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace KustoTest2.Models
{
    public class PowerDevice : Device
    {
        public DeviceType DeviceType { get; set; }
        public State DeviceState { get; set; }
        public string Hierarchy { get; set; }
        public string ColoName { get; set; }
        public long ColoId { get; set; }
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
        public CommunicationProtocol CopaConfigType { get; set; }
        public CopaConfig CopaConfig { get; set; }
        public string Location { get; set; }
        public string ReservedTiles { get; set; }
        public string ConsumedTiles { get; set; }
        public List<DeviceAssociation> DirectUpstreamDeviceList { get; set; }
        public List<DeviceAssociation> DirectDownstreamDeviceList { get; set; }
        public bool IsMonitorable { get; set; }
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType().GUID != this.GetType().GUID) return false;
            return this.Equals((PowerDevice)obj);
        }

        protected bool Equals(PowerDevice other)
        {
            return base.Equals(other)
            && this.DeviceType == other.DeviceType && this.DeviceState == other.DeviceState
            && string.Equals(this.ColoName, other.ColoName) && this.ColoId == other.ColoId
            && Equals(this.DevicePowerCapacity, other.DevicePowerCapacity)
            && this.AmpRating.Equals(other.AmpRating) && this.VoltageRating.Equals(other.VoltageRating)
            && this.KwRating.Equals(other.KwRating) && this.KvaRating.Equals(other.KvaRating)
            && this.XCoordination == other.XCoordination && this.YCoordination == other.YCoordination
            && string.Equals(this.Hierarchy, other.Hierarchy)
            && this.PowerFactor.Equals(other.PowerFactor) && this.DeRatingFactor.Equals(other.DeRatingFactor)
            && string.Equals(this.PanelName, other.PanelName)
            && this.CopaConfigType == other.CopaConfigType && Equals(this.CopaConfig, other.CopaConfig)
            && string.Equals(this.Location, other.Location)
            && this.DirectUpstreamDeviceList.OrderBy(assocation => assocation.DeviceName)
                .SequenceEqual(other.DirectUpstreamDeviceList.OrderBy(association => association.DeviceName),
                new DeviceAssociationComparer())
            && this.DirectDownstreamDeviceList.OrderBy(assocation => assocation.DeviceName)
                .SequenceEqual(other.DirectDownstreamDeviceList.OrderBy(association => association.DeviceName),
                new DeviceAssociationComparer())
            && this.IsMonitorable == other.IsMonitorable;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = hashCode * 397 + (int)this.DeviceType;
                hashCode = hashCode * 397 + (int)this.DeviceState;
                hashCode = hashCode * 397 + (this.ColoName?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + this.ColoId.GetHashCode();
                hashCode = hashCode * 397 + (this.DevicePowerCapacity?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + this.AmpRating.GetHashCode();
                hashCode = hashCode * 397 + this.VoltageRating.GetHashCode();
                hashCode = hashCode * 397 + this.KwRating.GetHashCode();
                hashCode = hashCode * 397 + this.KvaRating.GetHashCode();
                hashCode = hashCode * 397 + this.XCoordination;
                hashCode = hashCode * 397 + this.YCoordination;
                hashCode = hashCode * 397 + (this.Hierarchy?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + this.PowerFactor.GetHashCode();
                hashCode = hashCode * 397 + this.DeRatingFactor.GetHashCode();
                hashCode = hashCode * 397 + (this.PanelName?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + (int)this.CopaConfigType;
                hashCode = hashCode * 397 + (this.CopaConfig?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + (this.Location?.GetHashCode() ?? 0);
                hashCode = hashCode * 397 + this.DirectUpstreamDeviceList?.Aggregate(hashCode, (cur, association) => cur + association.GetHashCode()) ?? 0;
                hashCode = hashCode * 397 + this.DirectDownstreamDeviceList?.Aggregate(hashCode, (cur, association) => cur + association.GetHashCode()) ?? 0;
                hashCode = hashCode * 397 + this.IsMonitorable.GetHashCode();

                return hashCode;
            }
        }
    }
}
