// Mirrors of the QuickShop API DTOs (camelCase JSON).

// ---- auth -----------------------------------------------------------------
export interface AuthUser {
  id: string
  email: string
  fullName: string
  phoneNumber: string | null
  roles: string[]
}

export interface AuthResponse {
  user: AuthUser
  expiresAtUtc: string
}

export interface RegisterRequest {
  fullName: string
  email: string
  password: string
  confirmPassword: string
  acceptTerms: boolean
}

export interface LoginRequest {
  email: string
  password: string
  rememberMe: boolean
}

export interface ForgotPasswordResponse {
  message: string
  devResetToken?: string | null
}

// ---- catalog --------------------------------------------------------------
export interface ProductQuery {
  search?: string
  categoryId?: number
  categorySlug?: string
  brand?: string
  minPrice?: number
  maxPrice?: number
  inStockOnly?: boolean
  featured?: boolean
  sort?: 'newest' | 'price-asc' | 'price-desc' | 'rating' | 'popularity'
  page?: number
  pageSize?: number
  includeInactive?: boolean
}

export interface ProductSummary {
  id: number
  name: string
  slug: string
  sku: string
  brand: string
  price: number
  discountPercent: number
  salePrice: number
  imageUrl: string
  categoryId: number
  categoryName: string
  categorySlug: string
  rating: number
  ratingCount: number
  stock: number
  isAvailable: boolean
  isFeatured: boolean
  isActive: boolean
  createdAt: string
}

export interface ProductImage {
  id: number
  url: string
  sortOrder: number
}

export interface ProductVariant {
  id: number
  name: string
  priceDelta: number
  effectivePrice: number
  stock: number
  isAvailable: boolean
}

export interface ProductDetail extends ProductSummary {
  description: string
  images: ProductImage[]
  variants: ProductVariant[]
}

export interface PriceRange {
  min: number
  max: number
}

export interface CategoryFacet {
  id: number
  name: string
  slug: string
  productCount: number
}

export interface ProductFilters {
  brands: string[]
  priceRange: PriceRange
  categories: CategoryFacet[]
  totalProducts: number
}

export interface Category {
  id: number
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  productCount: number
  isActive: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ---- cart -----------------------------------------------------------------
export interface CartItem {
  id: number
  productId: number
  variantId: number | null
  name: string
  slug: string
  brand: string
  sku: string
  imageUrl: string
  variantName: string | null
  unitPrice: number
  mrp: number
  discountPercent: number
  quantity: number
  lineTotal: number
  stock: number
  maxQuantity: number
  isAvailable: boolean
}

export interface Cart {
  id: string
  owner: 'guest' | 'user'
  items: CartItem[]
  itemCount: number
  subtotal: number
  estimatedShipping: number
  total: number
  isEmpty: boolean
}

// ---- addresses ------------------------------------------------------------
export interface Address {
  id: number
  fullName: string
  phone: string
  line1: string
  line2: string | null
  city: string
  state: string
  postalCode: string
  country: string
  isDefault: boolean
}

export interface AddressUpsert {
  fullName: string
  phone: string
  line1: string
  line2?: string | null
  city: string
  state: string
  postalCode: string
  country?: string
  isDefault?: boolean
}

// ---- orders ---------------------------------------------------------------
export type OrderStatus =
  | 'Pending'
  | 'Paid'
  | 'Processing'
  | 'Shipped'
  | 'Delivered'
  | 'Cancelled'

export interface OrderSummary {
  id: number
  orderNumber: string
  status: OrderStatus
  subtotal: number
  shippingCost: number
  total: number
  placedAt: string
  itemCount: number
  thumbnailUrl: string | null
  customerName: string | null
  customerEmail: string | null
}

export interface OrderItem {
  id: number
  productId: number | null
  productName: string
  slug: string
  sku: string
  imageUrl: string | null
  variantName: string | null
  unitPrice: number
  quantity: number
  lineTotal: number
}

export interface PaymentInfo {
  provider: string
  status: string
  transactionId: string | null
  cardLast4: string | null
  failureReason: string | null
  amount: number
  processedAt: string | null
}

export interface OrderDetail extends OrderSummary {
  shippingMethod: string
  paymentMethod: string
  customerNote: string | null
  shipFullName: string
  shipPhone: string
  shipLine1: string
  shipLine2: string | null
  shipCity: string
  shipState: string
  shipPostalCode: string
  shipCountry: string
  items: OrderItem[]
  payment: PaymentInfo | null
}

export interface CheckoutRequest {
  addressId?: number | null
  newAddress?: AddressUpsert | null
  shippingMethod: 'standard' | 'express'
  paymentMethod: 'mock_card'
  /** Only last-4 is ever sent to the server; PAN/CVV are never transmitted. */
  cardLast4?: string | null
  customerNote?: string | null
}

export interface CheckoutResponse {
  order: OrderDetail
  payment: PaymentInfo
}

// ---- wishlist -------------------------------------------------------------
/** Wishlist items are ProductSummary objects returned in an { items } wrapper. */
export type WishlistResponse = { items: ProductSummary[] }

// ---- admin ----------------------------------------------------------------
export interface DashboardStats {
  totalRevenue: number
  totalOrders: number
  totalCustomers: number
  totalProducts: number
  pendingOrders: number
  lowStockProducts: number
  revenueLast7Days: RevenuePoint[]
  topCategories: CategoryRevenue[]
  recentOrders: OrderSummary[]
  lowStockItems: LowStockItem[]
}

export interface RevenuePoint {
  date: string
  revenue: number
  orders: number
}

export interface CategoryRevenue {
  categoryName: string
  revenue: number
  orderCount: number
}

export interface LowStockItem {
  productId: number
  name: string
  sku: string
  stock: number
  categoryName: string
}

export interface AdminProductUpsert {
  name: string
  brand: string
  description: string
  price: number
  discountPercent: number
  stock: number
  rating: number
  ratingCount: number
  categoryId: number
  imageUrl: string
  images: string[]
  isFeatured: boolean
  isActive: boolean
  sku?: string | null
  variants?: VariantUpsert[] | null
}

export interface VariantUpsert {
  name: string
  priceDelta: number
  stock: number
  isActive: boolean
}

export interface AdminCustomer {
  id: string
  fullName: string
  email: string
  phoneNumber: string | null
  createdAt: string
  orderCount: number
  totalSpent: number
  roles: string[]
}

// ---- errors ---------------------------------------------------------------
export interface ApiProblem {
  status: number
  title: string
  detail: string
  errors?: Record<string, string[]>
  orderNumber?: string
  providerReason?: string
}
