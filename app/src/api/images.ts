import apiClient from './client'

export async function uploadImage(file: File): Promise<string> {
  const formData = new FormData()
  formData.append('imageFile', file)

  const response = await apiClient.post('/images', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })

  return response.data.publicUrl as string
}
