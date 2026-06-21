/*
 * Connection protocol catalog + runtime capability detection.
 *
 * The ConnectModal renders one card per protocol and uses isProtocolAvailable()
 * to enable/disable it for the current browser (Web Serial & Web Bluetooth are
 * Chromium-only and require a secure context; ws:// is blocked from https://
 * pages by the mixed-content policy).
 */

import type { ConnectionProtocol } from "./types";

export interface ProtocolMeta {
  id: ConnectionProtocol;
  name: string;
  /** Short chip label. */
  shortName: string;
  /** lucide-react icon name. */
  icon: string;
  blurb: string;
  /** Needs a user-entered network address. */
  needsAddress: boolean;
  /** Placeholder/default address. */
  defaultAddress?: string;
  /** Default serial baud rate (webserial only). */
  defaultBaud?: number;
  /** One-line setup hint shown under the card. */
  help: string;
  /** Not yet broadly supported / depends on robot firmware. */
  experimental?: boolean;
  /** Accent colour key. */
  accent: string;
}

/**
 * Default rosbridge address. Mirrors the Android app + CLAUDE.md default Pi IP.
 * On a secure (https) origin we still suggest ws:// — the modal surfaces the
 * mixed-content warning and the user can switch to wss:// or run on the LAN.
 */
export const DEFAULT_ROSBRIDGE_ADDRESS = "ws://192.168.1.100:9090";

export const PROTOCOLS: ProtocolMeta[] = [
  {
    id: "rosbridge",
    name: "Wi-Fi · ROSBridge",
    shortName: "Wi-Fi",
    icon: "Wifi",
    blurb:
      "Connect over the network to the robot's rosbridge WebSocket server (port 9090). The recommended path — full duplex, works for any ROS 2 robot.",
    needsAddress: true,
    defaultAddress: DEFAULT_ROSBRIDGE_ADDRESS,
    help: "On the robot: ./launch_rosbridge.sh (or ros2 launch rosbridge_server rosbridge_websocket_launch.xml port:=9090).",
    accent: "#00D4FF",
  },
  {
    id: "webserial",
    name: "USB · Serial",
    shortName: "USB",
    icon: "Usb",
    blurb:
      "Plug the robot into this computer with a USB cable. Talks the firmware protocol directly — no ROS, no Pi, no network required.",
    needsAddress: false,
    defaultBaud: 115200,
    help: "Chrome/Edge only. You'll be asked to pick the serial port. For OmniBot this speaks the Yahboom protocol at 115200 baud.",
    accent: "#A78BFA",
  },
  {
    id: "webbluetooth",
    name: "Bluetooth · BLE",
    shortName: "BT",
    icon: "Bluetooth",
    blurb:
      "Pair over Bluetooth Low Energy (Nordic UART serial-over-BLE). Short range, no network — handy in the field.",
    needsAddress: false,
    help: "Chrome/Edge only. Requires the robot to expose a Nordic UART BLE service.",
    experimental: true,
    accent: "#38BDF8",
  },
  {
    id: "simulated",
    name: "Simulated",
    shortName: "Sim",
    icon: "MonitorPlay",
    blurb:
      "No hardware. A deterministic in-browser robot streams telemetry so you can explore every console without a robot connected.",
    needsAddress: false,
    help: "Always available. Great for demos and trying out a console before wiring up real hardware.",
    accent: "#34D399",
  },
];

export function getProtocol(id: ConnectionProtocol): ProtocolMeta {
  return PROTOCOLS.find((p) => p.id === id) ?? PROTOCOLS[0];
}

export interface Availability {
  available: boolean;
  /** Why it's unavailable (shown in the UI). */
  reason?: string;
}

/** Feature-detect a protocol in the current browser/context. */
export function isProtocolAvailable(id: ConnectionProtocol): Availability {
  if (typeof window === "undefined" || typeof navigator === "undefined") {
    return { available: false, reason: "Not available during server render." };
  }
  switch (id) {
    case "rosbridge":
      return typeof WebSocket !== "undefined"
        ? { available: true }
        : { available: false, reason: "This browser has no WebSocket support." };
    case "webserial":
      return "serial" in navigator
        ? { available: true }
        : {
            available: false,
            reason: "Web Serial needs Chrome or Edge over HTTPS or localhost.",
          };
    case "webbluetooth":
      return "bluetooth" in navigator
        ? { available: true }
        : {
            available: false,
            reason: "Web Bluetooth needs Chrome or Edge with a secure context.",
          };
    case "simulated":
      return { available: true };
    default:
      return { available: false, reason: "Unknown protocol." };
  }
}

/**
 * Mixed-content guard: an https:// page cannot open an insecure ws:// socket.
 * Returns a warning string when that mismatch is detected, else null.
 */
export function rosbridgeMixedContentWarning(address: string): string | null {
  if (typeof window === "undefined") return null;
  if (window.location.protocol === "https:" && address.startsWith("ws://")) {
    return (
      "This site is served over HTTPS, which blocks insecure ws:// links. Use a " +
      "wss:// (TLS) rosbridge, or open the site via http://localhost on the same " +
      "network as the robot."
    );
  }
  return null;
}

/** Light validation of a rosbridge URL for the connect form. */
export function isValidWsUrl(address: string): boolean {
  return /^wss?:\/\/[^\s]+$/i.test(address.trim());
}
