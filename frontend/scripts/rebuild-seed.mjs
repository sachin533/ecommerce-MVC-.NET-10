/**
 * Rebuilds seed-data.json images and appends the flagship branded catalog.
 *
 *  - Existing 186 catalog products: dummyjson URLs are replaced with local
 *    per-category placeholder art (3 tint variants, cycled per category index).
 *  - New flagship products (Apple/Samsung/Sony/…): use real photographs from
 *    frontend/public/images/products/_manifest-*.json when the sourcing agents
 *    produced them; otherwise fall back to category placeholder art.
 *  - Idempotent: safe to re-run (skips products that already exist by slug,
 *    and always recomputes image arrays).
 *
 * Run: node scripts/rebuild-seed.mjs
 */
import { readFileSync, writeFileSync, readdirSync } from 'node:fs'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const root = join(dirname(fileURLToPath(import.meta.url)), '..')
const seedPath = join(root, '..', '..', 'backend', 'src', 'QuickShop.Api', 'Data', 'Seed', 'seed-data.json')
const productsDir = join(root, 'public', 'images', 'products')

const seed = JSON.parse(readFileSync(seedPath, 'utf8'))

// ---- 1. new categories ----------------------------------------------------
const newCategories = [
  { name: 'Cameras', slug: 'cameras', description: 'Mirrorless, compact and action cameras', sortOrder: 15 },
  { name: 'Gaming', slug: 'gaming', description: 'Consoles, gaming PCs and accessories', sortOrder: 16 },
  { name: 'TVs', slug: 'tvs', description: '4K, QLED and OLED televisions', sortOrder: 17 },
]
for (const category of newCategories) {
  if (!seed.categories.some((c) => c.slug === category.slug)) seed.categories.push(category)
}

// ---- 2. flagship products -------------------------------------------------
/** name, slug, sku, brand, category, price(₹), discount%, stock, rating, ratingCount, daysAgo, featured, description, variants */
const flagships = [
  // ---- Apple ----
  ['MacBook Air 13" M3', 'apple-macbook-air-m3', 'APL-MBA13M3', 'Apple', 'laptops', 114900, 8, 24, 4.8, 2143, 6, true,
    'The 13-inch MacBook Air with the M3 chip delivers exceptional performance in a thin, fanless design. Up to 18 hours of battery life, a brilliant Liquid Retina display, 1080p camera and MagSafe charging make it the do-everything everyday laptop.',
    [['8GB / 256GB', 0, 10], ['8GB / 512GB', 20000, 8], ['16GB / 512GB', 35000, 6]]],
  ['MacBook Pro 14" M3', 'apple-macbook-pro-14', 'APL-MBP14M3', 'Apple', 'laptops', 199900, 5, 18, 4.9, 1620, 9, true,
    'The 14-inch MacBook Pro with M3 Pro delivers game-changing performance for pros. A stunning Liquid Retina XDR display, three Thunderbolt ports, HDMI, SDXC and MagSafe in a portable pro design.',
    [['M3 Pro 11C / 18GB / 512GB', 0, 8], ['M3 Pro 12C / 18GB / 1TB', 40000, 6], ['M3 Max 14C / 36GB / 1TB', 120000, 4]]],
  ['Apple iPhone 15', 'apple-iphone-15', 'APL-IP15-128', 'Apple', 'mobiles', 79900, 6, 40, 4.7, 5890, 8, true,
    'iPhone 15 with Dynamic Island, a 48MP Main camera with 2x Telephoto and USB-C. Durable colour-infused glass design, Ceramic Shield and A16 Bionic.',
    [['128GB', 0, 18], ['256GB', 10000, 14], ['512GB', 30000, 8]]],
  ['Apple iPhone 15 Pro', 'apple-iphone-15-pro', 'APL-IP15P-256', 'Apple', 'mobiles', 134900, 4, 22, 4.8, 3412, 12, true,
    'iPhone 15 Pro in aerospace-grade titanium with the A17 Pro chip, a customizable Action button and a more powerful Pro camera system with 5x Telephoto.',
    [['256GB', 0, 10], ['512GB', 20000, 7], ['1TB', 40000, 5]]],
  ['iPad Air 11" M2', 'apple-ipad-air', 'APL-IPA-M2-128', 'Apple', 'tablets', 59900, 7, 26, 4.7, 1878, 20, false,
    'iPad Air with the M2 chip brings blazing performance to the thin-and-light Air design. Liquid Retina display, Apple Pencil Pro support and landscape front camera.',
    [['128GB Wi-Fi', 0, 12], ['256GB Wi-Fi', 8000, 8], ['256GB Wi-Fi + Cellular', 20000, 6]]],
  ['AirPods Pro (2nd generation)', 'apple-airpods-pro-2', 'APL-APP2', 'Apple', 'headphones-audio', 24900, 12, 44, 4.8, 7215, 15, true,
    'AirPods Pro 2nd generation with the H2 chip deliver up to 2x more Active Noise Cancellation, Adaptive Audio, Transparency mode and Personalized Spatial Audio, with a MagSafe charging case.',
    []],
  ['AirPods Max', 'apple-airpods-max', 'APL-APMAX', 'Apple', 'headphones-audio', 59900, 10, 14, 4.6, 1102, 30, false,
    'AirPods Max combine a stunning over-ear design with high-fidelity audio, Adaptive EQ, Active Noise Cancellation and Transparency mode, plus Spatial Audio.',
    [['Midnight', 0, 5], ['Starlight', 0, 4], ['Blue', 0, 5]]],
  ['Apple Watch Series 9 GPS 45mm', 'apple-watch-s9', 'APL-AWS9-45', 'Apple', 'smart-watches', 44900, 9, 30, 4.8, 3320, 10, true,
    'Apple Watch Series 9 with the S9 SiP, Double Tap gesture, 2000-nit display and advanced health, fitness and safety features.',
    [['Midnight Sport Loop', 0, 12], ['Starlight Sport Loop', 0, 10], ['Product(RED) Sport Band', 0, 8]]],
  // ---- Samsung ----
  ['Samsung Galaxy S24 Ultra 512GB', 'samsung-galaxy-s24-ultra', 'SAM-S24U-512', 'Samsung', 'mobiles', 139999, 10, 26, 4.8, 4120, 5, true,
    'Galaxy S24 Ultra in titanium with built-in S Pen, 200MP camera, 5x optical zoom and Galaxy AI — Circle to Search, Live Translate and Generative Edit.',
    [['256GB', -10000, 10], ['512GB', 0, 10], ['1TB', 20000, 6]]],
  ['Samsung Galaxy S24 256GB', 'samsung-galaxy-s24', 'SAM-S24-256', 'Samsung', 'mobiles', 79999, 12, 34, 4.7, 2890, 5, true,
    'Galaxy S24 packs a 50MP camera, exynos 2400 / Snapdragon 8 Gen 3 power and Galaxy AI into a compact flagship with a 120Hz AMOLED display.',
    [['256GB', 0, 16], ['512GB', 12000, 10]]],
  ['Samsung Galaxy S23 256GB', 'samsung-galaxy-s23', 'SAM-S23-256', 'Samsung', 'mobiles', 64999, 22, 28, 4.6, 6210, 40, false,
    'Galaxy S23 with a 50MP adaptive pixel camera, Snapdragon 8 Gen 2 for Galaxy and a 6.1-inch Dynamic AMOLED 2X display.',
    [['256GB', 0, 14], ['512GB', 10000, 8]]],
  ['Samsung Galaxy Z Flip5 256GB', 'samsung-galaxy-z-flip5', 'SAM-ZF5-256', 'Samsung', 'mobiles', 99999, 15, 16, 4.5, 1340, 25, false,
    'Galaxy Z Flip5 folds flat with a 3.4-inch Flex Window, a 12MP dual camera and iconic pocketable design in premium finishes.',
    [['256GB', 0, 8], ['512GB', 15000, 6]]],
  ['Samsung Galaxy Tab S9 512GB', 'samsung-galaxy-tab-s9', 'SAM-TABS9-512', 'Samsung', 'tablets', 92999, 12, 15, 4.7, 980, 30, false,
    'Galaxy Tab S9 with 11-inch Dynamic AMOLED 2X display, Snapdragon 8 Gen 2, IP68 S Pen and DeX productivity.',
    [['512GB Wi-Fi', 0, 8], ['512GB 5G', 15000, 5]]],
  ['Samsung Galaxy Book3 Pro 360', 'samsung-galaxy-book3-pro-360', 'SAM-GBP3P', 'Samsung', 'laptops', 124999, 10, 12, 4.5, 640, 45, false,
    'Galaxy Book3 Pro 360 with a 16-inch 3K AMOLED 2X touchscreen, 360° hinge, S Pen support and Evo-certified performance.',
    [['16" / 16GB / 512GB', 0, 7], ['16" / 16GB / 1TB', 15000, 5]]],
  ['Samsung 55" Neo QLED 4K QN85C', 'samsung-neo-qled-55', 'SAM-QN85C-55', 'Samsung', 'tvs', 129999, 32, 10, 4.7, 760, 18, true,
    'Samsung 55-inch Neo QLED 4K TV with Quantum Matrix Technology, Neural Quantum Processor 4K, Dolby Atmos and a virtually bezel-less design.',
    [['55"', 0, 4], ['65"', 40000, 3], ['75"', 90000, 3]]],
  ['Samsung 50" Crystal UHD 4K CU7000', 'samsung-crystal-tv-50', 'SAM-CU7000-50', 'Samsung', 'tvs', 45999, 30, 14, 4.5, 1120, 22, false,
    'Samsung 50-inch Crystal UHD 4K TV with Crystal Processor, HDR10+ and Smart Hub streaming built in.',
    [['50"', 0, 7], ['55"', 8000, 4], ['65"', 18000, 3]]],
  // ---- LG ----
  ['LG 55" OLED evo C3 4K TV', 'lg-oled-c3-55', 'LG-OLEDC3-55', 'LG', 'tvs', 159999, 25, 9, 4.8, 540, 20, true,
    'LG 55-inch OLED evo C3 with the a9 AI Processor 4K Gen6, 120Hz panels, Dolby Vision and Dolby Atmos, plus four HDMI 2.1 ports for next-gen gaming.',
    [['55"', 0, 4], ['65"', 45000, 3], ['77"', 120000, 2]]],
  // ---- Sony ----
  ['Sony WH-1000XM5 Headphones', 'sony-wh-1000xm5', 'SNY-WH1000XM5', 'Sony', 'headphones-audio', 34990, 20, 32, 4.8, 8430, 7, true,
    'Sony WH-1000XM5 wireless noise-cancelling headphones with eight microphones, Auto NC Optimizer, crystal-clear hands-free calling and 30-hour battery.',
    [['Black', 0, 14], ['Midnight Blue', 0, 9], ['Platinum Silver', 0, 9]]],
  ['Sony WF-1000XM5 Earbuds', 'sony-wf-1000xm5', 'SNY-WF1000XM5', 'Sony', 'headphones-audio', 24990, 18, 26, 4.7, 3210, 14, false,
    'Sony WF-1000XM5 are the smallest, lightest noise-cancelling earbuds yet — Dynamic Driver X, exceptional call quality and up to 8 hours per charge.',
    [['Black', 0, 12], ['Platinum Silver', 0, 8], ['Smoke Blue', 0, 6]]],
  ['Sony Alpha a6400 Mirrorless Camera', 'sony-alpha-a6400', 'SNY-ILCE6400L', 'Sony', 'cameras', 84990, 15, 12, 4.7, 890, 35, true,
    'Sony Alpha a6400 with 24.2MP APS-C sensor, Real-time Tracking/eye AF, 4K HDR video and a 180-degree tiltable screen for vlogging.',
    [['Body only', -8000, 4], ['with 18-55mm lens', 0, 5], ['with 16-55mm f/2.8 G', 60000, 3]]],
  ['Sony ZV-1 Vlog Camera', 'sony-zv1', 'SNY-ZV1', 'Sony', 'cameras', 74990, 12, 10, 4.6, 620, 50, false,
    'Sony ZV-1 vlogging camera with 20.1MP 1-inch sensor, Vari-angle touchscreen, Directional 3-capsule mic and Background Defocus.',
    []],
  ['Sony PlayStation 5 Slim', 'sony-playstation-5', 'SNY-PS5-SLIM', 'Sony', 'gaming', 54990, 5, 20, 4.9, 9120, 4, true,
    'PlayStation 5 Slim with ultra-high-speed SSD, ray tracing, 4K/120Hz output and the DualSense wireless controller in the box.',
    [['Disc Edition', 0, 12], ['Digital Edition', -5000, 8]]],
  ['Sony DualSense Wireless Controller', 'sony-dualsense', 'SNY-DUALSENSE', 'Sony', 'gaming', 6490, 10, 40, 4.8, 4210, 25, false,
    'DualSense wireless controller with haptic feedback, adaptive triggers and built-in microphone for PS5.',
    [['White', 0, 18], ['Midnight Black', 0, 12], ['Cosmic Red', 500, 10]]],
  ['Sony 55" BRAVIA XR X90L 4K TV', 'sony-bravia-x90l-55', 'SNY-X90L-55', 'Sony', 'tvs', 144990, 28, 8, 4.7, 430, 24, false,
    'Sony 55-inch BRAVIA XR 4K LED TV with Cognitive Processor XR, XR Contrast Booster, Acoustic Multi-Audio and Google TV.',
    [['55"', 0, 4], ['65"', 35000, 2], ['75"', 80000, 2]]],
  // ---- Nintendo / Microsoft / Valve ----
  ['Nintendo Switch OLED Model', 'nintendo-switch-oled', 'NIN-SW-OLED', 'Nintendo', 'gaming', 37990, 5, 24, 4.8, 6340, 16, true,
    'Nintendo Switch – OLED Model with a 7-inch OLED screen, wide adjustable stand, 64GB storage and enhanced audio.',
    [['White Joy-Con', 0, 12], ['Neon Blue/Red Joy-Con', 0, 12]]],
  ['Microsoft Xbox Series X 1TB', 'xbox-series-x', 'MSF-XSX-1TB', 'Microsoft', 'gaming', 54990, 3, 14, 4.8, 3120, 30, false,
    'Xbox Series X — the fastest, most powerful Xbox ever with 12 teraflops, quick resume and 4K gaming at up to 120fps.',
    []],
  ['Valve Steam Deck 512GB', 'steam-deck-512', 'VLV-STEAM512', 'Valve', 'gaming', 62990, 0, 8, 4.6, 980, 40, false,
    'Steam Deck handheld PC with a 7-inch 1280×800 touchscreen, AMD APU and full Steam library compatibility.',
    []],
  // ---- Canon / Nikon / GoPro ----
  ['Canon EOS R50 + RF-S 18-45mm', 'canon-eos-r50', 'CNR-EOSR50K', 'Canon', 'cameras', 75999, 10, 14, 4.7, 760, 12, true,
    'Canon EOS R50 mirrorless camera with 24.2MP APS-C sensor, Dual Pixel CMOS AF II, 4K video and the RF-S 18-45mm kit lens.',
    []],
  ['Nikon Z50 II + 16-50mm', 'nikon-z50ii', 'NKN-Z50II-K', 'Nikon', 'cameras', 99999, 8, 10, 4.6, 340, 15, false,
    'Nikon Z50 II with 20.9MP APS-C sensor, EXPEED 7 processing, advanced autofocus and the compact NIKKOR Z 16-50mm kit lens.',
    []],
  ['GoPro HERO12 Black', 'gopro-hero12', 'GPR-HERO12', 'GoPro', 'cameras', 45999, 15, 18, 4.7, 1560, 9, false,
    'HERO12 Black captures 5.3K60 video with HyperSmooth 6.0 stabilisation, HDR photo, Bluetooth audio and rugged waterproof design.',
    [['Camera only', 0, 10], ['with Accessories Bundle', 6000, 8]]],
  // ---- JBL / Bose / Google ----
  ['JBL Charge 5 Portable Speaker', 'jbl-charge-5', 'JBL-CHG5', 'JBL', 'headphones-audio', 16999, 25, 30, 4.7, 2890, 28, false,
    'JBL Charge 5 bluetooth speaker with powerful JBL Original Pro sound, IP67 waterproofing, 20-hour battery and powerbank function.',
    [['Black', 0, 12], ['Blue', 0, 9], ['Red', 0, 9]]],
  ['Bose QuietComfort Ultra Headphones', 'bose-qc-ultra', 'BSE-QCUltra', 'Bose', 'headphones-audio', 44900, 12, 14, 4.8, 1420, 11, false,
    'Bose QuietComfort Ultra headphones with world-class noise cancellation, Immersive Audio and customisable comfort.',
    [['Black', 0, 6], ['White Smoke', 0, 4], ['Sandstone', 0, 4]]],
  ['Google Pixel 8 128GB', 'google-pixel-8', 'GGL-PIX8-128', 'Google', 'mobiles', 69999, 15, 22, 4.7, 2140, 13, false,
    'Google Pixel 8 with Tensor G3, pro-grade cameras with Magic Editor, 6.2-inch Actua display and 7 years of OS updates.',
    [['128GB', 0, 12], ['256GB', 10000, 10]]],
  // ---- Dell ----
  ['Dell XPS 13 9340', 'dell-xps-13', 'DEL-XPS13-9340', 'Dell', 'laptops', 149999, 8, 12, 4.6, 720, 8, true,
    'Dell XPS 13 with Intel Core Ultra processors, a stunning InfinityEdge display and CNC-machined aluminium construction.',
    [['Ultra 5 / 16GB / 512GB', 0, 6], ['Ultra 7 / 16GB / 1TB', 25000, 4], ['Ultra 7 / 32GB / 1TB', 45000, 2]]],
  ['Dell Inspiron 15 3520', 'dell-inspiron-15', 'DEL-INS15-3520', 'Dell', 'laptops', 65990, 18, 24, 4.4, 1830, 26, false,
    'Dell Inspiron 15 with 12th-gen Intel Core i5, 16GB RAM, 512GB SSD and a 15.6-inch FHD display — everyday productivity, sorted.',
    [['i5 / 8GB / 512GB', -5000, 10], ['i5 / 16GB / 512GB', 0, 8], ['i7 / 16GB / 1TB', 18000, 6]]],
  ['Alienware Aurora r16', 'alienware-aurora-r16', 'DEL-AWR16', 'Dell', 'gaming', 249999, 10, 6, 4.7, 410, 14, true,
    'Alienware Aurora r16 gaming desktop with up to Intel Core i9, NVIDIA GeForce RTX 4070, cryo-tech cooling and a 240Hz-ready build.',
    [['i7 / RTX 4070 / 16GB', 0, 3], ['i9 / RTX 4080 / 32GB', 80000, 3]]],
  // ---- Lenovo ----
  ['Lenovo ThinkPad X1 Carbon Gen 11', 'lenovo-thinkpad-x1-carbon', 'LNV-X1C-G11', 'Lenovo', 'laptops', 169999, 12, 10, 4.8, 860, 10, true,
    'ThinkPad X1 Carbon Gen 11 — legendary business ultrabook with 14-inch 2.8K display, Intel Core vPro, MIL-SPEC durability and all-day battery.',
    [['i5 / 16GB / 512GB', 0, 5], ['i7 / 16GB / 1TB', 20000, 3], ['i7 / 32GB / 1TB', 35000, 2]]],
  ['Lenovo IdeaPad Slim 3 14', 'lenovo-ideapad-slim-3', 'LNV-IPS3-14', 'Lenovo', 'laptops', 54990, 20, 26, 4.4, 1540, 21, false,
    'IdeaPad Slim 3 with AMD Ryzen 5, 16GB RAM, 512GB SSD and a 14-inch FHD display in a 1.46kg chassis.',
    [['Ryzen 5 / 8GB / 512GB', -3000, 12], ['Ryzen 5 / 16GB / 512GB', 0, 8], ['Ryzen 7 / 16GB / 1TB', 12000, 6]]],
  ['Lenovo Legion Pro 5 16', 'lenovo-legion-5-pro', 'LNV-LP5-16', 'Lenovo', 'gaming', 174999, 15, 8, 4.7, 590, 17, true,
    'Legion Pro 5 16 with Ryzen 7, NVIDIA RTX 4060, 16-inch 165Hz WQXGA display and Legion ColdFront cooling.',
    [['Ryzen 7 / RTX 4060 / 16GB', 0, 4], ['Ryzen 9 / RTX 4070 / 32GB', 40000, 4]]],
  // ---- HP ----
  ['HP Pavilion Plus 14', 'hp-pavilion-15', 'HPP-PAV14', 'HP', 'laptops', 94999, 12, 16, 4.5, 920, 19, false,
    'HP Pavilion Plus 14 with Intel Core Ultra, a 2.8K OLED display and audio by B&O in a slim aluminium body.',
    [['Ultra 5 / 16GB / 512GB', 0, 8], ['Ultra 7 / 16GB / 1TB', 18000, 5], ['Ultra 7 / 32GB / 1TB', 30000, 3]]],
  ['HP Spectre x360 14', 'hp-spectre-x360-14', 'HPP-SPT14', 'HP', 'laptops', 164999, 10, 10, 4.7, 680, 23, false,
    'HP Spectre x360 14 2-in-1 with 3K OLED touch display, Intel Core Ultra, poly-studio audio and a gem-cut design.',
    [['Ultra 7 / 16GB / 1TB', 0, 6], ['Ultra 7 / 32GB / 2TB', 25000, 4]]],
  ['HP Omen 16', 'hp-omen-16', 'HPP-OMN16', 'HP', 'gaming', 159999, 12, 9, 4.6, 470, 15, false,
    'HP Omen 16 gaming laptop with Intel Core i7, RTX 4060, 165Hz QHD display and Omen Tempest cooling.',
    [['i7 / RTX 4060 / 16GB', 0, 5], ['i9 / RTX 4070 / 32GB', 35000, 4]]],
  // ---- OnePlus ----
  ['OnePlus 12 5G', 'oneplus-12', 'OPN-12-256', 'OnePlus', 'mobiles', 69999, 8, 28, 4.7, 1980, 7, true,
    'OnePlus 12 with Snapdragon 8 Gen 3, Hasselblad camera system with 64MP 3x periscope telephoto and 100W SUPERVOOC charging.',
    [['12GB / 256GB', 0, 14], ['16GB / 512GB', 8000, 8], ['16GB / 1TB', 15000, 6]]],
  ['OnePlus 12R 5G', 'oneplus-12r', 'OPN-12R-128', 'OnePlus', 'mobiles', 39999, 10, 30, 4.6, 1420, 7, false,
    'OnePlus 12R with Snapdragon 8 Gen 2, 120Hz ProXDR display and 100W SUPERVOOC fast charging.',
    [['8GB / 128GB', 0, 14], ['12GB / 256GB', 4000, 10], ['16GB / 256GB', 6000, 6]]],
  ['OnePlus Nord CE4 Lite 5G', 'oneplus-nord-ce4', 'OPN-NCEL4', 'OnePlus', 'mobiles', 19999, 8, 36, 4.4, 1110, 10, false,
    'OnePlus Nord CE4 Lite with 5500mAh battery, 80W SUPERVOOC charging and a 120Hz AMOLED display.',
    [['8GB / 128GB', 0, 18], ['8GB / 256GB', 2000, 10], ['12GB / 256GB', 4000, 8]]],
  // ---- Xiaomi ----
  ['Xiaomi 14 5G', 'xiaomi-14', 'XMI-14-256', 'Xiaomi', 'mobiles', 79999, 15, 18, 4.6, 890, 12, false,
    'Xiaomi 14 with Leica Summilux optics, Snapdragon 8 Gen 3, 120W HyperCharge and a 6.36-inch 1.5K AMOLED display.',
    [['12GB / 256GB', 0, 9], ['12GB / 512GB', 6000, 9]]],
  ['Redmi Note 13 Pro+ 5G', 'xiaomi-redmi-note-13-pro', 'XMI-RN13PP', 'Xiaomi', 'mobiles', 29999, 18, 40, 4.5, 3240, 18, false,
    'Redmi Note 13 Pro+ with a 200MP camera, 120W HyperCharge, curved AMOLED display and IP68 protection.',
    [['8GB / 256GB', 0, 18], ['12GB / 256GB', 2000, 12], ['12GB / 512GB', 5000, 10]]],
  ['Xiaomi TV 5A 43" 4K', 'xiaomi-mi-tv-55', 'XMI-TV5A-43', 'Xiaomi', 'tvs', 26999, 25, 16, 4.4, 640, 27, false,
    'Xiaomi TV 5A 43-inch 4K with Dolby Vision, 30W speakers, Google TV and far-field voice control.',
    [['43"', 0, 8], ['50"', 6000, 5], ['55"', 10000, 3]]],
  ['Xiaomi Smart Band 9', 'xiaomi-smart-band-9', 'XMI-BAND9', 'Xiaomi', 'smart-watches', 3999, 10, 60, 4.5, 2760, 11, false,
    'Xiaomi Smart Band 9 with 1.62-inch AMOLED display, 21-day battery, 150+ sports modes and 5ATM water resistance.',
    []],
]

for (const p of flagships) {
  const [name, slug, sku, brand, categorySlug, price, discountPercent, stock, rating, ratingCount, daysAgo, isFeatured, description, variants] = p
  if (seed.products.some((x) => x.slug === slug)) continue
  seed.products.push({
    name, slug, sku, brand, description, price, discountPercent, stock, rating, ratingCount,
    categorySlug, isFeatured, isActive: true, createdAtDaysAgo: daysAgo,
    images: [], imageAlts: [],
    variants: (variants ?? []).map(([vName, priceDelta, vStock]) => ({ name: vName, priceDelta, stock: vStock })),
  })
}

// ---- 3. rebuild images ----------------------------------------------------
// Real photos from the sourcing agents' manifests (already downloaded locally).
const manifests = readdirSync(productsDir)
  .filter((f) => f.startsWith('_manifest-') && f.endsWith('.json'))
  .flatMap((f) => JSON.parse(readFileSync(join(productsDir, f), 'utf8')))

const realBySlug = new Map()
for (const entry of manifests) {
  const files = (entry.files ?? []).filter((file) => file.viewed !== false)
  if (files.length > 0) realBySlug.set(entry.slug, files)
}

let realCount = 0
let placeholderCount = 0

for (const product of seed.products) {
  const categoryIndex = seed.categories.findIndex((c) => c.slug === product.categorySlug)
  const tintBase = categoryIndex < 0 ? 0 : categoryIndex % 3

  const real = realBySlug.get(product.slug)
  if (real && real.length > 0) {
    product.images = real.map((f) => `/images/products/${f.file}`)
    product.imageAlts = real.map((f) => f.altText || `${product.name} product view`)
    realCount++
    continue
  }

  // Placeholder art: cycle 3 tint variants per category so galleries differ.
  const tint = (idx) => `/images/placeholders/${product.categorySlug}-${((tintBase + idx) % 3) + 1}.svg`
  product.images = [tint(0), tint(1), tint(2)]
  product.imageAlts = [
    `${product.name} — illustration view 1`,
    `${product.name} — illustration view 2`,
    `${product.name} — illustration view 3`,
  ]
  placeholderCount++
}

writeFileSync(seedPath, JSON.stringify(seed, null, 2) + '\n', 'utf8')
console.log(`Products: ${seed.products.length} (${flagships.length} flagship definitions, ${realCount} with real photos, ${placeholderCount} placeholder art)`)
console.log(`Categories: ${seed.categories.length}`)
