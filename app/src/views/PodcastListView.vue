<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSeoMeta, useHead } from '@unhead/vue'
import apiClient from '@/api/client'

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
const episodes = ref<PodcastEpisode[]>([])
const loading = ref(false)
const error = ref('')
const churchName = ref('')

const pageTitle = computed(() => 'Sermons')
const pageDescription = computed(() => {
  const name = churchName.value || SITE_NAME
  return `Listen to recent sermons from ${name}, an Independent Primitive Baptist church in East Fort Worth, Texas, near the Brentwood Hills neighborhood.` + (episodes.value.length > 0
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

const transcriptOpen = reactive<Record<string, boolean>>({})
const transcriptText = reactive<Record<string, string>>({})
const transcriptLoading = reactive<Record<string, boolean>>({})
const transcriptLoadError = reactive<Record<string, string>>({})

async function toggleTranscript(episode: PodcastEpisode) {
  if (!episode.transcriptUrl) return
  const id = episode.id
  transcriptOpen[id] = !transcriptOpen[id]
  if (transcriptOpen[id] && !transcriptText[id] && !transcriptLoading[id]) {
    transcriptLoading[id] = true
    transcriptLoadError[id] = ''
    try {
      const response = await fetch(episode.transcriptUrl)
      if (!response.ok) throw new Error(`HTTP ${response.status}`)
      transcriptText[id] = await response.text()
    } catch (err) {
      transcriptLoadError[id] = 'Failed to load transcript.'
      console.error('Failed to load transcript', err)
    } finally {
      transcriptLoading[id] = false
    }
  }
}

function transcriptLabel(status: string): string {
  switch (status) {
    case 'queued':
      return 'Transcription queued…'
    case 'processing':
      return 'Transcribing…'
    case 'completed':
      return 'Transcript ready'
    case 'error':
      return 'Transcription failed'
    default:
      return ''
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
    <div class="q-mb-lg">
      <h1 class="text-h4 q-mb-sm">Sermons</h1>
      <p class="text-body1 text-grey-7">
        Listen to recent sermons from {{ churchName }}.
        You can also subscribe to our sermon audio feed.
      </p>
      <q-btn
        label="Subscribe to Sermons (RSS)"
        color="primary"
        icon="rss_feed"
        href="/podcast/rss"
        target="_blank"
        aria-label="Subscribe to sermons RSS feed"
      />
    </div>

    <q-inner-loading v-if="loading" showing color="primary" label="Loading episodes..." />

    <q-banner v-if="error" class="bg-negative text-white q-mb-md" role="alert">
      <template v-slot:avatar>
        <q-icon name="error" color="white" />
      </template>
      {{ error }}
    </q-banner>

    <div v-if="!loading && episodes.length === 0 && !error" class="text-center q-pa-xl">
      <q-icon name="podcasts" size="4rem" color="grey-4" />
      <p class="text-h6 text-grey-6 q-mt-md">No episodes available yet.</p>
    </div>

    <div class="row q-col-gutter-md">
      <div v-for="episode in episodes" :key="episode.id" class="col-12">
        <q-card>
          <q-card-section>
            <div class="row items-start justify-between">
              <div class="col-grow">
                <div class="text-h6">{{ episode.title }}</div>
                <div class="text-subtitle2 text-grey-7 q-mt-xs">
                  <q-icon name="person" size="xs" class="q-mr-xs" />
                  {{ episode.speakerDisplay }}
                  <span class="q-mx-sm">|</span>
                  <q-icon name="event" size="xs" class="q-mr-xs" />
                  {{ formatDate(episode.publishedAt) }}
                  <span v-if="episode.seriesName" class="q-mx-sm">|</span>
                  <q-chip v-if="episode.seriesName" color="secondary" text-color="white" dense size="sm">
                    {{ episode.seriesName }}
                  </q-chip>
                </div>
              </div>
            </div>

            <p v-if="episode.description" class="q-mt-md text-body2" style="white-space: pre-wrap;">
              {{ episode.description }}
            </p>

            <div v-if="episode.transcriptStatus === 'processing' || episode.transcriptStatus === 'queued'" class="q-mt-sm">
              <q-chip color="amber" text-color="black" dense size="sm" icon="hourglass_top">
                {{ transcriptLabel(episode.transcriptStatus) }}
              </q-chip>
            </div>

            <div v-else-if="episode.transcriptStatus === 'error'" class="q-mt-sm">
              <q-chip color="negative" text-color="white" dense size="sm" icon="error" :label="episode.transcriptError ? 'Transcription failed' : transcriptLabel(episode.transcriptStatus)" />
            </div>

            <div v-else-if="episode.transcriptUrl" class="q-mt-sm">
              <div class="row items-center q-gutter-sm">
                <q-btn
                  flat
                  dense
                  color="primary"
                  icon="article"
                  :label="transcriptOpen[episode.id] ? 'Hide Transcript' : 'View Transcript'"
                  @click="toggleTranscript(episode)"
                  :aria-expanded="!!transcriptOpen[episode.id]"
                  aria-label="View transcript"
                />
                <q-btn
                  flat
                  dense
                  color="primary"
                  icon="download"
                  label="Download Transcript"
                  :href="episode.transcriptUrl"
                  :download="`${episode.title.replace(/[^\w]+/g, '_')}_transcript.txt`"
                  target="_blank"
                  aria-label="Download transcript"
                />
              </div>

              <div v-if="transcriptOpen[episode.id]" class="q-mt-sm">
                <q-inner-loading v-if="transcriptLoading[episode.id]" showing color="primary" label="Loading transcript..." />
                <q-banner v-else-if="transcriptLoadError[episode.id]" class="bg-negative text-white" dense>
                  {{ transcriptLoadError[episode.id] }}
                </q-banner>
                <q-card v-else-if="transcriptText[episode.id]" flat bordered class="bg-grey-1">
                  <q-card-section style="white-space: pre-wrap; max-height: 300px; overflow-y: auto;">
                    {{ transcriptText[episode.id] }}
                  </q-card-section>
                </q-card>
              </div>
            </div>

            <div v-if="episode.tags.length > 0" class="q-mt-sm">
              <q-chip
                v-for="tag in episode.tags"
                :key="tag"
                outline
                color="primary"
                text-color="primary"
                dense
                size="sm"
              >
                {{ tag }}
              </q-chip>
            </div>
          </q-card-section>

          <q-separator />

          <q-card-section>
            <div class="row items-center q-gutter-sm">
              <audio controls class="col-grow" :aria-label="`Audio player for ${episode.title}`">
                <source :src="episode.audioUrl" :type="episode.audioContentType" />
                Your browser does not support the audio element.
              </audio>
              <q-btn
                flat
                round
                color="primary"
                icon="download"
                :href="episode.audioUrl"
                :download="episode.audioFileName"
                :aria-label="`Download ${episode.title}`"
                target="_blank"
              >
                <q-tooltip>Download audio file ({{ formatFileSize(episode.audioFileSize) }})</q-tooltip>
              </q-btn>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<style scoped>
audio {
  max-width: 100%;
  width: 100%;
}
</style>
