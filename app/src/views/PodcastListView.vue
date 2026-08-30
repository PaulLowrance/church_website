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
  scripture: string | null
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

const filters = ref<{
  series: string
  speaker: string
  scriptures: string[]
  year: string
  search: string
}>({
  series: '',
  speaker: '',
  scriptures: [],
  year: '',
  search: ''
})
const mobileFiltersOpen = ref(false)
const activeFilterCount = computed(() => {
  let count = 0
  if (filters.value.series) count++
  if (filters.value.speaker) count++
  count += filters.value.scriptures.length
  if (filters.value.year) count++
  if (filters.value.search) count++
  return count
})

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

const allSeries = computed(() => {
  const set = new Set<string>()
  episodes.value.forEach(e => { if (e.seriesName) set.add(e.seriesName) })
  return Array.from(set).sort((a, b) => a.localeCompare(b))
})

const allSpeakers = computed(() => {
  const set = new Set<string>()
  episodes.value.forEach(e => set.add(e.speakerDisplay))
  return Array.from(set).sort((a, b) => a.localeCompare(b))
})

const allScriptures = computed(() => {
  const set = new Set<string>()
  episodes.value.forEach(e => {
    if (e.scripture) {
      parseScriptureReferences(e.scripture).forEach(ref => set.add(ref))
    }
  })
  return Array.from(set).sort((a, b) => a.localeCompare(b))
})

function parseScriptureReferences(raw: string): string[] {
  return raw
    .split(',')
    .map(part => part.replace(/\b(?:and|&)\b/gi, '').trim())
    .filter(part => part.length > 0)
}

const allYears = computed(() => {
  const set = new Set<number>()
  episodes.value.forEach(e => set.add(new Date(e.publishedAt).getFullYear()))
  return Array.from(set).sort((a, b) => b - a)
})

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric'
  })
}

function buildQueryParams() {
  const params = new URLSearchParams()
  if (filters.value.series) params.append('series', filters.value.series)
  if (filters.value.speaker) params.append('speaker', filters.value.speaker)
  if (filters.value.scriptures.length > 0) {
    params.append('scripture', filters.value.scriptures.join(','))
  }
  if (filters.value.year) params.append('year', filters.value.year)
  if (filters.value.search) params.append('search', filters.value.search)
  return params.toString()
}

async function loadEpisodes() {
  loading.value = true
  error.value = ''
  try {
    const query = buildQueryParams()
    const [episodesRes, siteRes] = await Promise.all([
      apiClient.get(`/podcast/episodes${query ? `?${query}` : ''}`),
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
}

function applyFilters() {
  mobileFiltersOpen.value = false
  loadEpisodes()
}

function clearFilters() {
  filters.value = { series: '', speaker: '', scriptures: [], year: '', search: '' }
  loadEpisodes()
}

function toggleScripture(ref: string) {
  const index = filters.value.scriptures.indexOf(ref)
  if (index === -1) {
    filters.value.scriptures.push(ref)
  } else {
    filters.value.scriptures.splice(index, 1)
  }
}

onMounted(() => {
  loadEpisodes()
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

      <div class="sermon-list__layout">
        <aside class="filter-sidebar" aria-label="Sermon filters">
          <div class="filter-sidebar__header">
            <h2 class="filter-sidebar__title">Filter</h2>
            <button v-if="activeFilterCount > 0" class="filter-sidebar__clear" type="button" @click="clearFilters">
              Clear {{ activeFilterCount }}
            </button>
          </div>

          <form class="filter-form" @submit.prevent="applyFilters">
            <label class="filter-field">
              <span class="filter-field__label">Search</span>
              <input
                v-model="filters.search"
                class="filter-field__input"
                type="search"
                placeholder="Title, speaker, description…"
                aria-label="Search sermons"
              />
            </label>

            <details class="filter-group">
              <summary class="filter-group__summary">Series</summary>
              <div class="filter-group__options">
                <label v-for="series in allSeries" :key="series" class="filter-option">
                  <input
                    v-model="filters.series"
                    type="radio"
                    name="series"
                    :value="series"
                  />
                  <span>{{ series }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Speaker</summary>
              <div class="filter-group__options">
                <label v-for="speaker in allSpeakers" :key="speaker" class="filter-option">
                  <input
                    v-model="filters.speaker"
                    type="radio"
                    name="speaker"
                    :value="speaker"
                  />
                  <span>{{ speaker }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Scripture</summary>
              <div class="filter-group__options">
                <label v-for="ref in allScriptures" :key="ref" class="filter-option">
                  <input
                    :checked="filters.scriptures.includes(ref)"
                    type="checkbox"
                    name="scripture"
                    :value="ref"
                    @change="toggleScripture(ref)"
                  />
                  <span>{{ ref }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Year</summary>
              <div class="filter-group__options">
                <label v-for="year in allYears" :key="year" class="filter-option">
                  <input
                    v-model="filters.year"
                    type="radio"
                    name="year"
                    :value="String(year)"
                  />
                  <span>{{ year }}</span>
                </label>
              </div>
            </details>

            <button class="filter-submit" type="submit">Apply Filters</button>
          </form>
        </aside>

        <div class="sermon-list__main">
          <div class="sermon-list__toolbar">
            <button
              class="filter-toggle"
              type="button"
              aria-haspopup="dialog"
              :aria-expanded="mobileFiltersOpen"
              @click="mobileFiltersOpen = true"
            >
              Filters
              <span v-if="activeFilterCount > 0" class="filter-toggle__badge">
                {{ activeFilterCount }}
              </span>
            </button>
            <p v-if="activeFilterCount > 0" class="sermon-list__active-filters">
              {{ activeFilterCount }} active filter{{ activeFilterCount === 1 ? '' : 's' }}
            </p>
          </div>

          <div v-if="!loading && episodes.length === 0 && !error" class="sermon-list__empty">
            <p>No sermons match the current filters.</p>
            <button v-if="activeFilterCount > 0" class="filter-submit" type="button" @click="clearFilters">
              Clear filters
            </button>
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
        </div>
      </div>

      <dialog
        class="filter-dialog"
        :open="mobileFiltersOpen"
        aria-label="Sermon filters"
        @close="mobileFiltersOpen = false"
      >
        <div class="filter-dialog__backdrop" @click="mobileFiltersOpen = false" />
        <div class="filter-dialog__sheet" role="dialog" aria-modal="true">
          <div class="filter-dialog__header">
            <h2 class="filter-dialog__title">Filter Sermons</h2>
            <button
              class="filter-dialog__close"
              type="button"
              aria-label="Close filters"
              @click="mobileFiltersOpen = false"
            >
              ×
            </button>
          </div>

          <form class="filter-form" @submit.prevent="applyFilters">
            <label class="filter-field">
              <span class="filter-field__label">Search</span>
              <input
                v-model="filters.search"
                class="filter-field__input"
                type="search"
                placeholder="Title, speaker, description…"
                aria-label="Search sermons"
              />
            </label>

            <details class="filter-group">
              <summary class="filter-group__summary">Series</summary>
              <div class="filter-group__options">
                <label v-for="series in allSeries" :key="series" class="filter-option">
                  <input v-model="filters.series" type="radio" name="series" :value="series" />
                  <span>{{ series }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Speaker</summary>
              <div class="filter-group__options">
                <label v-for="speaker in allSpeakers" :key="speaker" class="filter-option">
                  <input v-model="filters.speaker" type="radio" name="speaker" :value="speaker" />
                  <span>{{ speaker }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Scripture</summary>
              <div class="filter-group__options">
                <label v-for="ref in allScriptures" :key="ref" class="filter-option">
                  <input
                    :checked="filters.scriptures.includes(ref)"
                    type="checkbox"
                    name="scripture"
                    :value="ref"
                    @change="toggleScripture(ref)"
                  />
                  <span>{{ ref }}</span>
                </label>
              </div>
            </details>

            <details class="filter-group">
              <summary class="filter-group__summary">Year</summary>
              <div class="filter-group__options">
                <label v-for="year in allYears" :key="year" class="filter-option">
                  <input v-model="filters.year" type="radio" name="year" :value="String(year)" />
                  <span>{{ year }}</span>
                </label>
              </div>
            </details>

            <div class="filter-dialog__actions">
              <button v-if="activeFilterCount > 0" class="filter-submit filter-submit--ghost" type="button" @click="clearFilters">
                Clear
              </button>
              <button class="filter-submit" type="submit">Apply Filters</button>
            </div>
          </form>
        </div>
      </dialog>
    </main>
  </q-page>
</template>

<style scoped>
.sermon-list {
  max-width: 1200px;
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
  aspect-ratio: 4 / 3;
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

.sermon-list__layout {
  display: flex;
  gap: var(--space-6);
  align-items: flex-start;
}

.filter-sidebar {
  display: none;
  flex-shrink: 0;
  width: 240px;
  position: sticky;
  top: var(--space-4);
  max-height: calc(100vh - var(--space-8));
  overflow-y: auto;
  padding-right: var(--space-2);
}

@media (min-width: 1024px) {
  .filter-sidebar {
    display: block;
  }
}

.filter-sidebar__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.filter-sidebar__title {
  font-family: var(--heading);
  font-size: 1.25rem;
  margin: 0;
  color: var(--ink);
}

.filter-sidebar__clear {
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--accent-burgundy);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-decoration: underline;
  text-decoration-color: var(--accent-gold);
}

.sermon-list__main {
  flex: 1;
  min-width: 0;
}

.sermon-list__toolbar {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  margin-bottom: var(--space-4);
}

.filter-toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.625rem 1rem;
  border: 1px solid var(--rule);
  border-radius: 8px;
  background: var(--panel);
  color: var(--ink);
  font-family: var(--sans);
  font-size: 0.9375rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.filter-toggle:hover {
  background: var(--rule);
}

@media (min-width: 1024px) {
  .filter-toggle {
    display: none;
  }
}

.filter-toggle__badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.25rem;
  height: 1.25rem;
  padding: 0 0.375rem;
  border-radius: 9999px;
  background: var(--accent-burgundy);
  color: #fff;
  font-size: 0.75rem;
}

.sermon-list__active-filters {
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--muted);
  margin: 0;
}

.filter-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.filter-field {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
}

.filter-field__label {
  font-family: var(--sans);
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--ink-soft);
}

.filter-field__input {
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--rule);
  border-radius: 8px;
  background: var(--paper);
  color: var(--ink);
  font-family: var(--sans);
  font-size: 0.9375rem;
}

.filter-field__input:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

.filter-group {
  border: 1px solid var(--rule);
  border-radius: 8px;
  overflow: hidden;
}

.filter-group__summary {
  padding: var(--space-2) var(--space-3);
  font-family: var(--sans);
  font-size: 0.9375rem;
  font-weight: 500;
  color: var(--ink);
  background: var(--panel);
  cursor: pointer;
  list-style: none;
}

.filter-group__summary::-webkit-details-marker {
  display: none;
}

.filter-group__options {
  display: flex;
  flex-direction: column;
  gap: var(--space-1);
  padding: var(--space-2) var(--space-3);
  max-height: 200px;
  overflow-y: auto;
}

.filter-option {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--ink-soft);
  cursor: pointer;
}

.filter-option input[type='radio'] {
  margin: 0;
}

.filter-submit {
  padding: 0.625rem 1rem;
  border: 1px solid var(--accent-burgundy);
  border-radius: 8px;
  background: var(--accent-burgundy);
  color: #fff;
  font-family: var(--sans);
  font-size: 0.9375rem;
  font-weight: 500;
  cursor: pointer;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.filter-submit:hover {
  background: var(--accent-gold);
  border-color: var(--accent-gold);
  color: var(--ink);
}

.filter-submit--ghost {
  background: transparent;
  color: var(--accent-burgundy);
}

.filter-submit--ghost:hover {
  background: var(--accent-gold-soft);
  border-color: var(--accent-gold);
  color: var(--ink);
}

.filter-dialog {
  position: fixed;
  inset: 0;
  z-index: 100;
  display: flex;
  align-items: flex-end;
  justify-content: flex-end;
  width: 100%;
  height: 100%;
  max-width: 100%;
  max-height: 100%;
  margin: 0;
  padding: 0;
  border: none;
  background: transparent;
}

.filter-dialog:not([open]) {
  display: none;
}

.filter-dialog__backdrop {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
}

.filter-dialog__sheet {
  position: relative;
  width: 100%;
  max-height: 85vh;
  overflow-y: auto;
  background: var(--paper);
  border-radius: 16px 16px 0 0;
  padding: var(--space-4);
  box-shadow: var(--shadow);
}

@media (min-width: 640px) {
  .filter-dialog {
    align-items: center;
    justify-content: center;
  }

  .filter-dialog__sheet {
    width: 420px;
    max-height: 80vh;
    border-radius: 16px;
  }
}

.filter-dialog__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: var(--space-4);
}

.filter-dialog__title {
  font-family: var(--heading);
  font-size: 1.25rem;
  margin: 0;
  color: var(--ink);
}

.filter-dialog__close {
  width: 2rem;
  height: 2rem;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--rule);
  border-radius: 9999px;
  background: var(--panel);
  color: var(--ink);
  font-size: 1.25rem;
  cursor: pointer;
}

.filter-dialog__actions {
  display: flex;
  gap: var(--space-2);
  justify-content: flex-end;
  margin-top: var(--space-4);
}
</style>
