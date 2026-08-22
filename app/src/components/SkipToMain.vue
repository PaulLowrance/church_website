<script setup lang="ts">
// Skip to main content link for keyboard users
</script>

<template>
  <a href="#main" class="skip-link">
    Skip to main content
  </a>
</template>

<style scoped>
.skip-link {
  position: absolute;
  top: -100%;
  left: 50%;
  transform: translateX(-50%);
  z-index: 9999;
  padding: 0.75rem 1.25rem;
  background: var(--ink);
  color: var(--paper);
  font-family: var(--sans);
  font-size: 1rem;
  text-decoration: none;
  border-radius: 0.25rem;
  transition: top 0.2s ease;
}

.skip-link:focus {
  top: 0.5rem;
  outline: 3px solid var(--accent-gold);
  outline-offset: 2px;
}
</style>
