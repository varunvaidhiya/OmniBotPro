"use client";

import { Canvas } from "@react-three/fiber";
import {
  ContactShadows,
  Environment,
  Grid,
  Lightformer,
  MeshReflectorMaterial,
  OrbitControls,
} from "@react-three/drei";
import {
  Bloom,
  EffectComposer,
  N8AO,
  SMAA,
  Vignette,
} from "@react-three/postprocessing";
import { Suspense } from "react";
import * as THREE from "three";
import OmniBotModel from "./OmniBotModel";

export default function RobotScene() {
  return (
    <Canvas
      shadows
      // On-demand rendering: the scene only re-renders when OmniBotModel asks
      // for a frame (cursor moved, keys held, or the robot/arm is still
      // animating). When everything has settled the render loop stops, so the
      // GPU drops to ~0 instead of running flat-out on every page. This is the
      // single biggest win against the "MacBook gets hot" problem.
      frameloop="demand"
      // Cap the device-pixel-ratio at 1.5 instead of 2. On a Retina display
      // this roughly halves the number of shaded pixels (the canvas is
      // full-viewport) with no perceptible loss of sharpness.
      dpr={[1, 1.5]}
      gl={{
        antialias: false, // SMAA handles AA in the composer
        alpha: true,
        toneMapping: THREE.ACESFilmicToneMapping,
        toneMappingExposure: 1.0,
        powerPreference: "high-performance",
      }}
      camera={{ position: [0.78, 1.33, 0.9], fov: 32, near: 0.05, far: 50 }}
      style={{ width: "100%", height: "100%" }}
    >
      {/* subtle depth fade into the page background */}
      <fog attach="fog" args={["#0A0E1A", 2.6, 7.5]} />

      <Suspense fallback={null}>
        {/* ── lighting rig ── */}
        <ambientLight intensity={0.35} />
        <hemisphereLight args={["#cfe9ff", "#05070d", 0.5]} />

        {/* key light (casts shadows) */}
        <directionalLight
          position={[2.6, 3.8, 2.2]}
          intensity={3.2}
          color="#ffffff"
          castShadow
          shadow-mapSize={[1024, 1024]}
          shadow-bias={-0.0002}
          shadow-normalBias={0.02}
        >
          <orthographicCamera
            attach="shadow-camera"
            args={[-1.0, 1.0, 1.0, -1.0, 0.1, 9]}
          />
        </directionalLight>
        {/* soft front fill so the black chassis keeps its form */}
        <directionalLight position={[-1.6, 1.3, 2.6]} intensity={0.7} color="#dfe9ff" />

        {/* cyan rim + violet kicker — brand glow */}
        <spotLight position={[-2.6, 1.6, -1.6]} angle={0.6} penumbra={1} intensity={11} color="#00d4ff" distance={9} />
        <spotLight position={[2.2, 1.1, -2.2]} angle={0.7} penumbra={1} intensity={8} color="#7c3aed" distance={9} />

        {/* procedural studio environment (no CDN dependency) for crisp reflections */}
        <Environment resolution={512} frames={1}>
          <Lightformer intensity={1.8} position={[0, 3.2, 0]} scale={[5, 5, 1]} color="#ffffff" />
          <Lightformer intensity={1.4} position={[-3.2, 1.2, -2]} scale={[3, 3, 1]} color="#00d4ff" />
          <Lightformer intensity={1.1} position={[3.2, 1.2, -2]} scale={[3, 3, 1]} color="#7c3aed" />
          <Lightformer intensity={1.2} position={[0, 1.6, 3.2]} scale={[6, 2.4, 1]} color="#cfe0ff" />
          <Lightformer intensity={1.0} form="ring" position={[1.6, 2.2, 1.6]} scale={2} color="#ffffff" />
        </Environment>

        {/* ── the robot ── */}
        <OmniBotModel />

        {/* ── reflective showroom floor ── */}
        <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, 0, 0]} receiveShadow>
          <planeGeometry args={[30, 30]} />
          <MeshReflectorMaterial
            resolution={512}
            mixBlur={1.0}
            mixStrength={2.2}
            blur={[256, 80]}
            roughness={0.92}
            depthScale={1.1}
            minDepthThreshold={0.4}
            maxDepthThreshold={1.4}
            color="#080b14"
            metalness={0.55}
            mirror={0}
          />
        </mesh>

        {/* soft contact shadow under the wheels */}
        <ContactShadows
          position={[0, 0.002, 0]}
          opacity={0.5}
          scale={2.4}
          blur={2.6}
          far={0.9}
          resolution={512}
          color="#000000"
        />

        {/* faint tech grid floating just above the floor */}
        <Grid
          position={[0, 0.001, 0]}
          args={[12, 12]}
          cellSize={0.18}
          cellThickness={0.5}
          cellColor="#10293d"
          sectionSize={0.9}
          sectionThickness={1.0}
          sectionColor="#0a6f8c"
          fadeDistance={4.2}
          fadeStrength={2.4}
          followCamera={false}
          infiniteGrid
        />

        {/* Fixed elevated framing so the arm can always reach toward the
            cursor. Rotation is disabled: the canvas is a pointer-events:none
            background layer, so it must never swallow scroll/drag from the page. */}
        <OrbitControls
          makeDefault
          enableRotate={false}
          enablePan={false}
          enableZoom={false}
          minDistance={1.7}
          maxDistance={1.7}
          minPolarAngle={0.5}
          maxPolarAngle={0.9}
          target={[0, 0.12, 0]}
          enableDamping
          dampingFactor={0.08}
        />

        {/* ── post-processing: AO, bloom on the cyan accents, vignette, AA ── */}
        <EffectComposer multisampling={0} enableNormalPass={false}>
          <N8AO aoRadius={0.35} intensity={2.4} distanceFalloff={0.8} quality="performance" color="#04060d" />
          <Bloom
            intensity={0.5}
            luminanceThreshold={0.95}
            luminanceSmoothing={0.2}
            mipmapBlur
            radius={0.55}
          />
          <Vignette eskil={false} offset={0.22} darkness={0.72} />
          <SMAA />
        </EffectComposer>
      </Suspense>
    </Canvas>
  );
}
