<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import { useRouter } from 'vue-router'
import { marked } from 'marked'
import apiClient from '@/api/client'

const router = useRouter()

const title = ref('')
const slug = ref('')
const body = ref('')
const isMarkdown = ref(false)
const saving = ref(false)
const error = ref('')
const titleError = ref('')
const bodyError = ref('')
const slugError = ref('')

const previewHtml = computed(() => {
  if (isMarkdown.value) {
    return marked(body.value, { async: false }) as string
  }
  return body.value
})

function generateSlug(input: string): string {
  if (!input) return ''
  let s = input.trim().toLowerCase()
  s = s.replace(/[^a-z0-9\s-]/g, '')
  s = s.replace(/\s+/g, '-')
  s = s.replace(/-+/g, '-')
  s = s.replace(/^-+|-+$/g, '')
  return s
}

watch(title, (newTitle) => {
  if (!slug.value || slug.value === generateSlug(title.value)) {
    slug.value = generateSlug(newTitle)
  }
})

function validate(): boolean {
  titleError.value = ''
  bodyError.value = ''
  slugError.value = ''
  let valid = true

  if (!title.value.trim()) {
    titleError.value = 'Title is required'
    valid = false
  }

  if (!body.value.trim()) {
    bodyError.value = 'Body content is required'
    valid = false
  }

  if (!slug.value.trim()) {
    slugError.value = 'Slug is required'
    valid = false
  }

  return valid
}

async function savePage() {
  if (!validate()) return

  saving.value = true
  error.value = ''
  try {
    await apiClient.post('/pages', {
      slug: slug.value,
      title: title.value,
      body: body.value,
      isMarkdown: isMarkdown.value
    })
    router.push('/admin')
  } catch (err: any) {
    if (err.response?.data?.errors) {
      const errors = err.response.data.errors
      error.value = Object.values(errors).flat().join(', ')
    } else {
      error.value = err.response?.data?.message || 'Failed to create page'
    }
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
        <div class="text-h5">Create New Page</div>
        <q-btn label="Back to List" color="secondary" flat @click="goBack" />
      </q-card-section>
      <q-separator />
      <q-card-section>
        <q-form @submit.prevent="savePage">
          <q-input
            v-model="title"
            label="Page Title *"
            outlined
            dense
            class="q-mb-md"
            :error="!!titleError"
            :error-message="titleError"
          />

          <q-input
            v-model="slug"
            label="URL Slug *"
            outlined
            dense
            class="q-mb-md"
            hint="Auto-generated from title. You can customize it."
            :error="!!slugError"
            :error-message="slugError"
          />

          <q-toggle
            v-model="isMarkdown"
            label="Use Markdown"
            class="q-mb-md"
          />

          <div class="q-mb-md">
            <div class="text-subtitle2 q-mb-sm">Body Content *</div>
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
            <div v-if="bodyError" class="text-negative q-mt-xs">{{ bodyError }}</div>
          </div>

          <div v-if="error" class="text-negative q-mb-md">{{ error }}</div>

          <div class="row justify-end q-gutter-sm">
            <q-btn label="Cancel" flat @click="goBack" />
            <q-btn
              type="submit"
              label="Create Page"
              color="positive"
              :loading="saving"
            />
          </div>
        </q-form>
      </q-card-section>
    </q-card>
  </q-page>
</template>
