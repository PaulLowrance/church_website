<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const authStore = useAuthStore()
const title = ref('')
const body = ref('')
const isMarkdown = ref(false)
const isPublished = ref(true)
const notFound = ref(false)
const loading = ref(true)

const renderedBody = computed(() => {
  if (isMarkdown.value) {
    return marked(body.value, { async: false }) as string
  }
  return body.value
})

async function loadPage(slug: string) {
  loading.value = true
  notFound.value = false
  try {
    const response = await apiClient.get(`/pages/${slug}`)
    title.value = response.data.title
    body.value = response.data.body
    isMarkdown.value = response.data.isMarkdown
    isPublished.value = response.data.isPublished
  } catch (error: any) {
    if (error.response?.status === 404) {
      notFound.value = true
      title.value = 'Page Not Found'
      body.value = '<p>The page you are looking for does not exist.</p>'
      isMarkdown.value = false
      isPublished.value = true
    } else {
      console.error('Failed to load page', error)
    }
  } finally {
    loading.value = false
  }
}

// Load on mount and whenever the slug route param changes
loadPage(route.params.slug as string || 'home')
watch(() => route.params.slug, (newSlug) => {
  loadPage(newSlug as string || 'home')
})
</script>

<template>
  <q-page padding>
    <q-banner v-if="!isPublished && authStore.userRole === 'Admin'" class="bg-orange text-white q-mb-md">
      <template v-slot:avatar>
        <q-icon name="visibility_off" color="white" />
      </template>
      This page has not been made public yet. Visitors will see a 404 until you publish it.
    </q-banner>

    <q-card v-if="!loading">
      <q-card-section>
        <div class="text-h4">{{ title }}</div>
      </q-card-section>
      <q-separator />
      <q-card-section>
        <div v-html="renderedBody"></div>
      </q-card-section>
    </q-card>
  </q-page>
</template>
