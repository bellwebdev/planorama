// Generates src/globals.css from ../shared/tokens.json.
// globals.css holds ONLY: reset, base typography, and token custom properties.
// Run: npm run tokens (also runs automatically before dev/build).
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(fileURLToPath(import.meta.url));
const tokens = JSON.parse(readFileSync(resolve(root, "../../shared/tokens.json"), "utf8"));

const SKIP = new Set(["breakpoint"]);

function flatten(obj, prefix = []) {
  const out = [];
  for (const [key, value] of Object.entries(obj)) {
    if (key.startsWith("$") || SKIP.has(key)) continue;
    if (typeof value === "object" && value !== null) {
      out.push(...flatten(value, [...prefix, key]));
    } else {
      out.push([[...prefix, key].join("-"), value]);
    }
  }
  return out;
}

// Drop the top-level "color" segment so names match usage: --brand-primary, --status-voting, --day-1.
const vars = [
  ...flatten(tokens.color),
  ...flatten({ space: tokens.space, radius: tokens.radius, font: tokens.font }),
];

const css = `/* GENERATED FILE — do not edit by hand.
 * Source: shared/tokens.json · Generator: frontend/scripts/generate-tokens.mjs
 * Contains only: reset, base typography, design-token custom properties. */

:root {
${vars.map(([name, value]) => `  --${name}: ${value};`).join("\n")}
}

/* Reset */
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

html {
  -webkit-text-size-adjust: 100%;
}

img,
svg,
video {
  display: block;
  max-width: 100%;
}

button,
input,
select,
textarea {
  font: inherit;
  color: inherit;
}

/* Base typography (mobile-first, base viewport 360px) */
body {
  font-family: var(--font-family-base);
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-normal);
  line-height: 1.5;
  color: var(--text-default);
  background: var(--surface-bg);
  -webkit-font-smoothing: antialiased;
}

h1 {
  font-family: var(--font-family-heading);
  font-size: var(--font-size-h1);
  font-weight: var(--font-weight-semibold);
  line-height: 1.1;
}

h2 {
  font-family: var(--font-family-heading);
  font-size: var(--font-size-h2);
  font-weight: var(--font-weight-semibold);
  line-height: 1.1;
}

h3 {
  font-size: var(--font-size-h3);
  font-weight: var(--font-weight-semibold);
  line-height: 1.2;
}

p {
  font-size: var(--font-size-body);
  line-height: 1.5;
}

a {
  color: var(--brand-primary);
}
`;

writeFileSync(resolve(root, "../src/globals.css"), css);
console.log(`globals.css generated (${vars.length} tokens)`);
