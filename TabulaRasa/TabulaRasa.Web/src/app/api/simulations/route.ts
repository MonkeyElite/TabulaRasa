import { NextRequest } from "next/server";

const backendBaseUrl = process.env.TABULARASA_API_URL?.replace(/\/$/, "") ?? "http://localhost:5088/api";

export async function GET(request: NextRequest) {
  return proxy(request);
}

export async function POST(request: NextRequest) {
  return proxy(request);
}

async function proxy(request: NextRequest) {
  const target = `${backendBaseUrl}/simulations${request.nextUrl.search}`;
  const body = request.method === "GET" ? undefined : await request.text();
  const response = await fetch(target, {
    method: request.method,
    body,
    headers: {
      "Content-Type": request.headers.get("Content-Type") ?? "application/json"
    },
    cache: "no-store"
  });

  return new Response(await response.text(), {
    status: response.status,
    headers: {
      "Content-Type": response.headers.get("Content-Type") ?? "application/json"
    }
  });
}
