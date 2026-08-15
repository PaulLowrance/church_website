<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '@/api/client'

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
  publishedAt: string
  createdAt: string
  transcriptStatus: string
  transcriptError: string | null
  summaryStatus: string
  summaryError: string | null
  tags: string[]
}

const router = useRouter()
const episodes = ref<PodcastEpisode[]>([])
const loading = ref(false)

const confirmDelete = ref(false)
const deleteId = ref('')
const deleteTitle = ref('')
const deleting = ref(false)

const retryId = ref('')
const retryTitle = ref('')
const retryType = ref<'transcription' | 'summary'>('transcription')
const confirmRetry = ref(false)
const retrying = ref(false)

const regenerateId = ref('')
const regenerateTitle = ref('')
const confirmRegenerate = ref(false)
const regeneratingSummary = ref(false)

async function loadEpisodes() {
  loading.value = true
  try {
    const response = await apiClient.get('/admin/podcast/episodes')
    episodes.value = response.data
  } catch (error) {
    console.error('Failed to load sermons', error)
  } finally {
    loading.value = false
  }
}

function promptRetry(id: string, title: string, type: 'transcription' | 'summary') {
  retryId.value = id
  retryTitle.value = title
  retryType.value = type
  confirmRetry.value = true
}

async function doRetry() {
  retrying.value = true
  try {
    const endpoint = retryType.value === 'transcription'
      ? `/podcast/episodes/${retryId.value}/retry-transcription`
      : `/podcast/episodes/${retryId.value}/retry-summary`
    await apiClient.post(endpoint, {})
    confirmRetry.value = false
    await loadEpisodes()
  } catch (error) {
    console.error('Failed to retry', error)
  } finally {
    retrying.value = false
  }
}

function promptRegenerateSummary(id: string, title: string) {
  regenerateId.value = id
  regenerateTitle.value = title
  confirmRegenerate.value = true
}

async function doRegenerateSummary() {
  regeneratingSummary.value = true
  try {
    await apiClient.post(`/podcast/episodes/${regenerateId.value}/retry-summary`, {})
    confirmRegenerate.value = false
    await loadEpisodes()
  } catch (error) {
    console.error('Failed to regenerate summary', error)
  } finally {
    regeneratingSummary.value = false
  }
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

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

function transcriptStatusColor(status: string): string {
  switch (status) {
    case 'completed':
      return 'positive'
    case 'processing':
    case 'queued':
      return 'amber'
    case 'error':
      return 'negative'
    default:
      return 'grey'
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

function summaryStatusColor(status: string): string {
  switch (status) {
    case 'completed':
      return 'positive'
    case 'processing':
    case 'queued':
      return 'amber'
    case 'error':
      return 'negative'
    default:
      return 'grey'
  }
}

onMounted(async () => {
  await loadEpisodes()
})

function createEpisode() {
  router.push('/admin/podcast/create')
}

function editEpisode(id: string) {
  router.push(`/admin/podcast/${id}/edit`)
}

function promptDelete(id: string, title: string) {
  deleteId.value = id
  deleteTitle.value = title
  confirmDelete.value = true
}

async function doDelete() {
  deleting.value = true
  try {
    await apiClient.delete(`/podcast/episodes/${deleteId.value}`)
    episodes.value = episodes.value.filter(e => e.id !== deleteId.value)
    confirmDelete.value = false
  } catch (error) {
    console.error('Failed to delete episode', error)
  } finally {
    deleting.value = false
  }
}
</script>

<template>
  <q-page padding>
    <q-card>
      <q-card-section class="row items-center justify-between">
        <div class="text-h5">Manage Sermons</div>
        <q-btn label="Create New Sermon" color="positive" @click="createEpisode" />
      </q-card-section>
      <q-separator />
      <q-card-section>
        <q-table
          :rows="episodes"
          :columns="[
            { name: 'title', label: 'Title', field: 'title', align: 'left' },
            { name: 'speaker', label: 'Speaker', field: 'speakerDisplay', align: 'left' },
            { name: 'series', label: 'Series', field: 'seriesName', align: 'left' },
            { name: 'published', label: 'Published At', field: 'publishedAt', align: 'left' },
            { name: 'size', label: 'File Size', field: 'audioFileSize', align: 'right' },
            { name: 'transcript', label: 'Transcript', field: 'transcriptStatus', align: 'left' },
            { name: 'summary', label: 'Summary', field: 'summaryStatus', align: 'left' },
            { name: 'actions', label: 'Actions', field: 'actions', align: 'center' }
          ]"
          row-key="id"
          :loading="loading"
          flat
          bordered
        >
          <template v-slot:body-cell-series="props">
            <q-td :props="props">
              <span v-if="props.row.seriesName">{{ props.row.seriesName }}</span>
              <span v-else class="text-grey">—</span>
            </q-td>
          </template>
          <template v-slot:body-cell-published="props">
            <q-td :props="props">
              {{ formatDate(props.row.publishedAt) }}
            </q-td>
          </template>
          <template v-slot:body-cell-size="props">
            <q-td :props="props">
              {{ formatFileSize(props.row.audioFileSize) }}
            </q-td>
          </template>
          <template v-slot:body-cell-transcript="props">
            <q-td :props="props">
              <div>
                <q-chip
                  :color="transcriptStatusColor(props.row.transcriptStatus)"
                  text-color="white"
                  dense
                  size="sm"
                  :label="transcriptStatusLabel(props.row.transcriptStatus)"
                />
                <q-tooltip v-if="props.row.transcriptError">
                  {{ props.row.transcriptError }}
                </q-tooltip>
              </div>
            </q-td>
          </template>
          <template v-slot:body-cell-summary="props">
            <q-td :props="props">
              <div>
                <q-chip
                  :color="summaryStatusColor(props.row.summaryStatus)"
                  text-color="white"
                  dense
                  size="sm"
                  :label="summaryStatusLabel(props.row.summaryStatus)"
                />
                <q-tooltip v-if="props.row.summaryError">
                  {{ props.row.summaryError }}
                </q-tooltip>
              </div>
            </q-td>
          </template>
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <div class="q-gutter-xs">
                <q-btn
                  v-if="props.row.transcriptStatus === 'error'"
                  label="Retry Transcription"
                  color="primary"
                  size="sm"
                  flat
                  @click="promptRetry(props.row.id, props.row.title, 'transcription')"
                />
                <q-btn
                  v-if="props.row.summaryStatus === 'error'"
                  label="Retry Summary"
                  color="primary"
                  size="sm"
                  flat
                  @click="promptRetry(props.row.id, props.row.title, 'summary')"
                />
                <q-btn
                  v-if="props.row.transcriptStatus === 'completed' && props.row.summaryStatus !== 'processing' && props.row.summaryStatus !== 'queued' && props.row.summaryStatus !== 'error'"
                  label="Regenerate Summary"
                  color="primary"
                  size="sm"
                  flat
                  icon="auto_awesome"
                  aria-label="Regenerate AI summary for this sermon"
                  @click="promptRegenerateSummary(props.row.id, props.row.title)"
                />
                <q-btn
                  label="Edit"
                  color="primary"
                  size="sm"
                  @click="editEpisode(props.row.id)"
                />
                <q-btn
                  label="Delete"
                  color="negative"
                  size="sm"
                  flat
                  @click="promptDelete(props.row.id, props.row.title)"
                />
              </div>
            </q-td>
          </template>
        </q-table>
      </q-card-section>
    </q-card>

    <q-dialog v-model="confirmDelete" persistent>
      <q-card>
        <q-card-section class="row items-center">
          <q-avatar icon="warning" color="negative" text-color="white" />
          <span class="q-ml-sm">Are you sure you want to delete "<strong>{{ deleteTitle }}</strong>"?</span>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" v-close-popup />
          <q-btn flat label="Delete" color="negative" :loading="deleting" @click="doDelete" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <q-dialog v-model="confirmRetry" persistent>
      <q-card>
        <q-card-section class="row items-center">
          <q-avatar icon="replay" color="primary" text-color="white" />
          <span class="q-ml-sm">
            Retry {{ retryType === 'transcription' ? 'transcription' : 'summarization' }} for
            "<strong>{{ retryTitle }}</strong>"?<br />
            <span class="text-caption">This will incur an additional API cost.</span>
          </span>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" v-close-popup />
          <q-btn flat label="Retry" color="primary" :loading="retrying" @click="doRetry" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <q-dialog v-model="confirmRegenerate" persistent>
      <q-card>
        <q-card-section class="row items-center">
          <q-avatar icon="auto_awesome" color="primary" text-color="white" />
          <span class="q-ml-sm">
            Regenerate the AI summary for
            "<strong>{{ regenerateTitle }}</strong>"?<br />
            <span class="text-caption">
              The current description will be replaced and an additional API call will be made.
            </span>
          </span>
        </q-card-section>
        <q-card-actions align="right">
          <q-btn flat label="Cancel" v-close-popup />
          <q-btn
            flat
            label="Regenerate"
            color="primary"
            :loading="regeneratingSummary"
            @click="doRegenerateSummary"
          />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>
