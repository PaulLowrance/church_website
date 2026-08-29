<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta, useHead } from '@unhead/vue'
import { marked } from 'marked'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

const SITE_URL = import.meta.env.VITE_SITE_URL || 'https://bhpbc.org'
const SITE_NAME = 'Brentwood Hills Primitive Baptist Church'
const DEFAULT_DESCRIPTION = 'Primitive Baptist church in East Fort Worth, Texas. Sermons, service times, and contact information.'

interface LatestSermon {
  id: string
  title: string
  speakerDisplay: string
  description: string | null
  audioUrl: string
  transcriptUrl: string | null
  publishedAt: string
}

function stripHtml(html: string): string {
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim()
}

function truncateDescription(text: string): string {
  if (text.length <= 160) return text
  return text.slice(0, 157).trim() + '...'
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

const route = useRoute()
const authStore = useAuthStore()
const title = ref('')
const body = ref('')
const contentType = ref('wysiwyg')
const isPublished = ref(true)
const notFound = ref(false)
const loading = ref(true)
const latestSermon = ref<LatestSermon | null>(null)
const sermonLoading = ref(false)

const isHomePage = computed(() => {
  const slug = route.params.slug as string | undefined
  return route.path === '/' || slug === 'home'
})

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

async function loadLatestSermon() {
  if (!isHomePage.value) {
    latestSermon.value = null
    return
  }
  sermonLoading.value = true
  try {
    const response = await apiClient.get('/podcast/episodes')
    const episodes = response.data as LatestSermon[]
    latestSermon.value = episodes.length > 0 ? episodes[0] : null
  } catch (err) {
    console.error('Failed to load latest sermon for hero', err)
    latestSermon.value = null
  } finally {
    sermonLoading.value = false
  }
}

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
      body.value = ''
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
loadLatestSermon()
watch(() => route.params.slug, (newSlug) => {
  loadPage(newSlug as string || 'home')
  loadLatestSermon()
})
</script>

<template>
  <div class="home-page">
      <div
        v-if="!isPublished && authStore.userRole === 'Admin'"
        class="unpublished-banner"
        role="alert"
      >
        This page has not been made public yet. Visitors will see a 404 until you publish it.
      </div>

      <section v-if="isHomePage && !sermonLoading" class="hero" aria-labelledby="hero-title">
        <p id="hero-eyebrow" class="hero__eyebrow">This Sunday's Sermon</p>
        <h1 id="hero-title" class="hero__title">
          {{ latestSermon ? latestSermon.title : 'Welcome' }}
        </h1>
        <p v-if="latestSermon" class="hero__subtitle">
          <span class="hero__speaker">{{ latestSermon.speakerDisplay }}</span>
          <span class="hero__separator" aria-hidden="true">·</span>
          <time class="hero__date" :datetime="latestSermon.publishedAt">
            {{ formatDate(latestSermon.publishedAt) }}
          </time>
        </p>
        <p
          v-if="latestSermon?.description"
          class="hero__summary"
        >
          {{ latestSermon.description.slice(0, 200) }}{{ latestSermon.description.length > 200 ? '…' : '' }}
        </p>
        <div class="hero__actions">
          <router-link
            v-if="latestSermon"
            class="btn btn--primary"
            :to="`/sermon/${latestSermon.id}`"
          >
            Listen
          </router-link>
          <router-link
            v-if="latestSermon?.transcriptUrl"
            class="btn btn--secondary"
            :to="`/sermon/${latestSermon.id}`"
          >
            Read Transcript
          </router-link>
          <router-link
            v-if="!latestSermon"
            class="btn btn--secondary"
            to="/podcast"
          >
            Browse Sermons
          </router-link>
        </div>
      </section>

      <section v-if="!loading" class="page-content" :class="{ 'page-content--hero': isHomePage }">
        <header v-if="!isHomePage" class="page-content__header">
          <h1 class="page-content__title">{{ title }}</h1>
        </header>

        <div
          v-if="notFound"
          class="page-content__not-found"
          role="alert"
        >
          <h1 class="page-content__title">Page Not Found</h1>
          <p>Sorry, we couldn't find the page you were looking for.</p>
          <router-link class="btn btn--secondary" to="/">
            Return home
          </router-link>
        </div>

        <div
          v-else
          class="page-content__body"
          v-html="renderedBody"
        />
      </section>
    </div>
</template>

<style scoped>
.home-page {
  max-width: 1200px;
  margin: 0 auto;
  padding: var(--space-6) var(--space-4);
}

.unpublished-banner {
  background: var(--accent-gold-soft);
  border: 1px solid var(--accent-gold);
  color: var(--ink);
  padding: var(--space-3) var(--space-4);
  border-radius: 8px;
  margin-bottom: var(--space-4);
  font-family: var(--sans);
  font-size: 0.9375rem;
}

.hero {
  text-align: center;
  padding: var(--space-8) var(--space-4);
  margin-bottom: var(--space-8);
  background: linear-gradient(135deg, var(--paper) 0%, var(--panel) 100%);
  border: 1px solid var(--rule);
  border-radius: 16px;
  box-shadow: var(--shadow);
}

.hero__eyebrow {
  font-family: var(--sans);
  font-size: 0.875rem;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--accent-gold);
  margin: 0 0 var(--space-2);
}

.hero__title {
  font-family: var(--heading);
  font-size: clamp(2.25rem, 6vw, 4rem);
  line-height: 1.1;
  margin: 0 0 var(--space-3);
  color: var(--ink);
}

.hero__subtitle {
  font-family: var(--sans);
  font-size: 1.125rem;
  color: var(--muted);
  margin: 0 0 var(--space-4);
}

.hero__speaker {
  font-weight: 500;
}

.hero__separator {
  margin: 0 var(--space-2);
}

.hero__summary {
  font-family: var(--heading);
  font-size: 1.25rem;
  line-height: 1.6;
  color: var(--ink-soft);
  max-width: var(--measure);
  margin: 0 auto var(--space-5);
}

.hero__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: var(--space-3);
}

.page-content {
  max-width: var(--measure-wide);
  margin: 0 auto;
}

.page-content--hero {
  padding-top: var(--space-4);
}

.page-content__header {
  margin-bottom: var(--space-5);
  padding-bottom: var(--space-4);
  border-bottom: 1px solid var(--rule);
}

.page-content__title {
  font-family: var(--heading);
  font-size: clamp(2rem, 5vw, 3rem);
  margin: 0;
  color: var(--ink);
}

.page-content__not-found {
  text-align: center;
  padding: var(--space-8) 0;
}

.page-content__not-found p {
  color: var(--muted);
  margin-bottom: var(--space-4);
}

.page-content__body {
  font-family: var(--sans);
  font-size: 1.125rem;
  line-height: 1.7;
  color: var(--ink);
}

.page-content__body :deep(h1),
.page-content__body :deep(h2),
.page-content__body :deep(h3),
.page-content__body :deep(h4),
.page-content__body :deep(h5),
.page-content__body :deep(h6) {
  font-family: var(--heading);
  color: var(--ink);
  line-height: 1.2;
  margin: var(--space-6) 0 var(--space-3);
}

.page-content__body :deep(p) {
  max-width: var(--measure);
  margin: 0 0 var(--space-4);
}

.page-content__body :deep(img) {
  max-width: 100%;
  height: auto;
  display: block;
  margin: var(--space-4) 0;
  border-radius: 12px;
}

.page-content__body :deep(a) {
  color: var(--accent-burgundy);
  text-decoration: underline;
  text-decoration-color: var(--accent-gold);
  text-underline-offset: 0.15em;
}

.page-content__body :deep(a:hover) {
  color: var(--accent-gold);
}

.page-content__body :deep(ul),
.page-content__body :deep(ol) {
  max-width: var(--measure);
  margin: 0 0 var(--space-4);
  padding-left: var(--space-5);
}

.page-content__body :deep(li) {
  margin-bottom: var(--space-2);
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border-radius: 8px;
  font-family: var(--sans);
  font-size: 1rem;
  font-weight: 500;
  text-decoration: none;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease, color 0.2s ease;
}

.btn--primary {
  border: 1px solid var(--accent-burgundy);
  background: var(--accent-burgundy);
  color: #fff;
}

.btn--primary:hover {
  background: var(--accent-gold);
  border-color: var(--accent-gold);
  color: var(--ink);
}

.btn--secondary {
  border: 1px solid var(--rule);
  background: transparent;
  color: var(--accent-burgundy);
}

.btn--secondary:hover {
  background: var(--accent-gold-soft);
  border-color: var(--accent-gold);
}

.btn:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

@media (max-width: 600px) {
  .hero {
    padding: var(--space-6) var(--space-3);
  }

  .hero__actions {
    flex-direction: column;
    align-items: stretch;
  }

  .btn {
    justify-content: center;
  }
}
</style>
