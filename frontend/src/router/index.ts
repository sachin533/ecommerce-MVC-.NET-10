import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // ---- storefront ----
    { path: '/', name: 'home', component: () => import('@/views/HomeView.vue') },
    { path: '/shop', name: 'shop', component: () => import('@/views/ShopView.vue') },
    { path: '/category/:slug', name: 'category', component: () => import('@/views/ShopView.vue') },
    { path: '/deals', name: 'deals', component: () => import('@/views/DealsView.vue') },
    { path: '/new-arrivals', name: 'new-arrivals', component: () => import('@/views/NewArrivalsView.vue') },
    { path: '/brands', name: 'brands', component: () => import('@/views/BrandsView.vue') },
    { path: '/product/:slug', name: 'product', component: () => import('@/views/ProductDetailView.vue') },
    { path: '/cart', name: 'cart', component: () => import('@/views/CartView.vue') },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('@/views/CheckoutView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/order-confirmation/:id',
      name: 'order-confirmation',
      component: () => import('@/views/OrderConfirmationView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders',
      name: 'orders',
      component: () => import('@/views/OrdersView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders/:id',
      name: 'order-detail',
      component: () => import('@/views/OrderDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/wishlist',
      name: 'wishlist',
      component: () => import('@/views/WishlistView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/profile',
      name: 'profile',
      component: () => import('@/views/ProfileView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/addresses',
      name: 'addresses',
      component: () => import('@/views/AddressesView.vue'),
      meta: { requiresAuth: true },
    },

    // ---- auth ----
    { path: '/login', name: 'login', component: () => import('@/views/auth/LoginView.vue'), meta: { guestOnly: true } },
    { path: '/register', name: 'register', component: () => import('@/views/auth/RegisterView.vue'), meta: { guestOnly: true } },
    { path: '/forgot-password', name: 'forgot-password', component: () => import('@/views/auth/ForgotPasswordView.vue'), meta: { guestOnly: true } },
    { path: '/reset-password', name: 'reset-password', component: () => import('@/views/auth/ResetPasswordView.vue'), meta: { guestOnly: true } },

    // ---- admin ----
    {
      path: '/admin',
      component: () => import('@/layouts/AdminLayout.vue'),
      meta: { requiresAuth: true, requiresAdmin: true },
      children: [
        { path: '', name: 'admin-dashboard', component: () => import('@/views/admin/DashboardView.vue') },
        { path: 'products', name: 'admin-products', component: () => import('@/views/admin/ProductsView.vue') },
        { path: 'products/new', name: 'admin-product-new', component: () => import('@/views/admin/ProductFormView.vue') },
        { path: 'products/:id/edit', name: 'admin-product-edit', component: () => import('@/views/admin/ProductFormView.vue') },
        { path: 'categories', name: 'admin-categories', component: () => import('@/views/admin/CategoriesView.vue') },
        { path: 'orders', name: 'admin-orders', component: () => import('@/views/admin/OrdersView.vue') },
        { path: 'orders/:id', name: 'admin-order-detail', component: () => import('@/views/admin/OrderDetailView.vue') },
        { path: 'customers', name: 'admin-customers', component: () => import('@/views/admin/CustomersView.vue') },
      ],
    },

    // ---- fallbacks ----
    { path: '/access-denied', name: 'access-denied', component: () => import('@/views/AccessDeniedView.vue') },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') },
  ],
  scrollBehavior() {
    return { top: 0 }
  },
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Wait for the silent refresh on first navigation.
  if (auth.initializing) await auth.initialize()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return { name: 'access-denied' }
  }

  if (to.meta.guestOnly && auth.isAuthenticated) {
    return { name: 'home' }
  }

  return true
})

export default router
