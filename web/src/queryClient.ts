import { QueryClient } from "@tanstack/react-query";

export const qc = new QueryClient({
    defaultOptions: {
        queries: { retry: false, refetchOnWindowFocus: false },
        mutations: { retry: false },
    },
});
