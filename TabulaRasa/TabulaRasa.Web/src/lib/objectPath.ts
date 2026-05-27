export function getValue(source: unknown, path: string): unknown {
  return path.split(".").reduce<unknown>((current, segment) => {
    if (current === null || typeof current !== "object") {
      return undefined;
    }

    return (current as Record<string, unknown>)[segment];
  }, source);
}

export function setValue<T>(source: T, path: string, value: unknown): T {
  const segments = path.split(".");

  return setAt(source, segments, value) as T;
}

function setAt(source: unknown, segments: string[], value: unknown): unknown {
  if (segments.length === 0) {
    return value;
  }

  const [head, ...tail] = segments;
  const sourceObject = source && typeof source === "object" ? (source as Record<string, unknown>) : {};

  return {
    ...sourceObject,
    [head]: setAt(sourceObject[head], tail, value)
  };
}
