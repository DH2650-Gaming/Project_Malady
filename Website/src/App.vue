<template>
  <Navbar />
  <Homepage />
  <Mechanics />
  <Team />
  <World />
  <LiveStream />
</template>

<script setup>
import Navbar from './components/Navbar.vue'
import Homepage from './components/Homepage.vue'
import Mechanics from './components/Mechanics.vue'
import Team from './components/Team.vue'
import World from './components/World.vue'
</script>

<style>
html {
  scroll-behavior: smooth;
}
</style>