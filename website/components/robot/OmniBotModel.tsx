"use client";

/*
 * OmniBotModel — a faithful, interactive Three.js rebuild of the OmniBot
 * mecanum mobile-manipulator described in
 * robot_ws/src/omnibot_description/urdf/omnibot.urdf.xacro and matched against
 * the reference photographs in assets/PXL_2026*.jpg.
 *
 * Coordinate mapping  (URDF is Z-up / X-forward, three.js is Y-up):
 *     three.x =  urdf.x   (forward)
 *     three.y =  urdf.z   (up)
 *     three.z = -urdf.y   (right)
 *
 * Interaction (driven from useFrame):
 *   • Arrow keys / WASD  → drive the base (mecanum: forward, strafe, turn)
 *   • Mouse pointer      → the SO-101 arm aims toward the cursor
 */

import { useFrame, useThree } from "@react-three/fiber";
import { RoundedBox } from "@react-three/drei";
import { useEffect, useMemo, useRef } from "react";
import * as THREE from "three";

/* ── URDF constants (metres) ─────────────────────────────────────── */
const PLATE_T = 0.006;
const STANDOFF_H = 0.16;
const GROUND_CLEAR = 0.07;
const CHASSIS_L = 0.265;
const CHASSIS_W = 0.25;
const BASE_Z = GROUND_CLEAR + PLATE_T / 2; // bottom-plate centre height
const TOP_DZ = PLATE_T + STANDOFF_H; // top-plate centre above base centre
const TOP_Z = BASE_Z + TOP_DZ;
const TOP_SURFACE_Z = TOP_Z + PLATE_T / 2;
const WHEEL_R = 0.04;
const WHEEL_W = 0.037;
const WHEEL_X = 0.165 / 2;
const WHEEL_Y = 0.215 / 2;

/* convert URDF (x,y,z) → three position tuple */
const P = (x: number, y: number, z: number): [number, number, number] => [x, z, -y];

/* ── reusable geometry built once ────────────────────────────────── */

/** A 5-spoke chrome hub-cap shape (matches the star hubs in the photos). */
function useHubcapGeometry() {
  return useMemo(() => {
    const spokes = 5;
    const rOuter = WHEEL_R * 0.66;
    const rValley = WHEEL_R * 0.3;
    const rHub = WHEEL_R * 0.2;
    const shape = new THREE.Shape();
    const pts = spokes * 2;
    for (let i = 0; i <= pts; i++) {
      const a = (i / pts) * Math.PI * 2;
      // pointed spokes with a slight bevel for a turbine-fan look
      const r = i % 2 === 0 ? rOuter : rValley;
      const x = Math.cos(a) * r;
      const y = Math.sin(a) * r;
      if (i === 0) shape.moveTo(x, y);
      else shape.lineTo(x, y);
    }
    // central bolt hole
    const hole = new THREE.Path();
    hole.absarc(0, 0, rHub, 0, Math.PI * 2, true);
    shape.holes.push(hole);

    const geo = new THREE.ExtrudeGeometry(shape, {
      depth: 0.006,
      bevelEnabled: true,
      bevelThickness: 0.0025,
      bevelSize: 0.0025,
      bevelSegments: 2,
      steps: 1,
    });
    // shape is drawn in XY and extruded along +Z, which already lines up with
    // the wheel axle (three.z); just centre it on its own thickness.
    geo.center();
    return geo;
  }, []);
}

/* ── shared materials (physically based) ─────────────────────────── */
function useMaterials() {
  return useMemo(() => {
    const chassis = new THREE.MeshPhysicalMaterial({
      color: "#101216",
      metalness: 0.65,
      roughness: 0.36,
      clearcoat: 0.4,
      clearcoatRoughness: 0.5,
      envMapIntensity: 1.0,
    });
    const brass = new THREE.MeshStandardMaterial({
      color: "#b98a3e",
      metalness: 1.0,
      roughness: 0.28,
      envMapIntensity: 1.3,
    });
    const armWhite = new THREE.MeshPhysicalMaterial({
      color: "#d4d7db",
      metalness: 0.0,
      roughness: 0.68,
      clearcoat: 0.18,
      clearcoatRoughness: 0.6,
      envMapIntensity: 0.55,
    });
    const servo = new THREE.MeshPhysicalMaterial({
      color: "#0d0f12",
      metalness: 0.3,
      roughness: 0.42,
      clearcoat: 0.6,
      clearcoatRoughness: 0.35,
      envMapIntensity: 0.9,
    });
    const tire = new THREE.MeshStandardMaterial({
      color: "#0a0a0c",
      metalness: 0.1,
      roughness: 0.82,
      envMapIntensity: 0.5,
    });
    const chrome = new THREE.MeshPhysicalMaterial({
      color: "#eef2f6",
      metalness: 1.0,
      roughness: 0.07,
      clearcoat: 1.0,
      clearcoatRoughness: 0.04,
      envMapIntensity: 1.6,
    });
    const roller = new THREE.MeshStandardMaterial({
      color: "#b6bcc4",
      metalness: 0.92,
      roughness: 0.32,
      envMapIntensity: 1.2,
    });
    const lens = new THREE.MeshPhysicalMaterial({
      color: "#04060a",
      metalness: 0.2,
      roughness: 0.05,
      clearcoat: 1.0,
      clearcoatRoughness: 0.05,
      envMapIntensity: 1.4,
    });
    const cable = new THREE.MeshStandardMaterial({
      color: "#08090b",
      metalness: 0.0,
      roughness: 0.75,
    });
    const accent = new THREE.MeshStandardMaterial({
      color: "#00d4ff",
      emissive: new THREE.Color("#00d4ff"),
      emissiveIntensity: 2.2,
      metalness: 0.3,
      roughness: 0.4,
    });
    return { chassis, brass, armWhite, servo, tire, chrome, roller, lens, cable, accent };
  }, []);
}

type Mats = ReturnType<typeof useMaterials>;

/* ── one mecanum wheel (tire + diagonal rollers + chrome star hub) ── */
function MecanumWheel({
  mats,
  chir,
  hubGeo,
  outerSign,
  innerRef,
}: {
  mats: Mats;
  chir: 1 | -1;
  hubGeo: THREE.BufferGeometry;
  outerSign: 1 | -1; // +1 → outer face toward +z, −1 → toward −z
  innerRef: React.MutableRefObject<THREE.Group | null>;
}) {
  const rollers = useMemo(() => {
    const N = 12;
    const ringR = WHEEL_R - 0.005;
    return Array.from({ length: N }, (_, i) => ({
      phi: (i / N) * Math.PI * 2,
      ringR,
      key: i,
    }));
  }, []);

  return (
    <group ref={innerRef}>
      {/* tire carcass — dark rubber drum */}
      <mesh castShadow receiveShadow rotation={[Math.PI / 2, 0, 0]} material={mats.tire}>
        <cylinderGeometry args={[WHEEL_R * 0.78, WHEEL_R * 0.78, WHEEL_W * 1.02, 36]} />
      </mesh>
      {/* sidewall rims */}
      {[-1, 1].map((s) => (
        <mesh key={s} rotation={[Math.PI / 2, 0, 0]} position={[0, (s * WHEEL_W) / 2, 0]} material={mats.tire}>
          <torusGeometry args={[WHEEL_R * 0.8, 0.005, 10, 40]} />
        </mesh>
      ))}
      {/* barrel rollers around the rim */}
      {rollers.map(({ phi, ringR, key }) => (
        <group key={key} rotation={[0, 0, phi]}>
          <mesh
            position={[ringR, 0, 0]}
            rotation={[chir * (Math.PI / 4), 0, Math.PI / 2]}
            castShadow
            material={mats.roller}
          >
            {/* capsule reads as a barrel roller with rounded ends, no extra meshes */}
            <capsuleGeometry args={[0.0072, WHEEL_W * 0.86, 4, 10]} />
          </mesh>
        </group>
      ))}
      {/* chrome star hub-cap on the outer face (recessed slightly into the rim) */}
      <mesh
        position={[0, 0, outerSign * (WHEEL_W / 2 - 0.004)]}
        geometry={hubGeo}
        material={mats.chrome}
        castShadow
      />
      {/* dark hub recess behind the cap */}
      <mesh rotation={[Math.PI / 2, 0, 0]} material={mats.servo}>
        <cylinderGeometry args={[WHEEL_R * 0.34, WHEEL_R * 0.34, WHEEL_W * 1.04, 24]} />
      </mesh>
    </group>
  );
}

/* ── rounded box helper expressed in URDF space ──────────────────── */
function RBox({
  size,
  pos,
  mat,
  radius = 0.004,
}: {
  size: [number, number, number]; // URDF (sx, sy, sz)
  pos: [number, number, number]; // URDF (x, y, z)
  mat: THREE.Material;
  radius?: number;
}) {
  // three box extents: x=sx, y=sz, z=sy
  return (
    <RoundedBox
      args={[size[0], size[2], size[1]]}
      radius={Math.min(radius, size[0] / 2, size[1] / 2, size[2] / 2) * 0.98}
      smoothness={3}
      position={P(pos[0], pos[1], pos[2])}
      castShadow
      receiveShadow
      material={mat}
    />
  );
}

export default function OmniBotModel() {
  const mats = useMaterials();
  const hubGeo = useHubcapGeometry();
  const { pointer } = useThree();

  /* group refs */
  const root = useRef<THREE.Group>(null);
  const wheelFL = useRef<THREE.Group>(null);
  const wheelFR = useRef<THREE.Group>(null);
  const wheelRL = useRef<THREE.Group>(null);
  const wheelRR = useRef<THREE.Group>(null);

  /* arm joint group refs */
  const panG = useRef<THREE.Group>(null);
  const liftG = useRef<THREE.Group>(null);
  const elbowG = useRef<THREE.Group>(null);
  const wristG = useRef<THREE.Group>(null);
  const jawL = useRef<THREE.Group>(null);
  const jawR = useRef<THREE.Group>(null);

  /* mutable driving state */
  const keys = useRef<Record<string, boolean>>({});
  const vel = useRef({ x: 0, y: 0, w: 0 }); // body-frame velocities
  const heading = useRef(0);
  const wheelSpin = useRef(0);

  /* arm target angles (smoothed) */
  const arm = useRef({ pan: 0, lift: -0.5, elbow: 1.0, wrist: 0.4, grip: 0 });

  useEffect(() => {
    const tracked = [
      "arrowup",
      "arrowdown",
      "arrowleft",
      "arrowright",
      "w",
      "a",
      "s",
      "d",
      "q",
      "e",
    ];
    const down = (e: KeyboardEvent) => {
      const k = e.key.toLowerCase();
      if (!tracked.includes(k)) return;
      // only capture driving keys while the hero is on screen, so arrow-key
      // scrolling still works once the user has scrolled past it
      const heroVisible = window.scrollY < window.innerHeight * 0.6;
      if (!heroVisible) return;
      keys.current[k] = true;
      if (k.startsWith("arrow")) e.preventDefault(); // stop page scroll
    };
    const up = (e: KeyboardEvent) => {
      keys.current[e.key.toLowerCase()] = false;
    };
    window.addEventListener("keydown", down, { passive: false });
    window.addEventListener("keyup", up);
    return () => {
      window.removeEventListener("keydown", down);
      window.removeEventListener("keyup", up);
    };
  }, []);

  useFrame((_, dtRaw) => {
    const dt = Math.min(dtRaw, 0.05);
    const k = keys.current;

    /* ── target body velocities from keys ── */
    const FWD = 0.55;
    const STRAFE = 0.5;
    const TURN = 1.6;
    let tx = 0,
      ty = 0,
      tw = 0;
    if (k["arrowup"] || k["w"]) tx += FWD;
    if (k["arrowdown"] || k["s"]) tx -= FWD;
    if (k["q"]) ty += STRAFE;
    if (k["e"]) ty -= STRAFE;
    if (k["arrowleft"] || k["a"]) tw += TURN;
    if (k["arrowright"] || k["d"]) tw -= TURN;

    // smooth (the Yahboom board ramp-limits — mimic it)
    const ramp = Math.min(1, 4 * dt);
    vel.current.x += (tx - vel.current.x) * ramp;
    vel.current.y += (ty - vel.current.y) * ramp;
    vel.current.w += (tw - vel.current.w) * ramp;

    /* ── integrate pose ── */
    heading.current += vel.current.w * dt;
    const h = heading.current;
    const dxWorld = (vel.current.x * Math.cos(h) - vel.current.y * Math.sin(h)) * dt;
    const dyWorld = (vel.current.x * Math.sin(h) + vel.current.y * Math.cos(h)) * dt;

    if (root.current) {
      root.current.position.x += dxWorld;
      root.current.position.z += -dyWorld;
      root.current.rotation.y = h;

      // keep the robot inside a tight arena so it never leaves the frame
      const R = 0.62;
      const px = root.current.position.x;
      const pz = root.current.position.z;
      const d = Math.hypot(px, pz);
      if (d > R) {
        root.current.position.x = (px / d) * R;
        root.current.position.z = (pz / d) * R;
      }
    }

    /* ── wheel spin ── */
    wheelSpin.current += (vel.current.x / WHEEL_R) * dt;
    const baseSpin = wheelSpin.current;
    const turn = (vel.current.w * WHEEL_Y) / WHEEL_R;
    if (wheelFL.current) wheelFL.current.rotation.z = -(baseSpin - turn);
    if (wheelFR.current) wheelFR.current.rotation.z = -(baseSpin + turn);
    if (wheelRL.current) wheelRL.current.rotation.z = -(baseSpin - turn);
    if (wheelRR.current) wheelRR.current.rotation.z = -(baseSpin + turn);

    /* ── arm follows the pointer ── */
    const targetPan = THREE.MathUtils.clamp(-pointer.x * 1.4, -1.9, 1.9);
    const reach = THREE.MathUtils.clamp(pointer.y, -1, 1);
    const targetLift = THREE.MathUtils.clamp(-0.35 - reach * 0.85, -1.6, 0.7);
    const targetElbow = THREE.MathUtils.clamp(0.95 - reach * 0.7, -0.2, 1.6);
    const targetWrist = THREE.MathUtils.clamp(0.4 + reach * 0.5, -1.0, 1.4);
    const targetGrip = 0.25 + Math.sin(performance.now() / 700) * 0.2;

    const ease = Math.min(1, 6 * dt);
    arm.current.pan += (targetPan - arm.current.pan) * ease;
    arm.current.lift += (targetLift - arm.current.lift) * ease;
    arm.current.elbow += (targetElbow - arm.current.elbow) * ease;
    arm.current.wrist += (targetWrist - arm.current.wrist) * ease;
    arm.current.grip += (targetGrip - arm.current.grip) * ease;

    if (panG.current) panG.current.rotation.y = arm.current.pan;
    if (liftG.current) liftG.current.rotation.z = arm.current.lift;
    if (elbowG.current) elbowG.current.rotation.z = arm.current.elbow;
    if (wristG.current) wristG.current.rotation.z = arm.current.wrist;
    if (jawL.current) jawL.current.rotation.z = arm.current.grip;
    if (jawR.current) jawR.current.rotation.z = -arm.current.grip;
  });

  /* ── brass cage posts (perimeter, matches the photographed frame) ── */
  const cageXIn = CHASSIS_L / 2 - 0.014;
  const cageYIn = CHASSIS_W / 2 - 0.014;
  const cagePosts: [number, number][] = [
    [cageXIn, cageYIn],
    [cageXIn, -cageYIn],
    [-cageXIn, cageYIn],
    [-cageXIn, -cageYIn],
    [cageXIn, 0],
    [-cageXIn, 0],
    [0, cageYIn],
    [0, -cageYIn],
  ];
  const standoffZ = BASE_Z + PLATE_T / 2 + STANDOFF_H / 2;

  /* ── internal cable bundle (a couple of sagging tubes between plates) ── */
  const cableGeos = useMemo(() => {
    const make = (a: THREE.Vector3, b: THREE.Vector3, sag: number) => {
      const mid = a.clone().lerp(b, 0.5);
      mid.y -= sag;
      const curve = new THREE.CatmullRomCurve3([a, mid, b]);
      return new THREE.TubeGeometry(curve, 24, 0.006, 8, false);
    };
    const zMid = BASE_Z + STANDOFF_H * 0.5;
    return [
      make(new THREE.Vector3(0.06, BASE_Z + 0.03, -0.05), new THREE.Vector3(-0.07, zMid, 0.04), 0.05),
      make(new THREE.Vector3(0.04, zMid, 0.06), new THREE.Vector3(-0.05, BASE_Z + 0.04, -0.06), 0.045),
      make(new THREE.Vector3(-0.02, zMid + 0.02, 0.0), new THREE.Vector3(0.08, BASE_Z + 0.05, 0.02), 0.04),
    ];
  }, []);

  return (
    <group ref={root}>
      {/* ── plates ── */}
      <RBox size={[CHASSIS_L, CHASSIS_W, PLATE_T]} pos={[0, 0, BASE_Z]} mat={mats.chassis} radius={0.006} />
      <RBox size={[CHASSIS_L, CHASSIS_W, PLATE_T]} pos={[0, 0, TOP_Z]} mat={mats.chassis} radius={0.006} />

      {/* ── brass cage posts ── */}
      {cagePosts.map(([x, y], i) => (
        <mesh key={i} position={P(x, y, standoffZ)} castShadow material={mats.brass}>
          <cylinderGeometry args={[0.0065, 0.0065, STANDOFF_H, 14]} />
        </mesh>
      ))}

      {/* ── electronics + cabling hint between the plates ── */}
      <RBox size={[0.13, 0.17, 0.075]} pos={[-0.01, 0, BASE_Z + 0.06]} mat={mats.servo} radius={0.006} />
      <RBox size={[0.085, 0.05, 0.03]} pos={[0.03, 0.03, BASE_Z + 0.03]} mat={mats.lens} radius={0.003} />
      {/* a blue motor-driver block, like the photo */}
      <RBox size={[0.06, 0.045, 0.04]} pos={[0.0, -0.04, BASE_Z + 0.045]} mat={mats.accent} radius={0.003} />
      {cableGeos.map((g, i) => (
        <mesh key={i} geometry={g} material={mats.cable} castShadow />
      ))}
      {/* cyan status strip on the front edge of the top plate */}
      <RBox size={[0.006, 0.1, 0.004]} pos={[CHASSIS_L / 2 - 0.006, 0, TOP_Z]} mat={mats.accent} radius={0.001} />

      {/* ── front/rear side cameras (OV9732) ── */}
      <RBox size={[0.03, 0.03, 0.025]} pos={[CHASSIS_L / 2 - 0.015, 0, BASE_Z + 0.0155]} mat={mats.servo} radius={0.003} />
      <RBox size={[0.03, 0.03, 0.025]} pos={[-(CHASSIS_L / 2 - 0.015), 0, BASE_Z + 0.0155]} mat={mats.servo} radius={0.003} />

      {/* ── Orbbec Astra stereo depth camera on the top plate, facing forward ── */}
      <group position={P(0.0, 0, TOP_SURFACE_Z + 0.028)}>
        {/* horizontal bar body */}
        <RoundedBox args={[0.034, 0.05, 0.165]} radius={0.01} smoothness={4} castShadow material={mats.chassis} />
        {/* two stereo lenses + centre IR projector on the +x face */}
        {[-0.05, 0.0, 0.05].map((z, i) => (
          <group key={i} position={[0.018, 0, z]}>
            <mesh rotation={[0, 0, Math.PI / 2]} material={mats.servo}>
              <cylinderGeometry args={[0.013, 0.013, 0.006, 24]} />
            </mesh>
            <mesh position={[0.004, 0, 0]} rotation={[0, 0, Math.PI / 2]} material={i === 1 ? mats.accent : mats.lens}>
              <cylinderGeometry args={[0.0085, 0.0085, 0.003, 24]} />
            </mesh>
          </group>
        ))}
        {/* small support neck */}
        <mesh position={[0, -0.03, 0]} material={mats.servo}>
          <cylinderGeometry args={[0.01, 0.012, 0.02, 16]} />
        </mesh>
      </group>

      {/* ── four mecanum wheels (diagonal pairs share chirality) ── */}
      <group position={P(WHEEL_X, WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={1} hubGeo={hubGeo} outerSign={-1} innerRef={wheelFL} />
      </group>
      <group position={P(WHEEL_X, -WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={-1} hubGeo={hubGeo} outerSign={1} innerRef={wheelFR} />
      </group>
      <group position={P(-WHEEL_X, WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={-1} hubGeo={hubGeo} outerSign={-1} innerRef={wheelRL} />
      </group>
      <group position={P(-WHEEL_X, -WHEEL_Y, WHEEL_R)}>
        <MecanumWheel mats={mats} chir={1} hubGeo={hubGeo} outerSign={1} innerRef={wheelRR} />
      </group>

      {/* ════════════════════════════════════════════════════════════
          SO-101 6-DOF arm — mounted on the top plate, aims at the mouse
          ════════════════════════════════════════════════════════════ */}
      <group position={P(0.055, 0, TOP_SURFACE_Z)}>
        {/* fixed pedestal */}
        <RoundedBox args={[0.075, 0.065, 0.055]} radius={0.006} smoothness={3} position={[0, 0.032, 0]} castShadow material={mats.armWhite} />
        <mesh position={[0, 0.066, 0]} material={mats.armWhite}>
          <boxGeometry args={[0.08, 0.006, 0.06]} />
        </mesh>

        {/* PAN (about vertical Y) */}
        <group ref={panG} position={[0, 0.062, 0]}>
          <mesh rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
            <cylinderGeometry args={[0.023, 0.023, 0.052, 24]} />
          </mesh>

          {/* LIFT */}
          <group ref={liftG} position={[0, 0.012, 0]}>
            <RoundedBox args={[0.13, 0.034, 0.032]} radius={0.006} smoothness={3} position={[0.06, 0, 0]} castShadow material={mats.armWhite} />
            <mesh position={[0.12, 0, 0]} rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
              <cylinderGeometry args={[0.02, 0.02, 0.044, 22]} />
            </mesh>

            {/* ELBOW */}
            <group ref={elbowG} position={[0.12, 0, 0]}>
              <RoundedBox args={[0.125, 0.03, 0.028]} radius={0.005} smoothness={3} position={[0.058, 0, 0]} castShadow material={mats.armWhite} />
              <mesh position={[0.115, 0, 0]} rotation={[Math.PI / 2, 0, 0]} castShadow material={mats.servo}>
                <cylinderGeometry args={[0.019, 0.019, 0.042, 22]} />
              </mesh>

              {/* WRIST */}
              <group ref={wristG} position={[0.115, 0, 0]}>
                <RoundedBox args={[0.05, 0.046, 0.042]} radius={0.006} smoothness={3} position={[0.03, 0, 0]} castShadow material={mats.armWhite} />
                {/* wrist camera (cyan lens) */}
                <RoundedBox args={[0.022, 0.02, 0.022]} radius={0.003} smoothness={2} position={[0.03, 0.032, 0]} castShadow material={mats.servo} />
                <mesh position={[0.042, 0.032, 0]} rotation={[0, 0, Math.PI / 2]} material={mats.accent}>
                  <cylinderGeometry args={[0.005, 0.005, 0.004, 16]} />
                </mesh>
                {/* jaws */}
                <group ref={jawL} position={[0.055, 0, 0.013]}>
                  <RoundedBox args={[0.05, 0.013, 0.011]} radius={0.003} smoothness={2} position={[0.022, 0, 0]} castShadow material={mats.armWhite} />
                </group>
                <group ref={jawR} position={[0.055, 0, -0.013]}>
                  <RoundedBox args={[0.05, 0.013, 0.011]} radius={0.003} smoothness={2} position={[0.022, 0, 0]} castShadow material={mats.armWhite} />
                </group>
              </group>
            </group>
          </group>
        </group>
      </group>
    </group>
  );
}
