/*
 * Mock security data for OhhO Shield.
 */

export interface DeviceIdentity {
  id: string;
  fingerprint: string;
  status: "verified" | "rotate_key" | "untrusted";
  lastSeen: string;
}

export interface CVERecord {
  id: string;
  package: string;
  version: string;
  severity: "critical" | "high" | "medium" | "low";
  status: "open" | "patched";
}

export const INITIAL_DEVICES: DeviceIdentity[] = [
  { id: "amr-01", fingerprint: "sha256·a91f8c2e", status: "verified", lastSeen: "just now" },
  { id: "amr-02", fingerprint: "sha256·b43e11a9", status: "verified", lastSeen: "just now" },
  { id: "amr-04", fingerprint: "sha256·f89d22cc", status: "rotate_key", lastSeen: "4m ago" },
  { id: "amr-05", fingerprint: "sha256·c11b88df", status: "verified", lastSeen: "just now" },
  { id: "amr-07", fingerprint: "sha256·d33a91bb", status: "rotate_key", lastSeen: "just now" },
  { id: "amr-08", fingerprint: "sha256·e55c44aa", status: "verified", lastSeen: "1m ago" },
  { id: "amr-12", fingerprint: "sha256·9a2b33ff", status: "verified", lastSeen: "just now" },
  { id: "amr-15", fingerprint: "sha256·unknown", status: "untrusted", lastSeen: "12m ago" },
];

export const INITIAL_CVES: CVERecord[] = [
  { id: "CVE-2024-1101", package: "libssl", version: "3.0.2", severity: "high", status: "open" },
  { id: "CVE-2023-4492", package: "ros-rmw", version: "6.1.0", severity: "medium", status: "open" },
  { id: "CVE-2024-2210", package: "opencv", version: "4.9.0", severity: "low", status: "patched" },
  { id: "CVE-2023-8812", package: "fastrtps", version: "2.10.1", severity: "high", status: "patched" },
  { id: "CVE-2024-0091", package: "python3", version: "3.10.12", severity: "medium", status: "open" },
];
