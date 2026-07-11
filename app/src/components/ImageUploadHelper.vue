<script setup lang="ts">
import { ref, computed } from 'vue'
import { uploadImage } from '@/api/images'

const props = defineProps<{
  contentType: 'wysiwyg' | 'markdown' | 'html'
}>()

const emit = defineEmits<{
  (e: 'insert', snippet: string): void
}>()

const imageFile = ref<File | null>(null)
const uploading = ref(false)
const uploadedUrl = ref('')
const error = ref('')
const altText = ref('')
const width = ref('')
const height = ref('')
const alignment = ref<'none' | 'left' | 'center' | 'right'>('none')

const alignmentOptions = [
  { label: 'None', value: 'none' },
  { label: 'Left', value: 'left' },
  { label: 'Center', value: 'center' },
  { label: 'Right', value: 'right' }
]

const canInsert = computed(() => !!uploadedUrl.value)

async function upload() {
  if (!imageFile.value) return

  uploading.value = true
  error.value = ''
  try {
    uploadedUrl.value = await uploadImage(imageFile.value)
  } catch (err: any) {
    error.value = err.response?.data?.message || 'Failed to upload image'
    console.error(err)
  } finally {
    uploading.value = false
  }
}

function copyUrl() {
  navigator.clipboard.writeText(uploadedUrl.value)
}

function buildSnippet() {
  const alt = altText.value.trim() || 'Image'

  if (props.contentType === 'markdown') {
    return `![${alt}](${uploadedUrl.value})`
  }

  const styles: string[] = ['max-width:100%;']
  if (width.value) {
    styles.push(`width:${width.value}px;`)
  }
  if (height.value) {
    styles.push(`height:${height.value}px;`)
  } else if (width.value) {
    styles.push('height:auto;')
  }

  if (alignment.value === 'left') {
    styles.push('display:block;margin-left:0;margin-right:auto;')
  } else if (alignment.value === 'center') {
    styles.push('display:block;margin-left:auto;margin-right:auto;')
  } else if (alignment.value === 'right') {
    styles.push('display:block;margin-left:auto;margin-right:0;')
  }

  return `<img src="${uploadedUrl.value}" alt="${alt}" style="${styles.join(' ')}">`
}

function insert() {
  if (!uploadedUrl.value) return
  emit('insert', buildSnippet())
}
</script>

<template>
  <q-card flat bordered class="q-mt-md">
    <q-card-section>
      <div class="text-subtitle2">Insert Image</div>
    </q-card-section>
    <q-separator />
    <q-card-section>
      <q-file
        v-model="imageFile"
        label="Choose an image"
        accept="image/*"
        outlined
        dense
        class="q-mb-sm"
      >
        <template v-slot:prepend>
          <q-icon name="image" />
        </template>
      </q-file>

      <q-btn
        label="Upload"
        color="primary"
        size="sm"
        :loading="uploading"
        :disable="!imageFile"
        @click="upload"
      />

      <div v-if="uploadedUrl" class="q-mt-md">
        <q-img
          :src="uploadedUrl"
          fit="contain"
          style="max-width: 200px; max-height: 150px;"
          class="bg-grey-2 rounded-borders"
          alt="Preview"
        />
        <q-input
          v-model="uploadedUrl"
          readonly
          outlined
          dense
          label="Image URL"
          class="q-mt-sm"
        >
          <template v-slot:append>
            <q-btn icon="content_copy" flat round dense size="sm" @click="copyUrl" />
          </template>
        </q-input>
      </div>

      <q-input
        v-model="altText"
        label="Alt text"
        outlined
        dense
        class="q-mt-md"
        hint="Describe the image for screen readers"
      />

      <div v-if="contentType !== 'markdown'" class="row q-col-gutter-sm q-mt-sm">
        <q-input
          v-model="width"
          class="col-6"
          label="Width (px)"
          type="number"
          outlined
          dense
          hint="Optional"
        />
        <q-input
          v-model="height"
          class="col-6"
          label="Height (px)"
          type="number"
          outlined
          dense
          hint="Optional"
        />
      </div>

      <q-select
        v-if="contentType !== 'markdown'"
        v-model="alignment"
        :options="alignmentOptions"
        label="Alignment"
        outlined
        dense
        class="q-mt-sm"
        emit-value
        map-options
      />

      <q-btn
        label="Insert into page"
        color="positive"
        size="sm"
        class="q-mt-md"
        :disable="!canInsert"
        @click="insert"
      />

      <div v-if="error" class="text-negative q-mt-sm">{{ error }}</div>
    </q-card-section>
    <q-inner-loading :showing="uploading" />
  </q-card>
</template>
