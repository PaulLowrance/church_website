<template>
  <div v-if="isPublicRoute" class="public-shell">
    <SkipToMain />
    <PublicNav />
    <main id="main" class="main-content">
      <router-view />
    </main>
    <AppFooter />
  </div>

  <q-layout v-else view="hHh lpR fFf">
    <SkipToMain />
    <NavMenu />
    <q-page-container>
      <main id="main" class="main-content">
        <router-view />
      </main>
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta } from '@unhead/vue'
import NavMenu from '@/components/NavMenu.vue'
import PublicNav from '@/components/PublicNav.vue'
import AppFooter from '@/components/AppFooter.vue'
import SkipToMain from '@/components/SkipToMain.vue'

const route = useRoute()
const isPublicRoute = computed(() => !route.meta.noindex)
const isNoIndex = computed(() => !!route.meta.noindex)

useSeoMeta({
  robots: () => (isNoIndex.value ? 'noindex, nofollow' : 'index, follow')
})
</script>