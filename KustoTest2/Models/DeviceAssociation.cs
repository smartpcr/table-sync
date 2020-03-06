using System;
using System.Collections.Generic;

namespace KustoTest2.Models
{
    public class DeviceAssociation
    {
        public string DeviceName { get; set; }
        public AssociationType AssociationType { get; set; }
        public override string ToString()
        {
            return $"[DeviceName: \"{this.DeviceName}\", AssociationType: \"{this.AssociationType}\"]";
        }
    }

    public class DeviceAssociationComparer : IEqualityComparer<DeviceAssociation>
    {
        public bool Equals(DeviceAssociation x, DeviceAssociation y)
        {
            // Check whether the objects are the same object.
            if (Object.ReferenceEquals(x, y)) return true;

            return x != null && y != null && x.DeviceName.Equals(y.DeviceName) && x.AssociationType.Equals(y.AssociationType);

        }

        public int GetHashCode(DeviceAssociation obj)
        {
            // Get hash code for the Name field if it is not null.
            int hashDeviceName = obj.DeviceName?.GetHashCode() ?? 0;

            // Get hash code for the Code field.
            int hashDeviceAssociationType = obj.AssociationType.GetHashCode();

            return hashDeviceName ^ hashDeviceAssociationType;
        }
    }
}
