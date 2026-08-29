<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSeoMeta, useHead } from '@unhead/vue'
import apiClient from '@/api/client'
import { useSiteInfo, usePodcastEpisodeJsonLd } from '@/composables/useJsonLd'

const SITE_URL = import.meta.env.VITE_SITE_URL || 'https://bhpbc.org'
const SITE_NAME = 'Brentwood Hills Primitive Baptist Church'

interface PodcastEpisode {
  id: string
  title: string
  speakerTitle: string | null
  speakerName: string
  speakerDisplay: string
  description: string | null
  seriesName: string | null
  audioUrl: string
  audioFileName: string
  coverImageUrl: string | null
  audioFileSize: number
  audioContentType: string
  publishedAt: string
  createdAt: string
  transcriptStatus: string
  transcriptUrl: string | null
  transcriptError: string | null
  tags: string[]
}

const route = useRoute()
const router = useRouter()
const episodeId = route.params.id as string

const episode = ref<PodcastEpisode | null>(null)
const loading = ref(true)
const error = ref('')
const transcriptOpen = ref(false)
const transcriptText = ref('')
const transcriptLoading = ref(false)
const transcriptLoadError = ref('')

const { siteInfo, loadSiteInfo } = useSiteInfo()
usePodcastEpisodeJsonLd(episode, siteInfo)

const pageTitle = computed(() => episode.value ? episode.value.title : 'Sermon')
const pageDescription = computed(() => {
  if (!episode.value) return ''
  return episode.value.description
    ? episode.value.description.slice(0, 160)
    : `Listen to ${episode.value.title} from ${SITE_NAME}.`
})
const canonicalUrl = computed(() => `${SITE_URL}/sermon/${episodeId}`)
const coverImageUrl = computed(() => episode.value?.coverImageUrl)

useSeoMeta({
  title: () => `${pageTitle.value} | ${SITE_NAME}`,
  description: () => pageDescription.value,
  ogTitle: () => `${pageTitle.value} | ${SITE_NAME}`,
  ogDescription: () => pageDescription.value,
  ogType: 'article',
  ogUrl: () => canonicalUrl.value,
  ogImage: () => coverImageUrl.value || `${SITE_URL}/uploads/images/default-og.jpg`,
  twitterCard: 'summary_large_image'
})

useHead({
  link: () => [
    { rel: 'canonical', href: canonicalUrl.value }
  ]
})

async function toggleTranscript() {
  if (!episode.value?.transcriptUrl) return
  transcriptOpen.value = !transcriptOpen.value
  if (transcriptOpen.value && !transcriptText.value && !transcriptLoading.value) {
    transcriptLoading.value = true
    transcriptLoadError.value = ''
    try {
      const response = await fetch(episode.value.transcriptUrl)
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      transcriptText.value = await response.text()
    } catch (err) {
      transcriptLoadError.value = 'Failed to load transcript.'
      console.error('Failed to load transcript', err)
    } finally {
      transcriptLoading.value = false
    }
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

onMounted(async () => {
  loadSiteInfo()
  try {
    const response = await apiClient.get(`/podcast/episodes/${episodeId}`)
    episode.value = response.data
  } catch (err) {
    error.value = 'Failed to load sermon. Please try again later.'
    console.error(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="sermon-detail">
      <div v-if="loading" class="sermon-detail__loading" aria-live="polite">
        <p>Loading sermon…</p>
      </div>

      <div v-else-if="error" class="sermon-detail__error" role="alert">
        <p>{{ error }}</p>
        <button class="btn" @click="router.push('/podcast')">Back to Sermons</button>
      </div>

      <article v-else-if="episode" class="sermon-detail__article">
        <header class="sermon-detail__header">
          <div v-if="episode.seriesName" class="sermon-detail__eyebrow">
            {{ episode.seriesName }}
          </div>
          <h1 class="sermon-detail__title">{{ episode.title }}</h1>
          <p class="sermon-detail__meta">
            <span class="sermon-detail__speaker">{{ episode.speakerDisplay }}</span>
            <span class="sermon-detail__separator" aria-hidden="true">·</span>
            <time class="sermon-detail__date" :datetime="episode.publishedAt">
              {{ formatDate(episode.publishedAt) }}
            </time>
          </p>
        </header>

        <div
          v-if="episode.coverImageUrl"
          class="sermon-detail__cover"
        >
          <img
            :src="episode.coverImageUrl"
            :alt="episode.title"
            class="sermon-detail__cover-image"
            loading="eager"
            decoding="async"
          />
        </div>

        <div class="sermon-detail__audio">
          <audio
            controls
            preload="none"
            class="sermon-detail__player"
            :aria-label="`Audio player for ${episode.title}`"
          >
            <source :src="episode.audioUrl" :type="episode.audioContentType" />
            Your browser does not support the audio element.
          </audio>
          <div class="sermon-detail__audio-actions">
            <a
              class="btn btn--secondary"
              :href="episode.audioUrl"
              :download="episode.audioFileName"
              target="_blank"
            >
              Download audio ({{ formatFileSize(episode.audioFileSize) }})
            </a>
          </div>
        </div>

        <div v-if="episode.description" class="sermon-detail__description prose">
          <p>{{ episode.description }}</p>
        </div>

        <div class="sermon-detail__transcript">
          <button
            v-if="episode.transcriptUrl"
            class="btn"
            :aria-expanded="transcriptOpen"
            @click="toggleTranscript"
          >
            {{ transcriptOpen ? 'Hide Transcript' : 'Read Transcript' }}
          </button>

          <div v-if="transcriptOpen" class="sermon-detail__transcript-body">
            <p v-if="transcriptLoading" class="text-muted">Loading transcript…</p>
            <p v-else-if="transcriptLoadError" class="sermon-detail__error" role="alert">
              {{ transcriptLoadError }}
            </p>
            <pre v-else-if="transcriptText" class="sermon-detail__transcript-text">{{ transcriptText }}</pre>
          </div>
        </div>

        <div v-if="episode.tags.length > 0" class="sermon-detail__tags">
          <span class="text-muted">Topics:</span>
          <ul class="tag-list" role="list">
            <li v-for="tag in episode.tags" :key="tag" class="tag-list__item">
              {{ tag }}
            </li>
          </ul>
        </div>
      </article>
    </div>
</template>

<style scoped>
.sermon-detail {
  max-width: var(--measure-wide);
  margin: 0 auto;
  padding: var(--space-6) var(--space-4);
}

.sermon-detail__loading,
.sermon-detail__error {
  text-align: center;
  padding: var(--space-8) 0;
}

.sermon-detail__article {
  display: flex;
  flex-direction: column;
  gap: var(--space-6);
}

.sermon-detail__header {
  text-align: center;
}

.sermon-detail__eyebrow {
  font-family: var(--sans);
  font-size: 0.875rem;
  font-weight: 600;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--accent-gold);
  margin-bottom: var(--space-2);
}

.sermon-detail__title {
  font-family: var(--heading);
  font-size: clamp(2rem, 5vw, 3rem);
  line-height: 1.1;
  margin: 0 0 var(--space-3);
  color: var(--ink);
}

.sermon-detail__meta {
  font-family: var(--sans);
  font-size: 1rem;
  color: var(--muted);
  margin: 0;
}

.sermon-detail__speaker {
  font-weight: 500;
}

.sermon-detail__separator {
  margin: 0 var(--space-2);
}

.sermon-detail__cover {
  width: 100%;
  max-height: 420px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--panel);
  border: 1px solid var(--rule);
  border-radius: 12px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

.sermon-detail__cover-image {
  max-width: 100%;
  max-height: 420px;
  width: auto;
  height: auto;
  object-fit: contain;
  display: block;
}

.sermon-detail__audio {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  padding: var(--space-4);
  background: var(--panel);
  border-radius: 12px;
  border: 1px solid var(--rule);
}

.sermon-detail__player {
  width: 100%;
}

.sermon-detail__audio-actions {
  display: flex;
  justify-content: flex-end;
}

.sermon-detail__description {
  font-family: var(--heading);
  font-size: 1.125rem;
  line-height: 1.7;
  color: var(--ink-soft);
}

.sermon-detail__description p {
  max-width: var(--measure);
  margin: 0 auto;
}

.sermon-detail__transcript {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
  align-items: flex-start;
}

.sermon-detail__transcript-body {
  width: 100%;
}

.sermon-detail__transcript-text {
  width: 100%;
  max-width: var(--measure-wide);
  margin: 0;
  padding: var(--space-4);
  background: var(--panel);
  border: 1px solid var(--rule);
  border-radius: 12px;
  font-family: var(--sans);
  font-size: 1rem;
  line-height: 1.7;
  white-space: pre-wrap;
  color: var(--ink);
  overflow-x: auto;
}

.sermon-detail__tags {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-2);
  font-family: var(--sans);
  font-size: 0.875rem;
}

.tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-2);
  list-style: none;
  margin: 0;
  padding: 0;
}

.tag-list__item {
  padding: 0.25rem 0.75rem;
  border: 1px solid var(--rule);
  border-radius: 9999px;
  color: var(--muted);
  background: var(--panel);
}

.btn {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1.25rem;
  border: 1px solid var(--accent-burgundy);
  border-radius: 8px;
  background: var(--accent-burgundy);
  color: #fff;
  font-family: var(--sans);
  font-size: 0.9375rem;
  font-weight: 500;
  text-decoration: none;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.btn:hover {
  background: var(--accent-gold);
  border-color: var(--accent-gold);
  color: var(--ink);
}

.btn:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

.btn--secondary {
  background: transparent;
  color: var(--accent-burgundy);
}

.btn--secondary:hover {
  background: var(--accent-gold-soft);
  border-color: var(--accent-gold);
  color: var(--ink);
}

.text-muted {
  color: var(--muted);
}
</style>
