<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiClient from '@/api/client'

interface PodcastEpisode {
  id: string
  title: string
  speakerName: string
  description: string | null
  seriesName: string | null
  audioUrl: string
  audioFileName: string
  audioFileSize: number
  audioContentType: string
  publishedAt: string
  createdAt: string
  tags: string[]
}

const episodes = ref<PodcastEpisode[]>([])
const loading = ref(false)
const error = ref('')
const churchName = ref('')

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
    error.value = 'Failed to load podcast episodes. Please try again later.'
    console.error(err)
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <q-page padding>
    <div class="q-mb-lg">
      <h1 class="text-h4 q-mb-sm">Sermons & Podcast</h1>
      <p class="text-body1 text-grey-7">
        Listen to recent sermons from {{ churchName }}.
        You can also subscribe to our podcast feed.
      </p>
      <q-btn
        label="Subscribe to Podcast (RSS)"
        color="primary"
        icon="rss_feed"
        href="/podcast/rss"
        target="_blank"
        aria-label="Subscribe to podcast RSS feed"
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
                  {{ episode.speakerName }}
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
