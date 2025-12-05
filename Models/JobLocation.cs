using System;
using System.Collections.Generic;

namespace SummerSplashWeb.Models
{
    public class JobLocation
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; } = "USA";
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int Radius { get; set; } = 100; // Radius in meters for geofencing

        // Pool Type
        public string? PoolType { get; set; } // Lifeguard Pool, Pool Attendant Pool, Maintenance Pool, Residential Pool, Other
        public string? PoolSize { get; set; }

        // Lockbox Code (alphanumeric, up to 8 characters)
        public string? LockboxCode { get; set; }

        // Supervisor (selected from employees)
        public int? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorPhone { get; set; }

        // Pool Depth (feet and inches)
        public int? PoolDepthFeet { get; set; }
        public int? PoolDepthInches { get; set; }

        // Wading Pool (for babies)
        public bool HasWadingPool { get; set; }
        public int? WadingPoolSizeGallons { get; set; }

        // Spa
        public bool HasSpa { get; set; }
        public int? SpaSizeGallons { get; set; }

        // Legacy contact fields (kept for backward compatibility)
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property for multiple contacts
        public List<LocationContact>? Contacts { get; set; }

        public string DisplayAddress => Address ?? "No address provided";
        public string FullAddress => $"{Address}, {City}, {State} {ZipCode}, {Country}";
        public string DisplayContact => !string.IsNullOrEmpty(ContactName)
            ? $"{ContactName} - {ContactPhone}"
            : "No contact info";

        public string DisplayPoolDepth => PoolDepthFeet.HasValue
            ? $"{PoolDepthFeet}' {PoolDepthInches ?? 0}\""
            : "Not specified";
    }

    public class LocationContact
    {
        public int ContactId { get; set; }
        public int LocationId { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactRole { get; set; } // Property Manager, Regional Manager, Maintenance, Pool Manager
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
