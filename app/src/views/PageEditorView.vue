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
const contentType = ref<'wysiwyg' | 'markdown' | 'html'>('wysiwyg')
const isPublished = ref(false)
const showInNav = ref(true)
const navTitle = ref('')
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const navTitleWarning = ref('')

const previewHtml = computed(() => {
  if (contentType.value === 'markdown') {
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
    contentType.value = response.data.contentType || 'wysiwyg'
    isPublished.value = response.data.isPublished
    showInNav.value = response.data.showInNav
    navTitle.value = response.data.navTitle
  } catch (err) {
    error.value = 'Failed to load page content'
    console.error(err)
  } finally {
    loading.value = false
  }
})

function checkNavTitle() {
  if (navTitle.value.length > 25) {
    navTitleWarning.value = 'Menu name will be truncated to 25 characters'
  } else {
    navTitleWarning.value = ''
  }
}

async function savePage() {
  saving.value = true
  error.value = ''
  try {
    await apiClient.put(`/pages/${slug}`, {
      slug,
      title: title.value,
      body: body.value,
      contentType: contentType.value,
      isPublished: isPublished.value,
      showInNav: showInNav.value,
      navTitle: navTitle.value
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

          <q-select
            v-model="contentType"
            :options="[
              { label: 'WYSIWYG Editor', value: 'wysiwyg' },
              { label: 'Markdown', value: 'markdown' },
              { label: 'HTML', value: 'html' }
            ]"
            label="Content Format"
            outlined
            dense
            class="q-mb-md"
            emit-value
            map-options
          />

          <q-toggle
            v-model="isPublished"
            label="Publish Page"
            color="positive"
            class="q-mb-md"
          />

          <q-toggle
            v-model="showInNav"
            label="Show in Navigation Menu"
            color="info"
            class="q-mb-md"
          />

          <q-input
            v-model="navTitle"
            label="Navigation Menu Text"
            outlined
            dense
            class="q-mb-md"
            hint="Text shown in the navigation menu. Defaults to page title."
            :error="!!navTitleWarning"
            :error-message="navTitleWarning"
            maxlength="25"
            counter
            @update:model-value="checkNavTitle"
          />

          <div class="q-mb-md">
            <div class="text-subtitle2 q-mb-sm">Body Content</div>
            <q-editor
              v-if="contentType === 'wysiwyg'"
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
                  :label="contentType === 'markdown' ? 'Markdown Content' : 'HTML Content'"
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
