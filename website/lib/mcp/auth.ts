/*
 * MCP authentication — validates the API key on every request.
 *
 * The key is checked against the MCP_API_KEY environment variable.
 * If the variable is not set, auth is disabled (development mode).
 * In production, set MCP_API_KEY to a strong random string.
 *
 * Clients send the key via either:
 *   X-API-Key: <key>           (MCP convention)
 *   Authorization: Bearer <key> (standard HTTP)
 */

/** Returns true if the request is authorized, false otherwise. */
export function isAuthorized(req: Request): boolean {
  const expected = process.env.MCP_API_KEY;
  // No key configured → open access (dev mode).
  if (!expected) return true;

  const apiKey = req.headers.get("x-api-key");
  if (apiKey && apiKey === expected) return true;

  const auth = req.headers.get("authorization");
  if (auth && auth.startsWith("Bearer ")) {
    const token = auth.slice(7);
    if (token === expected) return true;
  }

  return false;
}

/** Returns 401 if not authorized, null if authorized. */
export function unauthorizedResponse(): Response {
  return new Response(
    JSON.stringify({
      jsonrpc: "2.0",
      id: null,
      error: {
        code: -32001,
        message: "Unauthorized. Provide an X-API-Key or Authorization: Bearer header.",
      },
    }),
    {
      status: 401,
      headers: { "Content-Type": "application/json" },
    },
  );
}
