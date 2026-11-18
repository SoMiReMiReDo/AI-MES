export type TraceHistoryEntry = {
    kind: string;
    code: string;
    at: string;
};

export type ViewedTrace = TraceHistoryEntry & { id: string };

type TraceHistory = {
    viewed: ViewedTrace[];
    searched: TraceHistoryEntry[];
};

const storageKey = "mesagent.trace-history";
const limit = 6;

export const emptyTraceHistory = (): TraceHistory => ({
    viewed: [],
    searched: [],
});

export function readTraceHistory(scope: string): TraceHistory {
    try {
        const raw = sessionStorage.getItem(storageKey);
        if (raw) {
            const stored: {
                scope?: string;
                viewed?: unknown;
                searched?: unknown;
            } = JSON.parse(raw);
            if (
                stored?.scope === scope &&
                Array.isArray(stored.viewed) &&
                Array.isArray(stored.searched)
            ) {
                return {
                    viewed: stored.viewed
                        .filter(
                            (entry: unknown) =>
                                isHistoryEntry(entry) &&
                                "id" in entry &&
                                typeof entry.id === "string",
                        )
                        .slice(0, limit),
                    searched: stored.searched
                        .filter(isHistoryEntry)
                        .slice(0, limit),
                };
            }
        }
    } catch {
        // History is optional when browser storage is disabled or unavailable.
    }
    return emptyTraceHistory();
}

function isHistoryEntry(entry: unknown): entry is TraceHistoryEntry {
    return (
        typeof entry === "object" &&
        entry !== null &&
        "kind" in entry &&
        typeof entry.kind === "string" &&
        "code" in entry &&
        typeof entry.code === "string" &&
        "at" in entry &&
        typeof entry.at === "string" &&
        Number.isFinite(Date.parse(entry.at))
    );
}

export function clearTraceHistory() {
    try {
        sessionStorage.removeItem(storageKey);
    } catch {
        // The in-page list can still be cleared without browser storage.
    }
}

export function writeTraceHistory(scope: string, history: TraceHistory) {
    if (!history.viewed.length && !history.searched.length) {
        clearTraceHistory();
        return;
    }
    try {
        sessionStorage.setItem(
            storageKey,
            JSON.stringify({ scope, ...history }),
        );
    } catch {
        // Continue with in-page history.
    }
}

export function rememberTrace<T extends TraceHistoryEntry>(
    entries: T[],
    entry: T,
    isSame: (previous: T) => boolean,
): T[] {
    return [entry, ...entries.filter((previous) => !isSame(previous))].slice(
        0,
        limit,
    );
}
