<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { marked } from 'marked'
import apiClient from '@/api/client'

const route = useRoute()
const router = useRouter()
const slug = route.params.slug as string

const title = ref('')
const body = ref('')
const isMarkdown = ref(false)
const isPublished = ref(false)
const loading = ref(false)
const saving = ref(false)
const error = ref('')

const previewHtml = computed(() => {
  if (isMarkdown.value) {
    return marked(body.value, { async: false }) as string
  }
  return body.value
})

onMounted(async () => {
  loading.value = true
  try {
    const response = await apiClient.get(`/pages/${slug}`)
    title.value = response.data.title
    body.value = response.data.body
    isMarkdown.value = response.data.isMarkdown
    isPublished.value = response.data.isPublished
  } catch (err) {
    error.value = 'Failed to load page content'
    console.error(err)
  } finally {
    loading.value = false
  }
})

async function savePage() {
  saving.value = true
  error.value = ''
  try {
    await apiClient.put(`/pages/${slug}`, {
      slug,
      title: title.value,
      body: body.value,
      isMarkdown: isMarkdown.value,
      isPublished: isPublished.value
    })
    router.push('/admin')
  } catch (err) {
    error.value = 'Failed to save page'
    console.error(err)
  } finally {
    saving.value = false
  }
}

function goBack() {
  router.push('/admin')
}
</script>

<template>
  <q-page padding>
    <q-card>
      <q-card-section class="row items-center justify-between">
        <div class="text-h5">Edit Page: {{ slug }}</div>
        <q-btn label="Back to List" color="secondary" flat @click="goBack" />
      </q-card-section>
      <q-separator />
      <q-card-section>
        <q-form @submit.prevent="savePage">
          <q-input
            v-model="title"
            label="Page Title"
            outlined
            dense
            class="q-mb-md"
            :rules="[(val: string) => !!val || 'Title is required']"
          />

          <q-toggle
            v-model="isMarkdown"
            label="Use Markdown"
            class="q-mb-md"
          />

          <q-toggle
            v-model="isPublished"
            label="Publish Page"
            color="positive"
            class="q-mb-md"
          />

          <div class="q-mb-md">
            <div class="text-subtitle2 q-mb-sm">Body Content</div>
            <q-editor
              v-if="!isMarkdown"
              v-model="body"
              min-height="300px"
              :toolbar="[
                ['bold', 'italic', 'strike', 'underline'],
                ['unordered', 'ordered'],
                ['link'],
                ['undo', 'redo']
              ]"
            />
            <div v-else class="row q-col-gutter-md">
              <div class="col-12 col-md-6">
                <q-input
                  v-model="body"
                  type="textarea"
                  outlined
                  rows="15"
                  label="Markdown Content"
                />
              </div>
              <div class="col-12 col-md-6">
                <q-card flat bordered>
                  <q-card-section>
                    <div class="text-subtitle2">Preview</div>
                  </q-card-section>
                  <q-separator />
                  <q-card-section>
                    <div v-html="previewHtml"></div>
                  </q-card-section>
                </q-card>
              </div>
            </div>
          </div>

          <div v-if="error" class="text-negative q-mb-md">{{ error }}</div>

          <div class="row justify-end q-gutter-sm">
            <q-btn label="Cancel" flat @click="goBack" />
            <q-btn
              type="submit"
              label="Save Changes"
              color="primary"
              :loading="saving"
            />
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-page>
</template>
