import { defineStore } from 'pinia'
import axios from 'axios'

interface AuthState {
  token: string | null
  username: string | null
  role: string | null
}

export const useAuthStore = defineStore('auth', {
  state: (): AuthState => ({
    token: localStorage.getItem('token'),
    username: localStorage.getItem('username'),
    role: localStorage.getItem('role')
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    userRole: (state) => state.role
  },
  actions: {
    async login(username: string, password: string) {
      const response = await axios.post('/api/auth/login', { username, password })
      this.token = response.data.token
      this.username = response.data.username
      this.role = response.data.role
      localStorage.setItem('token', this.token!)
      localStorage.setItem('username', this.username!)
      localStorage.setItem('role', this.role!)
    },
    logout() {
      this.token = null
      this.username = null
      this.role = null
      localStorage.removeItem('token')
      localStorage.removeItem('username')
      localStorage.removeItem('role')
    }
  }
})
