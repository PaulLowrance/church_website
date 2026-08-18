<template>
  <q-layout view="hHh lpR fFf">
    <NavMenu />
    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta } from '@unhead/vue'
import NavMenu from '@/components/NavMenu.vue'

const route = useRoute()
const isNoIndex = computed(() => !!route.meta.noindex)

useSeoMeta({
  robots: () => (isNoIndex.value ? 'noindex, nofollow' : 'index, follow')
})
</script>
