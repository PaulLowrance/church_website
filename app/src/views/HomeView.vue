<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta, useHead } from '@unhead/vue'
import { marked } from 'marked'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

const SITE_URL = import.meta.env.VITE_SITE_URL || 'https://bhpbc.org'
const SITE_NAME = 'Brentwood Hills Primitive Baptist Church'
const DEFAULT_DESCRIPTION = 'Independent Primitive Baptist church in Brentwood, Tennessee. Sermons, service times, and contact information.'

function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim()
}

function truncateDescription(text: string): string {
  if (text.length <= 160) return text
  return text.slice(0, 157).trim() + '...'
}

const route = useRoute()
const authStore = useAuthStore()
const title = ref('')
const body = ref('')
const contentType = ref('wysiwyg')
const isPublished = ref(true)
const notFound = ref(false)
const loading = ref(true)

const renderedBody = computed(() => {
  if (contentType.value === 'markdown') {
    return marked(body.value, { async: false }) as string
  }
  return body.value
})

const pageTitle = computed(() => {
  if (notFound.value) return 'Page Not Found'
  if (title.value) return title.value
  return ''
})

const pageDescription = computed(() => {
  if (notFound.value) return 'The page you are looking for does not exist.'
  const text = stripHtml(renderedBody.value)
  if (!text) return DEFAULT_DESCRIPTION
  return truncateDescription(text)
})

const canonicalUrl = computed(() => {
  return `${SITE_URL}${route.path}`
})

useSeoMeta({
  title: () => (pageTitle.value ? `${pageTitle.value} | ${SITE_NAME}` : SITE_NAME),
  description: () => pageDescription.value,
  ogTitle: () => (pageTitle.value ? `${pageTitle.value} | ${SITE_NAME}` : SITE_NAME),
  ogDescription: () => pageDescription.value,
  ogType: 'website',
  ogUrl: () => canonicalUrl.value,
  twitterCard: 'summary_large_image',
  robots: () => (notFound.value ? 'noindex, follow' : 'index, follow')
})

useHead({
  link: () => [
    { rel: 'canonical', href: canonicalUrl.value }
  ]
})

async function loadPage(slug: string) {
  loading.value = true
  notFound.value = false
  try {
    const response = await apiClient.get(`/pages/${slug}`)
    title.value = response.data.title
    body.value = response.data.body
    contentType.value = response.data.contentType || 'wysiwyg'
    isPublished.value = response.data.isPublished
  } catch (error: any) {
    if (error.response?.status === 404) {
      notFound.value = true
      title.value = 'Page Not Found'
      body.value = '<p>The page you are looking for does not exist.</p>'
      contentType.value = 'wysiwyg'
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
        <div class="page-content" v-html="renderedBody"></div>
      </q-card-section>
    </q-card>
  </q-page>
</template>

<style scoped>
.page-content :deep(img) {
  max-width: 100%;
  height: auto;
  display: block;
  margin: 1rem 0;
}
</style>
