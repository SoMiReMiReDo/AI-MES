export function searchNavigation<T extends { label: string }>(
    entries: T[],
    query: string,
) {
    const needle = query.trim().toLocaleLowerCase();
    if (!needle) return entries;

    return entries
        .map((entry) => ({
            entry,
            rank: match(entry.label.toLocaleLowerCase(), needle),
        }))
        .filter(({ rank }) => rank < 4)
        .sort((a, b) => a.rank - b.rank)
        .map(({ entry }) => entry);
}

function match(label: string, needle: string) {
    if (label === needle) return 0;
    if (label.startsWith(needle)) return 1;
    if (label.includes(needle)) return 2;

    let offset = 0;
    for (const char of needle) {
        const position = label.indexOf(char, offset);
        if (position < 0) return 4;
        offset = position + char.length;
    }
    return 3;
}
