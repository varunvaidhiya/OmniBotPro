/*
 * Simulation hook for OhhO Shield.
 */

import { useState, useCallback, useMemo } from "react";
import { INITIAL_DEVICES, INITIAL_CVES, type DeviceIdentity, type CVERecord } from "./security";

export function useSecuritySimulation() {
  const [devices, setDevices] = useState<DeviceIdentity[]>(INITIAL_DEVICES);
  const [cves, setCves] = useState<CVERecord[]>(INITIAL_CVES);
  
  const [policies, setPolicies] = useState({
    secureBoot: true,
    signedOta: true,
    encryptedDds: false,
  });

  const [isScanning, setIsScanning] = useState(false);

  const togglePolicy = useCallback((key: keyof typeof policies) => {
    setPolicies(prev => ({ ...prev, [key]: !prev[key] }));
  }, []);

  const rotateKey = useCallback((deviceId: string) => {
    setDevices(prev => prev.map(d => {
      if (d.id === deviceId && d.status === "rotate_key") {
        return { ...d, status: "verified", fingerprint: `sha256·${Math.random().toString(16).substr(2, 8)}` };
      }
      return d;
    }));
  }, []);

  const triggerScan = useCallback(() => {
    setIsScanning(true);
    setTimeout(() => {
      // randomly patch a medium CVE
      setCves(prev => prev.map(c => c.severity === "medium" && c.status === "open" ? { ...c, status: "patched" } : c));
      setIsScanning(false);
    }, 2000);
  }, []);

  const riskScore = useMemo(() => {
    let score = 100;
    
    // Penalize for open policies
    if (!policies.secureBoot) score -= 10;
    if (!policies.signedOta) score -= 15;
    if (!policies.encryptedDds) score -= 5;

    // Penalize for untrusted/rotate devices
    devices.forEach(d => {
      if (d.status === "untrusted") score -= 5;
      if (d.status === "rotate_key") score -= 2;
    });

    // Penalize for open CVEs
    cves.filter(c => c.status === "open").forEach(c => {
      if (c.severity === "critical") score -= 15;
      if (c.severity === "high") score -= 8;
      if (c.severity === "medium") score -= 3;
      if (c.severity === "low") score -= 1;
    });

    return Math.max(0, score);
  }, [policies, devices, cves]);

  return {
    devices,
    cves,
    policies,
    togglePolicy,
    rotateKey,
    triggerScan,
    isScanning,
    riskScore,
  };
}
