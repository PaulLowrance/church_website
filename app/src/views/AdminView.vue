<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

interface PageItem {
  slug: string
  title: string
  isMarkdown: boolean
}

const pages = ref<PageItem[]>([])
const loading = ref(false)
const authStore = useAuthStore()
const router = useRouter()

onMounted(async () => {
  loading.value = true
  try {
    const response = await apiClient.get('/pages')
    pages.value = response.data
  } catch (error) {
    console.error('Failed to load pages', error)
  } finally {
    loading.value = false
  }
})

function editPage(slug: string) {
  router.push(`/admin/pages/${slug}/edit`)
}

function createPage() {
  router.push('/admin/pages/create')
}

function logout() {
  authStore.logout()
  router.push('/')
}
</script>

<template>
  <q-page padding>
    <q-card>
      <q-card-section class="row items-center justify-between">
        <div class="text-h5">Manage Pages</div>
        <div class="q-gutter-sm">
          <q-btn label="Create New Page" color="positive" @click="createPage" />
          <q-btn label="Logout" color="negative" flat @click="logout" />
        </div>
      </q-card-section>
      <q-separator />
      <q-card-section>
        <q-table
          :rows="pages"
          :columns="[
            { name: 'title', label: 'Title', field: 'title', align: 'left' },
            { name: 'slug', label: 'Slug', field: 'slug', align: 'left' },
            { name: 'format', label: 'Format', field: 'isMarkdown', align: 'left' },
            { name: 'actions', label: 'Actions', field: 'actions', align: 'center' }
          ]"
          row-key="slug"
          :loading="loading"
          flat
          bordered
        >
          <template v-slot:body-cell-format="props">
            <q-td :props="props">
              <q-chip
                :color="props.row.isMarkdown ? 'secondary' : 'primary'"
                text-color="white"
                dense
              >
                {{ props.row.isMarkdown ? 'Markdown' : 'HTML / WYSIWYG' }}
              </q-chip>
            </q-td>
          </template>
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <q-btn
                label="Edit"
                color="primary"
                size="sm"
                @click="editPage(props.row.slug)"
              />
            </q-td>
          </template>
        </q-table>
      </q-card-section>
    </q-card>
  </q-page>
</template>
