namespace ChunkProcessing.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string SerialNumber { get; set; }
        public string OSVersion { get; set; }
        public string ComplianceStatus { get; set; }
        public DateTime LastCheckIn { get; set; }
        public string UserPrincipalName { get; set; }
        public string Department { get; set; }
        public string Description { get; internal set; }
        public string Type { get; internal set; }
        public string Location { get; internal set; }
        public DateTime InstallationDate { get; internal set; }
        public string Status { get; internal set; }
    }
}
