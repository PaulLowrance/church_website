<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'

const props = defineProps<{
  src: string
  fileName: string
  fileSize: number
  title: string
}>()

const audioRef = ref<HTMLAudioElement | null>(null)
const isPlaying = ref(false)
const duration = ref(0)
const currentTime = ref(0)
const isLoaded = ref(false)
const error = ref('')

const fileExtension = computed(() => {
  const lastDot = props.fileName.lastIndexOf('.')
  return lastDot === -1 ? 'audio' : props.fileName.slice(lastDot + 1).toLowerCase()
})

const fileSizeFormatted = computed(() => formatFileSize(props.fileSize))

const durationDisplay = computed(() => formatTime(duration.value))

const currentTimeDisplay = computed(() => formatTime(currentTime.value))

function formatTime(seconds: number): string {
  if (!isFinite(seconds) || seconds < 0) return '0:00'
  const mins = Math.floor(seconds / 60)
  const secs = Math.floor(seconds % 60)
  return `${mins}:${secs.toString().padStart(2, '0')}`
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function togglePlay() {
  if (!audioRef.value) return

  if (isPlaying.value) {
    audioRef.value.pause()
  } else {
    error.value = ''
    audioRef.value.play().catch((err) => {
      console.error('Audio playback failed', err)
      error.value = 'Audio playback failed. Please try again.'
      isPlaying.value = false
    })
  }
}

function handlePlay() {
  isPlaying.value = true
}

function handlePause() {
  isPlaying.value = false
}

function handleEnded() {
  isPlaying.value = false
  currentTime.value = 0
}

function handleLoadedMetadata() {
  if (!audioRef.value) return
  duration.value = audioRef.value.duration || 0
  isLoaded.value = true
}

function handleTimeUpdate() {
  if (!audioRef.value) return
  currentTime.value = audioRef.value.currentTime || 0
}

function handleError() {
  error.value = 'Audio failed to load.'
  isPlaying.value = false
}

function seek(event: MouseEvent) {
  if (!audioRef.value || !duration.value) return
  const target = event.currentTarget as HTMLElement
  const rect = target.getBoundingClientRect()
  const ratio = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width))
  const newTime = ratio * duration.value
  audioRef.value.currentTime = newTime
  currentTime.value = newTime
}

onMounted(() => {
  const audio = audioRef.value
  if (!audio) return

  audio.addEventListener('play', handlePlay)
  audio.addEventListener('pause', handlePause)
  audio.addEventListener('ended', handleEnded)
  audio.addEventListener('loadedmetadata', handleLoadedMetadata)
  audio.addEventListener('timeupdate', handleTimeUpdate)
  audio.addEventListener('error', handleError)
})

onUnmounted(() => {
  const audio = audioRef.value
  if (!audio) return

  audio.removeEventListener('play', handlePlay)
  audio.removeEventListener('pause', handlePause)
  audio.removeEventListener('ended', handleEnded)
  audio.removeEventListener('loadedmetadata', handleLoadedMetadata)
  audio.removeEventListener('timeupdate', handleTimeUpdate)
  audio.removeEventListener('error', handleError)
})
</script>

<template>
  <div class="audio-player" role="region" :aria-label="`Audio player for ${title}`">
    <audio
      ref="audioRef"
      :src="src"
      preload="none"
      class="audio-player__native"
      :aria-label="`Audio player for ${title}`"
    />

    <button
      type="button"
      class="audio-player__play"
      :aria-label="isPlaying ? `Pause ${title}` : `Play ${title}`"
      @click="togglePlay"
    >
      <svg v-if="isPlaying" class="audio-player__icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <rect x="6" y="4" width="4" height="16" />
        <rect x="14" y="4" width="4" height="16" />
      </svg>
      <svg v-else class="audio-player__icon" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
        <path d="M8 5v14l11-7z" />
      </svg>
    </button>

    <div class="audio-player__track" @click="seek">
      <div class="audio-player__progress" :style="{ width: `${duration ? (currentTime / duration) * 100 : 0}%` }" />
    </div>

    <div class="audio-player__meta" aria-live="polite">
      <span class="audio-player__time">{{ currentTimeDisplay }} / {{ durationDisplay }}</span>
      <span class="audio-player__file" aria-hidden="true">.{{ fileExtension }} / {{ fileSizeFormatted }}</span>
    </div>

    <a
      class="audio-player__download"
      :href="src"
      :download="fileName"
      target="_blank"
      rel="noopener"
      :aria-label="`Download ${title}`"
      title="Download audio"
    >
      <svg class="audio-player__icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" />
        <polyline points="7 10 12 15 17 10" />
        <line x1="12" y1="15" x2="12" y2="3" />
      </svg>
    </a>

    <p v-if="error" class="audio-player__error" role="alert">{{ error }}</p>
  </div>
</template>

<style scoped>
.audio-player {
  display: flex;
  align-items: center;
  gap: var(--space-3);
  width: 100%;
  min-width: 0;
  padding: var(--space-2) var(--space-3);
  background: var(--panel);
  border: 1px solid var(--rule);
  border-radius: 8px;
  flex-wrap: wrap;
}

.audio-player__native {
  display: none;
}

.audio-player__play {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  flex-shrink: 0;
  padding: 0;
  border: none;
  border-radius: 50%;
  background: var(--accent-burgundy);
  color: #fff;
  cursor: pointer;
  transition: background-color 0.2s ease, transform 0.2s ease;
}

.audio-player__play:hover {
  background: var(--accent-gold);
  color: var(--ink);
}

.audio-player__play:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

.audio-player__icon {
  width: 1.125rem;
  height: 1.125rem;
}

.audio-player__track {
  position: relative;
  flex: 1 1 100%;
  height: 6px;
  background: var(--rule);
  border-radius: 9999px;
  cursor: pointer;
  order: 3;
}

@media (min-width: 640px) {
  .audio-player__track {
    flex: 1 1 auto;
    order: 0;
  }
}

.audio-player__progress {
  height: 100%;
  background: var(--accent-burgundy);
  border-radius: 9999px;
  transition: width 0.15s linear;
  pointer-events: none;
}

.audio-player__meta {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  flex-shrink: 0;
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--muted);
  white-space: nowrap;
}

.audio-player__time {
  font-variant-numeric: tabular-nums;
  color: var(--ink-soft);
}

.audio-player__file {
  color: var(--muted);
}

.audio-player__file::before {
  content: '·';
  margin-right: var(--space-2);
  color: var(--rule);
}

.audio-player__download {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  flex-shrink: 0;
  border: 1px solid var(--rule);
  border-radius: 8px;
  color: var(--accent-burgundy);
  text-decoration: none;
  transition: background-color 0.2s ease, border-color 0.2s ease;
}

.audio-player__download:hover {
  background: var(--accent-gold-soft);
  border-color: var(--accent-gold);
  color: var(--accent-gold);
}

.audio-player__download:focus-visible {
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}

.audio-player__error {
  flex: 1 1 100%;
  margin: 0;
  font-family: var(--sans);
  font-size: 0.875rem;
  color: var(--q-negative, #c62828);
  order: 4;
}
</style>
