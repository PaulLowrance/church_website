<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const authStore = useAuthStore()
const router = useRouter()

async function handleLogin() {
  loading.value = true
  error.value = ''
  try {
    await authStore.login(username.value, password.value)
    router.push('/admin')
  } catch (err) {
    error.value = 'Invalid username or password'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <q-page class="flex flex-center">
    <q-card style="min-width: 300px">
      <q-card-section>
        <div class="text-h6">Admin Login</div>
      </q-card-section>
      <q-card-section>
        <q-form @submit.prevent="handleLogin">
          <q-input v-model="username" label="Username" outlined dense class="q-mb-md" />
          <q-input v-model="password" label="Password" type="password" outlined dense class="q-mb-md" />
          <q-btn type="submit" label="Login" color="primary" :loading="loading" class="full-width" />
        </q-form>
        <div v-if="error" class="text-negative q-mt-sm">{{ error }}</div>
      </q-card-section>
    </q-card>
  </q-page>
</template>
