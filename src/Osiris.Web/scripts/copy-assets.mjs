import { copyFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = dirname(dirname(fileURLToPath(import.meta.url)));

const assets = [
  {
    from: join(root, "node_modules", "alpinejs", "dist", "cdn.min.js"),
    to: join(root, "wwwroot", "lib", "alpinejs", "cdn.min.js")
  },
  {
    from: join(root, "node_modules", "htmx.org", "dist", "htmx.min.js"),
    to: join(root, "wwwroot", "lib", "htmx", "htmx.min.js")
  }
];

for (const asset of assets) {
  mkdirSync(dirname(asset.to), { recursive: true });
  copyFileSync(asset.from, asset.to);
}
