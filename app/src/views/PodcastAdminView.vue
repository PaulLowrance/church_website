<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
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
  publishedAt: string
  createdAt: string
  tags: string[]
}

const router = useRouter()
const episodes = ref<PodcastEpisode[]>([])
const loading = ref(false)

const confirmDelete = ref(false)
const deleteId = ref('')
const deleteTitle = ref('')
const deleting = ref(false)

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

onMounted(async () => {
  loading.value = true
  try {
    const response = await apiClient.get('/admin/podcast/episodes')
    episodes.value = response.data
  } catch (error) {
    console.error('Failed to load podcast episodes', error)
  } finally {
    loading.value = false
  }
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
        <div class="text-h5">Manage Podcast Episodes</div>
        <q-btn label="Create New Episode" color="positive" @click="createEpisode" />
      </q-card-section>
      <q-separator />
      <q-card-section>
        <q-table
          :rows="episodes"
          :columns="[
            { name: 'title', label: 'Title', field: 'title', align: 'left' },
            { name: 'speaker', label: 'Speaker', field: 'speakerName', align: 'left' },
            { name: 'series', label: 'Series', field: 'seriesName', align: 'left' },
            { name: 'published', label: 'Published At', field: 'publishedAt', align: 'left' },
            { name: 'size', label: 'File Size', field: 'audioFileSize', align: 'right' },
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
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <div class="q-gutter-xs">
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
  </q-page>
</template>
