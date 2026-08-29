import { ref } from 'vue'
import { useHead } from '@unhead/vue'
import apiClient from '@/api/client'

export interface SiteInfo {
  churchName: string
  url: string
  telephone: string
  email: string
  streetAddress: string
  addressLocality: string
  addressRegion: string
  postalCode: string
  addressCountry: string
  geoLatitude: number | null
  geoLongitude: number | null
  denomination: string
  defaultCoverImage: string
}

export function useSiteInfo() {
  const siteInfo = ref<SiteInfo | null>(null)
  const error = ref('')

  async function loadSiteInfo() {
    try {
      const response = await apiClient.get('/site-info')
      siteInfo.value = response.data
    } catch (err) {
      error.value = 'Failed to load site information.'
      console.error(err)
    }
  }

  return { siteInfo, error, loadSiteInfo }
}

export function useChurchJsonLd(siteInfo: { value: SiteInfo | null }) {
  useHead(() => {
    const info = siteInfo.value
    if (!info) return {}
    return {
      script: [
        {
          key: 'church-schema',
          type: 'application/ld+json',
          innerHTML: JSON.stringify({
            '@context': 'https://schema.org',
            '@type': ['Church', 'LocalBusiness'],
            '@id': `${info.url}/#church`,
            name: info.churchName,
            url: info.url,
            ...(info.telephone ? { telephone: info.telephone } : {}),
            ...(info.email ? { email: info.email } : {}),
            ...(info.streetAddress ? {
              address: {
                '@type': 'PostalAddress',
                streetAddress: info.streetAddress,
                addressLocality: info.addressLocality,
                addressRegion: info.addressRegion,
                postalCode: info.postalCode,
                addressCountry: info.addressCountry
              }
            } : {}),
            ...(info.geoLatitude != null && info.geoLongitude != null ? {
              geo: {
                '@type': 'GeoCoordinates',
                latitude: info.geoLatitude,
                longitude: info.geoLongitude
              }
            } : {}),
            denomination: info.denomination
          })
        }
      ]
    }
  })
}

export function usePodcastEpisodeJsonLd(episode: { value: { id: string; title: string; publishedAt: string; audioUrl: string } | null }, siteInfo: { value: SiteInfo | null }) {
  useHead(() => {
    const ep = episode.value
    const info = siteInfo.value
    if (!ep) return {}
    return {
      script: [
        {
          key: 'podcast-episode-schema',
          type: 'application/ld+json',
          innerHTML: JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'PodcastEpisode',
            name: ep.title,
            ...(info ? { url: `${info.url}/sermon/${ep.id}` } : {}),
            datePublished: ep.publishedAt,
            audio: {
              '@type': 'AudioObject',
              contentUrl: ep.audioUrl
            }
          })
        }
      ]
    }
  })
}