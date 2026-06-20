<template>
  <div class="list-viewer">
    <div class="list-viewer__header">
      <div class="list-viewer__title-block">
        <h2 class="list-viewer__title">{{ title }}</h2>
        <p v-if="subtitle" class="list-viewer__subtitle">{{ subtitle }}</p>
      </div>
    </div>

    <slot name="before-card" />

    <el-card v-if="$slots.default" shadow="never" class="list-viewer__card">
      <slot />
    </el-card>

    <slot name="after-card" />
  </div>
</template>

<script setup lang="ts">

interface Props {
  title: string
  subtitle?: string
}

defineProps<Props>()
</script>

<style scoped>
.list-viewer {
  width: 100%;
  min-width: 0;
  padding: 24px;
}

.list-viewer__header {
  margin-bottom: 24px;
  min-width: 0;
}

.list-viewer__title-block {
  min-width: 0;
}

.list-viewer__title {
  margin: 0;
  color: var(--el-text-color-primary);
  font-size: 22px;
  font-weight: 600;
  line-height: 1.25;
}

.list-viewer__subtitle {
  margin: 4px 0 0;
  color: var(--el-text-color-secondary);
  font-size: 14px;
  line-height: 1.4;
}

.list-viewer__card {
  width: 100%;
  min-width: 0;
}

@media (max-width: 768px) {
  .list-viewer {
    padding: 0;
  }
}
</style>
