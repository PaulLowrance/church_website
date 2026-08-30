<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/api/client'

const router = useRouter()
const route = useRoute()
const episodeId = route.params.id as string

const title = ref('')
const speakerTitle = ref('')
const speakerName = ref('')
const description = ref('')
const scripture = ref('')
const seriesName = ref('')
const publishedAt = ref('')
const tags = ref('')
const audioFile = ref<File | null>(null)
const coverImageFile = ref<File | null>(null)
const currentAudioUrl = ref('')
const currentCoverImageUrl = ref('')
const transcriptStatus = ref('')
const transcriptError = ref('')
const summaryStatus = ref('')
const summaryError = ref('')

const loading = ref(true)
const saving = ref(false)
const retrying = ref(false)
const errors = ref<Record<string, string>>({})

const confirmRegenerateSummary = ref(false)
const regeneratingSummary = ref(false)

function transcriptStatusLabel(status: string): string {
  switch (status) {
    case 'queued':
      return 'Queued'
    case 'processing':
      return 'Transcribing'
    case 'completed':
      return 'Completed'
    case 'error':
      return 'Error'
    default:
      return 'None'
  }
}

function summaryStatusLabel(status: string): string {
  switch (status) {
    case 'queued':
      return 'Summary Queued'
    case 'processing':
      return 'Summarizing'
    case 'completed':
      return 'Summary Done'
    case 'error':
      return 'Summary Error'
    default:
      return 'No Summary'
  }
}

async function retryTranscription() {
  retrying.value = true
  try {
    await apiClient.post(`/podcast/episodes/${episodeId}/retry-transcription`, {})
    transcriptStatus.value = 'queued'
    transcriptError.value = ''
  } catch (err: any) {
    console.error('Failed to retry transcription', err)
    transcriptError.value = err.response?.data?.message || 'Failed to retry transcription.'
  } finally {
    retrying.value = false
  }
}

async function retrySummary() {
  retrying.value = true
  try {
    const response = await apiClient.post(`/podcast/episodes/${episodeId}/retry-summary`, {})
    description.value = response.data.description || ''
    summaryStatus.value = response.data.summaryStatus || 'completed'
    summaryError.value = ''
  } catch (err: any) {
    console.error('Failed to retry summary', err)
    summaryError.value = err.response?.data?.message || 'Failed to retry summary.'
  } finally {
    retrying.value = false
  }
}

const episodeTitle = ref('')
const showRegenerateSummaryConfirm = () => {
  if (transcriptStatus.value !== 'completed') return
  confirmRegenerateSummary.value = true
}
const cancelRegenerateSummary = () => {
  confirmRegenerateSummary.value = false
}
const confirmAndRegenerateSummary = async () => {
  regeneratingSummary.value = true
  try {
    const response = await apiClient.post(`/podcast/episodes/${episodeId}/retry-summary`, {})
    description.value = response.data.description || ''
    summaryStatus.value = response.data.summaryStatus || 'completed'
    summaryError.value = ''
    confirmRegenerateSummary.value = false
  } catch (err: any) {
    console.error('Failed to regenerate summary', err)
    summaryError.value = err.response?.data?.message || 'Failed to regenerate summary.'
  } finally {
    regeneratingSummary.value = false
  }
}

onMounted(async () => {
  try {
    const response = await apiClient.get(`/podcast/episodes/${episodeId}`)
    const episode = response.data
    title.value = episode.title
    episodeTitle.value = episode.title
    speakerTitle.value = episode.speakerTitle || ''
    speakerName.value = episode.speakerName
    description.value = episode.description || ''
    scripture.value = episode.scripture || ''
    seriesName.value = episode.seriesName || ''
    publishedAt.value = new Date(episode.publishedAt).toISOString().slice(0, 16)
    tags.value = episode.tags.join(', ')
    currentAudioUrl.value = episode.audioUrl
    currentCoverImageUrl.value = episode.coverImageUrl || ''
    transcriptStatus.value = episode.transcriptStatus || 'none'
    transcriptError.value = episode.transcriptError || ''
    summaryStatus.value = episode.summaryStatus || 'none'
    summaryError.value = episode.summaryError || ''
  } catch (error) {
    console.error('Failed to load episode', error)
    errors.value.general = 'Failed to load episode data.'
  } finally {
    loading.value = false
  }
})

function validate(): boolean {
  errors.value = {}
  if (!title.value.trim()) errors.value.title = 'Title is required'
  if (!speakerName.value.trim()) errors.value.speakerName = 'Speaker name is required'
  return Object.keys(errors.value).length === 0
}

async function saveEpisode() {
  if (!validate()) return

  saving.value = true
  try {
    const formData = new FormData()
    formData.append('id', episodeId)
    formData.append('title', title.value.trim())
    if (speakerTitle.value.trim()) formData.append('speakerTitle', speakerTitle.value.trim())
    formData.append('speakerName', speakerName.value.trim())
    if (description.value) formData.append('description', description.value.trim())
    if (scripture.value.trim()) formData.append('scripture', scripture.value.trim())
    if (seriesName.value) formData.append('seriesName', seriesName.value.trim())
    formData.append('publishedAt', new Date(publishedAt.value).toISOString())
    if (tags.value) formData.append('tags', tags.value)
    if (audioFile.value) {
      formData.append('audioFile', audioFile.value)
    }
    if (coverImageFile.value) {
      formData.append('coverImageFile', coverImageFile.value)
    }

    await apiClient.put(`/podcast/episodes/${episodeId}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })

    router.push('/admin/podcast')
  } catch (err: any) {
    console.error('Failed to update episode', err)
    if (err.response?.data?.errors) {
      errors.value = err.response.data.errors
    } else {
      errors.value.general = 'Failed to update sermon. Please try again.'
    }
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push('/admin/podcast')
}
</script>

<template>
  <q-page padding>
    <q-card style="max-width: 800px; margin: 0 auto">
      <q-card-section>
        <div class="text-h5">Edit Sermon</div>
      </q-card-section>

      <q-separator />

      <q-card-section v-if="loading" class="text-center q-pa-xl">
        <q-spinner color="primary" size="3rem" />
        <p class="q-mt-md">Loading episode...</p>
      </q-card-section>

      <q-card-section v-else>
        <q-form @submit.prevent="saveEpisode" greedy>
          <q-input
            v-model="title"
            label="Sermon Title *"
            :error="!!errors.title"
            :error-message="errors.title"
            outlined
            class="q-mb-md"
            aria-label="Sermon title"
          />

          <q-input
            v-model="speakerTitle"
            label="Speaker Title (optional)"
            hint="e.g. Elder, Pastor, Brother"
            outlined
            class="q-mb-md"
            aria-label="Speaker title"
          />

          <q-input
            v-model="speakerName"
            label="Speaker Name *"
            :error="!!errors.speakerName"
            :error-message="errors.speakerName"
            outlined
            class="q-mb-md"
            aria-label="Speaker name"
          />

          <q-input
            v-model="seriesName"
            label="Series Name"
            outlined
            class="q-mb-md"
            aria-label="Series name"
          />

          <q-input
            v-model="description"
            label="Description"
            type="textarea"
            outlined
            rows="4"
            class="q-mb-md"
            aria-label="Sermon description"
          />

          <q-input
            v-model="scripture"
            label="Scripture Reference"
            hint="e.g. 1 John 1:1-4"
            outlined
            class="q-mb-md"
            aria-label="Scripture reference"
          />

          <q-input
            v-model="publishedAt"
            label="Publish Date & Time *"
            type="datetime-local"
            outlined
            class="q-mb-md"
            aria-label="Publish date and time"
          />

          <q-input
            v-model="tags"
            label="Tags (comma separated)"
            hint="e.g. sermon, gospel, faith"
            outlined
            class="q-mb-md"
            aria-label="Tags"
          />

          <div class="q-mb-md">
            <div class="text-caption text-grey-7 q-mb-xs">Cover Image</div>
            <div v-if="currentCoverImageUrl && !coverImageFile" class="q-mb-sm">
              <img
                :src="currentCoverImageUrl"
                alt="Current cover image"
                style="max-height: 120px; max-width: 100%; border-radius: 8px;"
              />
            </div>
            <q-file
              v-model="coverImageFile"
              label="Replace Cover Image"
              accept="image/*"
              outlined
              :hint="coverImageFile ? `Selected: ${coverImageFile.name}` : 'Leave empty to keep current image'"
              aria-label="Replace cover image"
            >
              <template v-slot:prepend>
                <q-icon name="image" />
              </template>
            </q-file>
          </div>

          <div class="q-mb-md">
            <div class="text-caption text-grey-7 q-mb-xs">Current Audio File</div>
            <audio v-if="currentAudioUrl && !audioFile" controls class="full-width q-mb-sm">
              <source :src="currentAudioUrl" />
            </audio>
            <q-file
              v-model="audioFile"
              label="Replace Audio File"
              accept="audio/*"
              outlined
              :hint="audioFile ? `Selected: ${audioFile.name}` : 'Leave empty to keep current file'"
              aria-label="Replace audio file"
            >
              <template v-slot:prepend>
                <q-icon name="attach_file" />
              </template>
            </q-file>
          </div>

          <div class="q-mb-md">
            <div class="text-caption text-grey-7 q-mb-xs">Transcript Status</div>
            <div class="row items-center q-gutter-sm">
              <q-chip
                dense
                :color="transcriptStatus === 'completed' ? 'positive' : (transcriptStatus === 'error' ? 'negative' : 'grey')"
                text-color="white"
                :label="transcriptStatusLabel(transcriptStatus)"
              />
              <q-btn
                v-if="transcriptStatus === 'error'"
                label="Retry Transcription"
                color="primary"
                size="sm"
                flat
                :loading="retrying"
                @click="retryTranscription"
              />
            </div>
            <q-banner v-if="transcriptError" class="bg-negative text-white q-mt-sm" dense>
              {{ transcriptError }}
            </q-banner>

            <div class="text-caption text-grey-7 q-mt-md q-mb-xs">Summary Status</div>
            <div class="row items-center q-gutter-sm">
              <q-chip
                dense
                :color="summaryStatus === 'completed' ? 'positive' : (summaryStatus === 'error' ? 'negative' : 'grey')"
                text-color="white"
                :label="summaryStatusLabel(summaryStatus)"
              />
              <q-btn
                v-if="summaryStatus === 'error'"
                label="Retry Summary"
                color="primary"
                size="sm"
                flat
                :loading="retrying"
                @click="retrySummary"
              />
              <q-btn
                v-if="transcriptStatus === 'completed' && summaryStatus !== 'processing' && summaryStatus !== 'queued' && summaryStatus !== 'error'"
                label="Regenerate Summary"
                color="primary"
                size="sm"
                flat
                icon="auto_awesome"
                :loading="regeneratingSummary"
                aria-label="Regenerate AI summary for this sermon"
                @click="showRegenerateSummaryConfirm"
              />
            </div>
            <q-banner v-if="summaryError" class="bg-negative text-white q-mt-sm" dense>
              {{ summaryError }}
            </q-banner>
          </div>

          <q-banner v-if="errors.general" class="bg-negative text-white q-mb-md" role="alert">
            <template v-slot:avatar>
              <q-icon name="error" color="white" />
            </template>
            {{ errors.general }}
          </q-banner>

          <div class="row q-gutter-sm justify-end">
            <q-btn label="Cancel" flat @click="goBack" />
            <q-btn label="Save Changes" type="submit" color="positive" :loading="saving" />
          </div>
        </q-form>
      </q-card-section>
    </q-card>

    <q-dialog v-model="confirmRegenerateSummary" persistent>
      <q-card>
        <q-card-section class="row items-center">
          <q-avatar icon="auto_awesome" color="primary" text-color="white" />
          <span class="q-ml-sm">
            Regenerate the AI summary for
            "<strong>{{ episodeTitle }}</strong>"?<br />
            <span class="text-caption">
              The current description will be replaced and an additional API call will be made.
            </span>
          </span>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" v-close-popup @click="cancelRegenerateSummary" />
          <q-btn
            flat
            label="Regenerate"
            color="primary"
            :loading="regeneratingSummary"
            @click="confirmAndRegenerateSummary"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>
