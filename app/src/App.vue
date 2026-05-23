<template>
  <q-layout view="hHh lpR fFf">
    <NavMenu />
    <q-page-container>
      <router-view />
    </q-page-container>
  </q-layout>
</template>

<script setup lang="ts">
import NavMenu from '@/components/NavMenu.vue'
</script>
