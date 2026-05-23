<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import apiClient from '@/api/client'
import { useAuthStore } from '@/stores/auth'

interface PageItem {
  slug: string
  title: string
  isMarkdown: boolean
  isPublished: boolean
}

const pages = ref<PageItem[]>([])
const loading = ref(false)
const authStore = useAuthStore()
const router = useRouter()

const confirmDelete = ref(false)
const deleteSlug = ref('')
const deleteTitle = ref('')
const deleting = ref(false)

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

function promptDelete(slug: string, title: string) {
  deleteSlug.value = slug
  deleteTitle.value = title
  confirmDelete.value = true
}

async function doDelete() {
  deleting.value = true
  try {
    await apiClient.delete(`/pages/${deleteSlug.value}`)
    pages.value = pages.value.filter(p => p.slug !== deleteSlug.value)
    confirmDelete.value = false
  } catch (error) {
    console.error('Failed to delete page', error)
  } finally {
    deleting.value = false
  }
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
            { name: 'published', label: 'Published', field: 'isPublished', align: 'center' },
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
          <template v-slot:body-cell-published="props">
            <q-td :props="props">
              <q-icon
                :name="props.row.isPublished ? 'check_circle' : 'cancel'"
                :color="props.row.isPublished ? 'positive' : 'grey'"
                size="sm"
              />
            </q-td>
          </template>
          <template v-slot:body-cell-actions="props">
            <q-td :props="props">
              <div class="q-gutter-xs">
                <q-btn
                  label="Edit"
                  color="primary"
                  size="sm"
                  @click="editPage(props.row.slug)"
                />
                <q-btn
                  label="Delete"
                  color="negative"
                  size="sm"
                  flat
                  @click="promptDelete(props.row.slug, props.row.title)"
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
