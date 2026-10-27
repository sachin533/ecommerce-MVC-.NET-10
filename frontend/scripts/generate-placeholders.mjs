/**
 * Generates clean per-category SVG placeholder art for catalog products whose
 * photographs are unavailable. Three tint variants per category keep gallery
 * images from looking identical. Run: node scripts/generate-placeholders.mjs
 */
import { mkdirSync, writeFileSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const outDir = join(dirname(fileURLToPath(import.meta.url)), '..', 'public', 'images', 'placeholders')

// Line-art glyph paths (24x24 viewBox), matching CategoryIcon.vue.
const glyphs = {
  laptops: 'M4 5.5A1.5 1.5 0 0 1 5.5 4h13A1.5 1.5 0 0 1 20 5.5V16H4zM2 18.5h20M5 20h14',
  mobiles: 'M7.5 2.5h9a1.5 1.5 0 0 1 1.5 1.5v16a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 6 20V4a1.5 1.5 0 0 1 1.5-1.5zM10.5 18.5h3',
  tablets: 'M5 3.5h14A1.5 1.5 0 0 1 20.5 5v14a1.5 1.5 0 0 1-1.5 1.5H5A1.5 1.5 0 0 1 3.5 19V5A1.5 1.5 0 0 1 5 3.5zM10.75 17.75h2.5',
  'headphones-audio': 'M4 14v-2a8 8 0 0 1 16 0v2M4 14a2 2 0 0 1 2 2v2a2 2 0 0 1-2 2H3a1 1 0 0 1-1-1v-4a1 1 0 0 1 1-1zM20 14a2 2 0 0 0-2 2v2a2 2 0 0 0 2 2h1a1 1 0 0 0 1-1v-4a1 1 0 0 0-1-1z',
  'smart-watches': 'M12 6.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11zM8.5 2.5h7l.75 3.25M8.5 21.5h7l.75-3.25M12 9.5v2.75l1.75 1',
  watches: 'M12 6.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11zM8.5 2.5h7l.75 3.25M8.5 21.5h7l.75-3.25M12 9.5v2.75l1.75 1',
  accessories: 'M6 8h12l1.2 12.2a1 1 0 0 1-1 1.8H5.8a1 1 0 0 1-1-1.8zM9 8V6a3 3 0 0 1 6 0v2',
  'home-appliances': 'M6 3.5h12A1.5 1.5 0 0 1 19.5 5v14a1.5 1.5 0 0 1-1.5 1.5H6A1.5 1.5 0 0 1 4.5 19V5A1.5 1.5 0 0 1 6 3.5zM12 8.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7z',
  clothing: 'M9 3.5l3 2 3-2 5 3.5-2 4-2-1v11.5a1 1 0 0 1-1 1H9a1 1 0 0 1-1-1V10l-2 1-2-4z',
  footwear: 'M3 15.5c3.5.5 6-1 8.5-4.5l2 1.5 4.5 1.5a2 2 0 0 1 1.5 2l-.3 1.8a1.5 1.5 0 0 1-1.5 1.2H4.5A1.5 1.5 0 0 1 3 17.5z',
  'bags-luggage': 'M8 7V5a4 4 0 0 1 8 0v2M5 7h14a1 1 0 0 1 1 1v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V8a1 1 0 0 1 1-1zM12 11v6',
  'home-furniture': 'M4 11V7.5A1.5 1.5 0 0 1 5.5 6h13A1.5 1.5 0 0 1 20 7.5V11M3 11h18a1.5 1.5 0 0 1 1.5 1.5V17A1.5 1.5 0 0 1 21 18.5H3A1.5 1.5 0 0 1 1.5 17v-4.5A1.5 1.5 0 0 1 3 11zM4.5 18.5V21M19.5 18.5V21',
  'sports-fitness': 'M6.5 6.5l11 11M4 9.5l2-2M20 14.5l-2 2M6.5 6.5l-2 2a2.8 2.8 0 0 0 0 4l1 1a2.8 2.8 0 0 0 4 0l2-2M17.5 17.5l2-2a2.8 2.8 0 0 0 0-4l-1-1a2.8 2.8 0 0 0-4 0l-2 2',
  'kitchen-dining': 'M7 2.5v8a2.5 2.5 0 0 0 5 0v-8M9.5 10.5V21.5M17 2.5c-1.5 1.5-2 3.5-2 5.5s.5 3 2 3.5V21.5',
  cameras: 'M4 8.5h3l1.5-2.5h7L17 8.5h3A1.5 1.5 0 0 1 21.5 10v8A1.5 1.5 0 0 1 20 19.5H4A1.5 1.5 0 0 1 2.5 18v-8A1.5 1.5 0 0 1 4 8.5zM12 16a3.25 3.25 0 1 0 0-6.5 3.25 3.25 0 0 0 0 6.5z',
  gaming: 'M7.5 7.5h9a5 5 0 0 1 4.9 4l.8 4.2a2.6 2.6 0 0 1-4.6 2.1l-1.4-1.9H7.8l-1.4 1.9a2.6 2.6 0 0 1-4.6-2.1l.8-4.2a5 5 0 0 1 4.9-4zM8 10.5v3M6.5 12h3M15.5 11h.01M18 13h.01',
  tvs: 'M3.5 4.5h17a1 1 0 0 1 1 1v10a1 1 0 0 1-1 1h-17a1 1 0 0 1-1-1v-10a1 1 0 0 1 1-1zM8 20.5h8M12 16.5v4',
}

// Three tint variants per category: [from, to, accent]
const variants = [
  ['#f8fafc', '#e2e8f0', '#64748b'],
  ['#eef4ff', '#dbe7fe', '#3b6fd4'],
  ['#fdf4ff', '#f3e8ff', '#8b5cf6'],
]

const categories = Object.keys(glyphs)
mkdirSync(outDir, { recursive: true })

for (const category of categories) {
  variants.forEach(([from, to, accent], index) => {
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="640" viewBox="0 0 640 640" role="img" aria-label="${category} illustration">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${from}"/>
      <stop offset="1" stop-color="${to}"/>
    </linearGradient>
  </defs>
  <rect width="640" height="640" fill="url(#bg)"/>
  <circle cx="${index === 1 ? 500 : 140}" cy="${index === 2 ? 500 : 140}" r="180" fill="${accent}" opacity="0.06"/>
  <g transform="translate(160 160) scale(13.3)" fill="none" stroke="${accent}" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round" opacity="0.85">
    <path d="${glyphs[category]}"/>
  </g>
</svg>
`
    writeFileSync(join(outDir, `${category}-${index + 1}.svg`), svg, 'utf8')
  })
}

console.log(`Generated ${categories.length * variants.length} placeholder SVGs in ${outDir}`)
