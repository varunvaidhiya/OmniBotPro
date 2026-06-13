"use client";

import { Canvas } from "@react-three/fiber";
import {
  ContactShadows,
  Environment,
  Grid,
  Lightformer,
  OrbitControls,
} from "@react-three/drei";
import { Suspense } from "react";
import * as THREE from "three";
import OmniBotModel from "./OmniBotModel";

export default function RobotScene() {
  return (
    <Canvas
      shadows
      dpr={[1, 2]}
      gl={{
        antialias: true,
        alpha: true,
        toneMapping: THREE.ACESFilmicToneMapping,
        toneMappingExposure: 1.05,
        powerPreference: "high-performance",
      }}
      camera={{ position: [0.78, 0.52, 0.92], fov: 34, near: 0.05, far: 50 }}
      style={{ width: "100%", height: "100%" }}
    >
      {/* subtle depth fade into the page background */}
      <fog attach="fog" args={["#0A0E1A", 2.4, 6.5]} />

      <Suspense fallback={null}>
        {/* ── lighting rig ── */}
        <ambientLight intensity={0.5} />
        <hemisphereLight args={["#cfe9ff", "#06080f", 0.55]} />

        {/* key light (casts shadows) */}
        <directionalLight
          position={[2.6, 3.6, 2.2]}
          intensity={3.0}
          color="#ffffff"
          castShadow
          shadow-mapSize={[2048, 2048]}
          shadow-bias={-0.0002}
        >
          <orthographicCamera
            attach="shadow-camera"
            args={[-1.6, 1.6, 1.6, -1.6, 0.1, 10]}
          />
        </directionalLight>
        {/* soft front fill so the black chassis keeps its form */}
        <directionalLight position={[-1.5, 1.2, 2.5]} intensity={0.7} color="#dfe9ff" />

        {/* cyan rim + violet kicker — subtle brand glow, not a wash */}
        <spotLight position={[-2.6, 1.5, -1.6]} angle={0.6} penumbra={1} intensity={9} color="#00d4ff" distance={9} />
        <spotLight position={[2.2, 1.0, -2.2]} angle={0.7} penumbra={1} intensity={7} color="#7c3aed" distance={9} />

        {/* procedural studio environment (no CDN dependency) for crisp reflections */}
        <Environment resolution={256} frames={1}>
          <Lightformer intensity={1.6} position={[0, 3, 0]} scale={[4, 4, 1]} color="#ffffff" />
          <Lightformer intensity={1.3} position={[-3, 1, -2]} scale={[3, 3, 1]} color="#00d4ff" />
          <Lightformer intensity={1.0} position={[3, 1, -2]} scale={[3, 3, 1]} color="#7c3aed" />
          <Lightformer intensity={1.1} position={[0, 1.5, 3]} scale={[5, 2, 1]} color="#cfe0ff" />
        </Environment>

        {/* ── the robot ── */}
        <OmniBotModel />

        {/* ── ground ── */}
        <ContactShadows
          position={[0, 0.001, 0]}
          opacity={0.55}
          scale={4}
          blur={2.2}
          far={1.2}
          resolution={1024}
          color="#000000"
        />
        <Grid
          position={[0, 0, 0]}
          args={[12, 12]}
          cellSize={0.18}
          cellThickness={0.6}
          cellColor="#13314a"
          sectionSize={0.9}
          sectionThickness={1.1}
          sectionColor="#00d4ff"
          fadeDistance={5.5}
          fadeStrength={2}
          followCamera={false}
          infiniteGrid
        />

        {/* drag to orbit · idle auto-rotate · scroll to zoom (clamped) */}
        <OrbitControls
          makeDefault
          enablePan={false}
          minDistance={0.7}
          maxDistance={2.2}
          minPolarAngle={0.25}
          maxPolarAngle={Math.PI / 2 - 0.04}
          autoRotate
          autoRotateSpeed={0.45}
          target={[0, 0.2, 0]}
          enableDamping
          dampingFactor={0.08}
        />
      </Suspense>
    </Canvas>
  );
}
