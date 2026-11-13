import type { components } from "./generated/api";

export class ApiError extends Error {
    constructor(
        public status: number,
        public code: string,
        message: string,
    ) {
        super(message);
    }
}

let csrf = "";

export async function refreshCsrf() {
    const r = await fetch("/api/v1/auth/csrf", { credentials: "same-origin" });
    if (!r.ok) throw new Error("无法获取安全令牌");
    csrf = (await r.json()).requestToken;
}

export async function api<T = any>(
    path: string,
    method = "GET",
    body?: unknown,
    etag?: string | null,
    idempotencyKey?: string,
): Promise<T> {
    if (method !== "GET" && !csrf) await refreshCsrf();
    const r = await fetch("/api/v1/" + path, {
        method,
        credentials: "same-origin",
        headers: {
            ...(body !== undefined
                ? { "Content-Type": "application/json" }
                : {}),
            ...(method !== "GET" ? { "X-CSRF-TOKEN": csrf } : {}),
            ...(idempotencyKey ? { "Idempotency-Key": idempotencyKey } : {}),
            ...(etag ? { "If-Match": etag } : {}),
        },
        body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    if (!r.ok) {
        const e = await r.json().catch(() => ({}));
        if (r.status === 401 && path !== "auth/login" && path !== "auth/me")
            window.dispatchEvent(new Event("session-invalid"));
        throw new ApiError(r.status, e.code, e.title || "请求失败");
    }
    return r.status === 204 ? (undefined as T) : await r.json();
}

// UI view models narrow the server's numeric JSON output and required identity keys.
export type Me = components["schemas"]["CurrentUserView"];
export type Row = Omit<components["schemas"]["OrgRow"], "id" | "etag"> & {
    id: string;
    etag: string;
    [key: string]: any;
};
export type Page = Omit<
    components["schemas"]["PageViewOfOrgRow"],
    "items" | "total" | "page" | "pageSize"
> & { items: Row[]; total: number; page: number; pageSize: number };

export function signIn(request: components["schemas"]["LoginRequest"]) {
    return api<{ passwordChangeRequired: boolean }>(
        "auth/login",
        "POST",
        request,
    );
}
