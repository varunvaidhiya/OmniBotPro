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
      if (d.id === deviceId && (d.status === "rotate_key" || d.status === "untrusted")) {
        return { ...d, status: "verified", fingerprint: `sha256·${Math.random().toString(16).substr(2, 8)}`, lastSeen: "just now" };
      }
      return d;
    }));
  }, []);

  const triggerScan = useCallback(() => {
    setIsScanning(true);
    const toResolve = cves.filter((c) => c.status === "open");
    if (toResolve.length === 0) {
      setIsScanning(false);
      return;
    }
    let resolved = 0;
    const interval = setInterval(() => {
      resolved++;
      setCves((prev) => {
        const open = prev.filter((c) => c.status === "open");
        if (open.length === 0) return prev;
        const target = open[Math.floor(Math.random() * open.length)];
        return prev.map((c) =>
          c.id === target.id ? { ...c, status: "patched" as const } : c,
        );
      });
      if (resolved >= toResolve.length) {
        clearInterval(interval);
        setIsScanning(false);
      }
    }, 400);
  }, [cves]);

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
