<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { marked } from 'marked'
import apiClient from '@/api/client'

const route = useRoute()
const title = ref('')
const body = ref('')
const isMarkdown = ref(false)
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
  } catch (error: any) {
    if (error.response?.status === 404) {
      notFound.value = true
      title.value = 'Page Not Found'
      body.value = '<p>The page you are looking for does not exist.</p>'
      isMarkdown.value = false
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
