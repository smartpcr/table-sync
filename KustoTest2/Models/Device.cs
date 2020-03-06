using System;

namespace KustoTest2.Models
{
    public class Device : IEquatable<Device>
    {
        public string DeviceName { get; set; }
        public string DcName { get; set; }
        public long DcCode { get; set; }
        public OnboardingMode OnboardingMode { get; set; }

        public bool Equals(Device other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DeviceName.ToLower() == other.DeviceName.ToLower();
        }
    }
}
