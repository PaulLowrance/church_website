<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta, useHead } from '@unhead/vue'
import apiClient from '@/api/client'

const SITE_URL = import.meta.env.VITE_SITE_URL || 'https://bhpbc.org'
const SITE_NAME = 'Brentwood Hills Primitive Baptist Church'

interface PodcastEpisode {
  id: string
  title: string
  speakerDisplay: string
  description: string | null
  seriesName: string | null
  audioUrl: string
  audioFileName: string
  coverImageUrl: string | null
  audioContentType: string
  publishedAt: string
}

const route = useRoute()
const episodes = ref<PodcastEpisode[]>([])
const loading = ref(false)
const error = ref('')
const churchName = ref('')

const pageTitle = computed(() => 'Sermons')
const pageDescription = computed(() => {
  const name = churchName.value || SITE_NAME
  return `Listen to recent sermons from ${name}, a Primitive Baptist church in East Fort Worth, Texas.` + (episodes.value.length > 0
    ? ` Featuring ${episodes.value.length} sermon${episodes.value.length === 1 ? '' : 's'}.`
    : '')
})
const canonicalUrl = computed(() => `${SITE_URL}${route.path}`)

useSeoMeta({
  title: () => `${pageTitle.value} | ${SITE_NAME}`,
  description: () => pageDescription.value,
  ogTitle: () => `${pageTitle.value} | ${SITE_NAME}`,
  ogDescription: () => pageDescription.value,
  ogType: 'website',
  ogUrl: () => canonicalUrl.value,
  twitterCard: 'summary_large_image'
})

useHead({
  link: () => [
    { rel: 'canonical', href: canonicalUrl.value }
  ]
})

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

onMounted(async () => {
  loading.value = true
  try {
    const [episodesRes, siteRes] = await Promise.all([
      apiClient.get('/podcast/episodes'),
      apiClient.get('/site-info')
    ])
    episodes.value = episodesRes.data
    churchName.value = siteRes.data.churchName
  } catch (err) {
    error.value = 'Failed to load sermons. Please try again later.'
    console.error(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <q-page padding>
    <main id="main" class="sermon-list">
      <header class="sermon-list__header">
        <h1 class="sermon-list__title">Sermons</h1>
        <p class="sermon-list__intro">
          Listen to recent sermons from {{ churchName }}.
          <a class="sermon-list__subscribe" href="/podcast/rss" target="_blank" rel="noopener">
            Subscribe to the sermon feed (RSS)
          </a>
        </p>
      </header>

      <q-inner-loading v-if="loading" showing color="primary" label="Loading episodes..." />

      <q-banner v-if="error" class="bg-negative text-white q-mb-md" role="alert">
        <template v-slot:avatar>
          <q-icon name="error" color="white" />
        </template>
        {{ error }}
      </q-banner>

      <div v-if="!loading && episodes.length === 0 && !error" class="sermon-list__empty">
        <p>No episodes available yet.</p>
      </div>

      <div v-if="!loading && episodes.length > 0" class="sermon-grid">
        <article
          v-for="episode in episodes"
          :key="episode.id"
          class="sermon-card"
        >
          <router-link
            :to="`/sermon/${episode.id}`"
            class="sermon-card__media"
            :aria-label="`Open ${episode.title}`"
          >
            <img
              v-if="episode.coverImageUrl"
              :src="episode.coverImageUrl"
              :alt="episode.title"
              class="sermon-card__image"
              loading="lazy"
              decoding="async"
            />
            <div v-else class="sermon-card__placeholder">
              <span class="sermon-card__placeholder-text">{{ episode.title }}</span>
            </div>
            <span v-if="episode.seriesName" class="sermon-card__series">
              {{ episode.seriesName }}
            </span>
          </router-link>

          <div class="sermon-card__body">
            <h2 class="sermon-card__title">
              <router-link :to="`/sermon/${episode.id}`">
                {{ episode.title }}
              </router-link>
            </h2>

            <p class="sermon-card__meta">
              <span class="sermon-card__speaker">{{ episode.speakerDisplay }}</span>
              <span class="sermon-card__separator" aria-hidden="true">·</span>
              <time class="sermon-card__date" :datetime="episode.publishedAt">
                {{ formatDate(episode.publishedAt) }}
              </time>
            </p>

            <p v-if="episode.description" class="sermon-card__description">
              {{ episode.description.slice(0, 140) }}{{ episode.description.length > 140 ? '…' : '' }}
            </p>

            <div class="sermon-card__actions">
              <audio
                controls
                preload="none"
                class="sermon-card__audio"
                :aria-label="`Audio player for ${episode.title}`"
              >
                <source :src="episode.audioUrl" :type="episode.audioContentType" />
                Your browser does not support the audio element.
              </audio>
              <a
                class="sermon-card__download"
                :href="episode.audioUrl"
                :download="episode.audioFileName"
                target="_blank"
                :aria-label="`Download ${episode.title}`"
                title="Download audio"
              >
                ↓
              </a>
            </div>
          </div>
        </article>
      </div>
    </main>
  </q-page>
</template>

<style scoped>
.sermon-list {
  max-width: var(--measure-wide);
  margin: 0 auto;
  padding: var(--space-6) var(--space-4);
}

.sermon-list__header {
  margin-bottom: var(--space-6);
  text-align: center;
}

.sermon-list__title {
  font-family: var(--heading);
  font-size: clamp(2rem, 5vw, 3rem);
  margin: 0 0 var(--space-3);
  color: var(--ink);
}

.sermon-list__intro {
  font-family: var(--sans);
  font-size: 1.125rem;
  color: var(--muted);
  max-width: var(--measure);
  margin: 0 auto;
}

.sermon-list__subscribe {
  display: inline;
  margin-left: 0.25rem;
}

.sermon-list__empty {
  text-align: center;
  padding: var(--space-8) 0;
  color: var(--muted);
}

.sermon-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--space-5);
}

@media (min-width: 768px) {
  .sermon-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

.sermon-card {
  display: flex;
  flex-direction: column;
  background: var(--panel);
  border: 1px solid var(--rule);
  border-radius: 12px;
  overflow: hidden;
  box-shadow: var(--shadow);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.sermon-card:hover {
  transform: translateY(-2px);
  box-shadow:
    rgba(0, 0, 0, 0.12) 0 14px 20px -3px,
    rgba(0, 0, 0, 0.06) 0 6px 8px -2px;
}

.sermon-card__media {
  position: relative;
  display: block;
  width: 100%;
  aspect-ratio: 1 / 1;
  overflow: hidden;
  background: var(--rule);
  text-decoration: none;
}

.sermon-card__image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
  transition: transform 0.3s ease;
}

.sermon-card__media:hover .sermon-card__image {
  transform: scale(1.03);
}

.sermon-card__placeholder {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: var(--space-4);
  text-align: center;
  background: linear-gradient(135deg, var(--rule) 0%, var(--panel) 100%);
  color: var(--muted);
}

.sermon-card__placeholder-text {
  font-family: var(--heading);
  font-size: 1.25rem;
  line-height: 1.2;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.sermon-card__series {
  position: absolute;
  bottom: var(--space-3);
  left: var(--space-3);
  padding: 0.25rem 0.75rem;
  background: rgba(0, 0, 0, 0.6);
  color: #fff;
  font-family: var(--sans);
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  border-radius: 9999px;
}

.sermon-card__body {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding: var(--space-4);
  flex: 1;
}

.sermon-card__title {
  font-family: var(--heading);
  font-size: 1.25rem;
  line-height: 1.25;
  margin: 0;
}

.sermon-card__title a {
  color: var(--ink);
  text-decoration: none;
}

.sermon-card__title a:hover {
  color: var(--accent-burgundy);
}

.sermon-card__meta {
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--muted);
  margin: 0;
}

.sermon-card__speaker {
  font-weight: 500;
}

.sermon-card__separator {
  margin: 0 var(--space-1);
}

.sermon-card__description {
  font-family: var(--sans);
  font-size: 0.9375rem;
  line-height: 1.6;
  color: var(--ink-soft);
  margin: 0;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.sermon-card__actions {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-top: auto;
  padding-top: var(--space-3);
}

.sermon-card__audio {
  flex: 1;
  min-width: 0;
  height: 40px;
}

.sermon-card__download {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  flex-shrink: 0;
  border: 1px solid var(--rule);
  border-radius: 8px;
  color: var(--accent-burgundy);
  text-decoration: none;
  font-size: 1.25rem;
  transition: background-color 0.2s ease;
}

.sermon-card__download:hover {
  background: var(--accent-gold-soft);
  border-color: var(--accent-gold);
}

audio {
  max-width: 100%;
  width: 100%;
}
</style>
