<template>
  <q-layout view="hHh lpR fFf">
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
import SkipToMain from '@/components/SkipToMain.vue'

const route = useRoute()
const isNoIndex = computed(() => !!route.meta.noindex)

useSeoMeta({
  robots: () => (isNoIndex.value ? 'noindex, nofollow' : 'index, follow')
})
</script>
