<template>
  <el-button
    class="ai-assistant-button"
    type="primary"
    plain
    :icon="MagicStick"
    @click="openDialog"
  >
    Ask AI
  </el-button>

  <el-dialog
    v-model="dialogVisible"
    :title="`Ask AI about ${contextTitle}`"
    width="560px"
    append-to-body
  >
    <p class="ai-assistant-button__help">
      Faz uma pergunta sobre esta página. O contexto atual é enviado para futura integração com um provider AI.
    </p>

    <el-form @submit.prevent="submit">
      <el-form-item label="Question">
        <el-input
          v-model="question"
          type="textarea"
          :rows="5"
          placeholder="Ex: Que ações devo tomar com base nos dados desta página?"
          autocomplete="off"
        />
      </el-form-item>
    </el-form>

    <div v-if="answer" class="ai-assistant-button__answer">
      <strong>Prepared context</strong>
      <p>{{ answer }}</p>
    </div>

    <template #footer>
      <el-button @click="dialogVisible = false">
        Cancel
      </el-button>
      <el-button type="primary" :loading="submitting" @click="submit">
        Ask
      </el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { MagicStick } from '@element-plus/icons-vue'
import eventBus from '@admin-shell/ui/event-bus'

interface AiQuestionPayload {
  question: string
  context: {
    title: string
    subtitle?: string
    meta?: Record<string, unknown>
    path: string
  }
}

const props = defineProps<{
  contextTitle: string
  contextSubtitle?: string
  contextMeta?: Record<string, unknown>
}>()

const emit = defineEmits<{
  ask: [payload: AiQuestionPayload]
}>()

const dialogVisible = ref(false)
const question = ref('')
const submitting = ref(false)
const answer = ref('')

function openDialog(): void {
  question.value = ''
  answer.value = ''
  dialogVisible.value = true
}

function buildPayload(): AiQuestionPayload {
  return {
    question: question.value.trim(),
    context: {
      title: props.contextTitle,
      subtitle: props.contextSubtitle,
      meta: props.contextMeta,
      path: window.location.pathname,
    },
  }
}

async function submit(): Promise<void> {
  const payload = buildPayload()

  if (!payload.question) {
    answer.value = 'Escreve uma pergunta antes de submeter.'
    return
  }

  submitting.value = true

  try {
    eventBus.publish('ai:ask', payload)
    emit('ask', payload)
    answer.value = `Pergunta preparada para "${payload.context.title}". Liga aqui um provider AI para devolver uma resposta real.`
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.ai-assistant-button {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.ai-assistant-button__help {
  margin: 0 0 16px;
  color: var(--el-text-color-secondary);
  line-height: 1.6;
}

.ai-assistant-button__answer {
  margin-top: 16px;
  padding: 12px;
  border: 1px solid var(--el-border-color-light);
  border-radius: var(--el-border-radius-base);
  background: var(--el-fill-color-light);
  color: var(--el-text-color-primary);
  line-height: 1.5;
}

.ai-assistant-button__answer p {
  margin: 6px 0 0;
}
</style>
