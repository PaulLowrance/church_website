<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '@/api/client'

interface NavItem {
  slug: string
  navTitle: string
}

const churchName = ref('')
const navItems = ref<NavItem[]>([])

onMounted(async () => {
  try {
    const [siteRes, navRes] = await Promise.all([
      apiClient.get('/site-info'),
      apiClient.get('/pages/nav')
    ])
    churchName.value = siteRes.data.churchName
    navItems.value = navRes.data
  } catch (error) {
    console.error('Failed to load footer data', error)
  }
})
</script>

<template>
  <footer class="site-footer">
    <div class="site-footer__inner">
      <div class="site-footer__brand">
        <img
          src="/uploads/images/bhpbc-header-footer.svg"
          alt="Brentwood Hills Primitive Baptist Church"
          class="site-footer__logo"
        />
        <p class="site-footer__name">{{ churchName }}</p>
        <p class="site-footer__tag">A Primitive Baptist Church</p>
      </div>

      <nav class="site-footer__nav" aria-label="Footer navigation">
        <router-link to="/" class="site-footer__link">Home</router-link>
        <router-link
          v-for="item in navItems"
          :key="item.slug"
          :to="`/${item.slug}`"
          class="site-footer__link"
        >
          {{ item.navTitle }}
        </router-link>
        <router-link to="/podcast" class="site-footer__link">Sermons</router-link>
      </nav>

      <p class="site-footer__legal">
        © {{ new Date().getFullYear() }} {{ churchName || 'Brentwood Hills Primitive Baptist Church' }}
      </p>
    </div>
  </footer>
</template>

<style scoped>
.site-footer {
  background: var(--panel);
  border-top: 1px solid var(--rule);
  padding: var(--space-8) var(--space-4);
  margin-top: var(--space-8);
}

.site-footer__inner {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-4);
  text-align: center;
}

.site-footer__logo {
  height: 48px;
  width: auto;
  margin-bottom: var(--space-2);
}

.site-footer__name {
  font-family: var(--heading);
  font-size: 1.25rem;
  color: var(--ink);
  margin: 0;
}

.site-footer__tag {
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--muted);
  margin: 0;
}

.site-footer__nav {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: var(--space-5);
}

.site-footer__link {
  font-family: var(--sans);
  font-size: 0.9375rem;
  color: var(--ink-soft);
  text-decoration: none;
}

.site-footer__link:hover {
  color: var(--accent-burgundy);
  text-decoration: underline;
  text-decoration-color: var(--accent-gold);
}

.site-footer__legal {
  font-family: var(--sans);
  font-size: 0.8125rem;
  color: var(--muted);
  margin: 0;
}
</style>