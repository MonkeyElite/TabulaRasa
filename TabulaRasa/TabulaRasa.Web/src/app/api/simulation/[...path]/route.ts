import { NextRequest } from "next/server";

const backendBaseUrl = process.env.TABULARASA_API_URL?.replace(/\/$/, "") ?? "http://localhost:5088/api";

type Context = {
  params: Promise<{ path: string[] }>;
};

export async function GET(request: NextRequest, context: Context) {
  return proxy(request, context);
}

export async function POST(request: NextRequest, context: Context) {
  return proxy(request, context);
}

async function proxy(request: NextRequest, context: Context) {
  const { path } = await context.params;
  const target = `${backendBaseUrl}/simulation/${path.join("/")}${request.nextUrl.search}`;
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
