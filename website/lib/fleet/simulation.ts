/*
 * Simulation hook for OhhO Fleet.
 *
 * Drives the live map by slightly updating robot positions towards their targets.
 * Also handles a mock OTA rollout action.
 */

import { useEffect, useState, useRef, useCallback } from "react";
import { generateInitialFleet, INITIAL_ALERTS, VERSIONS, type Robot, type Alert } from "./fleet";

export function useFleetSimulation() {
  const [robots, setRobots] = useState<Robot[]>([]);
  const [alerts, setAlerts] = useState<Alert[]>(INITIAL_ALERTS);
  const [isRollingOut, setIsRollingOut] = useState(false);
  
  const rafRef = useRef<number>(0);

  // Initialize once on client
  useEffect(() => {
    setRobots(generateInitialFleet());
  }, []);

  // Simulation Loop
  useEffect(() => {
    if (robots.length === 0) return;

    let lastTime = performance.now();
    
    const loop = (time: number) => {
      const delta = (time - lastTime) / 1000; // seconds
      lastTime = time;

      setRobots((currentRobots) => 
        currentRobots.map((robot) => {
          if (robot.status === "offline") return robot;

          let { x, y, targetX, targetY, speed } = robot;
          
          // Move towards target
          const dx = targetX - x;
          const dy = targetY - y;
          const dist = Math.sqrt(dx * dx + dy * dy);

          if (dist < 5) {
            // Pick a new target
            targetX = Math.random() * 400;
            targetY = Math.random() * 300;
          } else {
            // We scale movement by 20x to make it visible on the dashboard quickly
            x += (dx / dist) * speed * delta * 20;
            y += (dy / dist) * speed * delta * 20;
          }

          return { ...robot, x, y, targetX, targetY };
        })
      );

      rafRef.current = requestAnimationFrame(loop);
    };

    rafRef.current = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(rafRef.current);
  }, [robots.length]);

  // Mock OTA Rollout action
  const triggerOTA = useCallback(() => {
    setIsRollingOut(true);
    
    setAlerts((prev) => [
      {
        id: `a-${Date.now()}`,
        title: "fleet · OTA started",
        description: "Initiated rollout of v2.4.0-rc.1 to remaining healthy robots.",
        severity: "info",
        time: "just now",
      },
      ...prev
    ]);

    // Simulate robots progressively updating
    let count = 0;
    const interval = setInterval(() => {
      setRobots((prev) => {
        const updatable = prev.filter(r => r.status !== "offline" && r.version !== VERSIONS.CANARY);
        if (updatable.length === 0) {
          clearInterval(interval);
          setIsRollingOut(false);
          return prev;
        }
        
        // Pick one to update
        const toUpdate = updatable[Math.floor(Math.random() * updatable.length)];
        return prev.map(r => r.id === toUpdate.id ? { ...r, version: VERSIONS.CANARY } : r);
      });
      count++;
      if (count > 20) {
        clearInterval(interval);
        setIsRollingOut(false);
      }
    }, 800);

  }, []);

  const rollbackOTA = useCallback(() => {
    setIsRollingOut(true);
    setAlerts((prev) => [
      {
        id: `a-${Date.now()}`,
        title: "fleet · Rollback started",
        description: `Rolling back all canary robots to ${VERSIONS.STABLE}.`,
        severity: "warning",
        time: "just now",
      },
      ...prev
    ]);

    let count = 0;
    const interval = setInterval(() => {
      setRobots((prev) => {
        const canary = prev.filter(r => r.version === VERSIONS.CANARY);
        if (canary.length === 0) {
          clearInterval(interval);
          setIsRollingOut(false);
          return prev;
        }
        const toRollback = canary[Math.floor(Math.random() * canary.length)];
        return prev.map(r => r.id === toRollback.id ? { ...r, version: VERSIONS.STABLE } : r);
      });
      count++;
      if (count > 20) {
        clearInterval(interval);
        setIsRollingOut(false);
      }
    }, 500);
  }, []);

  return {
    robots,
    alerts,
    isRollingOut,
    triggerOTA,
    rollbackOTA,
  };
}
