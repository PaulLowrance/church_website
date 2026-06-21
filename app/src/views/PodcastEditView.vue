<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/api/client'

const router = useRouter()
const route = useRoute()
const episodeId = route.params.id as string

const title = ref('')
const speakerName = ref('')
const description = ref('')
const seriesName = ref('')
const publishedAt = ref('')
const tags = ref('')
const audioFile = ref<File | null>(null)
const currentAudioUrl = ref('')

const loading = ref(true)
const saving = ref(false)
const errors = ref<Record<string, string>>({})

onMounted(async () => {
  try {
    const response = await apiClient.get(`/podcast/episodes/${episodeId}`)
    const episode = response.data
    title.value = episode.title
    speakerName.value = episode.speakerName
    description.value = episode.description || ''
    seriesName.value = episode.seriesName || ''
    publishedAt.value = new Date(episode.publishedAt).toISOString().slice(0, 16)
    tags.value = episode.tags.join(', ')
    currentAudioUrl.value = episode.audioUrl
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
    formData.append('speakerName', speakerName.value.trim())
    if (description.value) formData.append('description', description.value.trim())
    if (seriesName.value) formData.append('seriesName', seriesName.value.trim())
    formData.append('publishedAt', new Date(publishedAt.value).toISOString())
    if (tags.value) formData.append('tags', tags.value)
    if (audioFile.value) {
      formData.append('audioFile', audioFile.value)
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
      errors.value.general = 'Failed to update episode. Please try again.'
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
        <div class="text-h5">Edit Podcast Episode</div>
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
            label="Episode Title *"
            :error="!!errors.title"
            :error-message="errors.title"
            outlined
            class="q-mb-md"
            aria-label="Episode title"
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
            aria-label="Episode description"
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
  </q-page>
</template>
